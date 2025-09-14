using Application.Common;
using Application.Dto.OrderDTOs;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Extension.Cache;
using Application.Features.CartItemFeat.Queries;
using Application.Features.OrderFeat.Events;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using MediatR;

namespace Application.Features.OrderFeat.Commands
{
    public record CreatePlaceOrderCommand(
            int UserId,
            string ShippingAddress,
            string ShippingCity,
            double? DeliveryLatitude = null,
            double? DeliveryLongitude = null,
            string? Notes = null
        ) : IRequest<Result<OrderDTO>>;

    public class CreatePlaceOrderCommandHandler : IRequestHandler<CreatePlaceOrderCommand, Result<OrderDTO>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly IMediator _mediator;
        private readonly IEventUsageService _eventUsageService;
        private readonly IHybridCacheService _cacheService;
        private readonly IShippingService _shippingService;
        public CreatePlaceOrderCommandHandler(
            ICartItemRepository cartItemRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            IUserRepository userRepository,
            IRabbitMqPublisher rabbitMqPublisher,
            IEventUsageService eventUsageService,
            IHybridCacheService cacheService,
            IShippingService shippingService,
            IMediator mediator)
        {
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _userRepository = userRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _cacheService = cacheService;
            _mediator = mediator;
            _eventUsageService = eventUsageService;
            _shippingService = shippingService;
        }
        public async Task<Result<OrderDTO>> Handle(CreatePlaceOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //Fetch cart items for the user
                var cartItems = await _cartItemRepository.GetByUserIdAsync(request.UserId);
                if (!cartItems.Any())
                {
                    return Result<OrderDTO>.Failure("Cart is empty");
                }

                // Group cart items by ProductId to handle duplicates
                var groupedCartItems = cartItems
                .GroupBy(ci => ci.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Items = group.ToList(),
                    TotalQuantity = group.Sum(ci => ci.Quantity),
                    // Use the most recent pricing or average if different
                    AveragePrice = group.Average(ci => ci.ReservedPrice),
                    TotalEventDiscount = group.Sum(ci => ci.EventDiscountAmount * ci.Quantity),
                    TotalRegularDiscount = group.Sum(ci => ci.RegularDiscountAmount * ci.Quantity),
                    // Take the first event ID if multiple (or implement business logic for priority)
                    AppliedEventId = group.FirstOrDefault(ci => ci.AppliedEventId.HasValue)?.AppliedEventId
                })
                .ToList();

                // Validate stock and calcualte total amount
                decimal totalAmount = 0;
                decimal? totalEventDiscount = 0;
                var orderItems = new List<OrderItem>();
                var eventUsagesToCreate = new List<EventUsage>();

                foreach (var groupedItem in groupedCartItems)
                {
                    var product = await _productRepository.FindByIdAsync(groupedItem.ProductId);
                    if (product == null)
                    {
                        return Result<OrderDTO>.Failure($"Product not found: {groupedItem.ProductId}");
                    }

                    if (product.ReservedStock < groupedItem.TotalQuantity)
                    {
                        return Result<OrderDTO>.Failure($"Insufficient reserve stock for product: {product.Name}. Available: {product.ReservedStock}, Required: {groupedItem.TotalQuantity}");
                    }

                    // Calculate total amount
                    var itemPrice = groupedItem.AveragePrice;
                    var itemTotal = itemPrice * groupedItem.TotalQuantity;
                    totalAmount += itemTotal;

                    // create single OrderItem per product
                    var orderItem = new OrderItem
                    {
                        ProductId = groupedItem.ProductId,
                        ProductName = product.Name,
                        Quantity = groupedItem.TotalQuantity,
                        UnitPrice = itemPrice,
                        EventDiscountAmount = groupedItem.TotalEventDiscount / groupedItem.TotalQuantity, // Per unit
                        RegularDiscountAmount = groupedItem.TotalRegularDiscount / groupedItem.TotalQuantity, // Per unit
                        AppliedEventId = groupedItem.AppliedEventId
                    };
                    orderItems.Add(orderItem);

                    //  Prepare event usage records - one per cart item that had event discount
                    foreach (var cartItem in groupedItem.Items.Where(ci => ci.AppliedEventId.HasValue && ci.EventDiscountAmount > 0))
                    {
                        var eventUsage = new EventUsage
                        {
                            BannerEventId = cartItem.AppliedEventId!.Value,
                            UserId = request.UserId,
                            // OrderId will be set after order creation
                            DiscountApplied = cartItem.EventDiscountAmount * cartItem.Quantity,
                            UsedAt = DateTime.UtcNow,
                            IsActive = false, // Will be activated on order confirmation
                            IsDeleted = false,
                            ProductsCount = cartItem.Quantity,
                            OrderValue = cartItem.ReservedPrice * cartItem.Quantity,
                            Notes = $"Order placement - Product: {product.Name}, Qty: {cartItem.Quantity}, CartItemId: {cartItem.Id}"
                        };
                        eventUsagesToCreate.Add(eventUsage);
                        totalEventDiscount += eventUsage.DiscountApplied;
                    }

                    // Deduct stock
                    product.StockQuantity -= groupedItem.TotalQuantity;
                    product.ReservedStock -= groupedItem.TotalQuantity;
                    await _productRepository.UpdateAsync(product);
                    

                }
                await _productRepository.SaveChangesAsync(cancellationToken);

                // Calcualte shipping cost using user's selected location
                var shippingRequest = new ShippingRequestDTO
                {
                    UserId = request.UserId,
                    DeliveryLatitude = request.DeliveryLatitude,
                    DeliveryLongitude = request.DeliveryLongitude,
                    Address = request.ShippingAddress,
                    City = request.ShippingCity,
                    OrderTotal = totalAmount
                };

                var shippingResult = await _shippingService.CalculateShippingAsync(shippingRequest,cancellationToken);
                if(!shippingResult.Succeeded)
                {
                    return Result<OrderDTO>.Failure("Failed to calculate shipping:" + shippingResult);
                }

                var shippingCost = shippingResult.Data.FinalShippingCost;
                var shippingId = shippingResult.Data.Configuration?.Id ?? 1;

                // Calculate grand Total
                var grandTotal = totalAmount + shippingCost;
                

                // Create Order
                var order = new Order
                {
                    UserId = request.UserId,
                    OrderDate = DateTime.UtcNow,
                    PaymentStatus = "Pending",
                    OrderStatus = "Pending",
                    TotalAmount = totalAmount,
                    GrandTotal = grandTotal,
                    EventDiscountAmount = totalEventDiscount ?? 0,
                    ShippingAddress = request.ShippingAddress,
                    ShippingCity = request.ShippingCity,
                    ShippingCost = shippingCost,
                    ShippingId = shippingId,
                    DeliveryLatitude = request.DeliveryLatitude,
                    DeliveryLongitude = request.DeliveryLongitude,
                    Notes = request.Notes,
                    IsConfirmed = false,

                };

                await _orderRepository.AddAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);


                // Publish OrderPlaced event to RabbitMQ
                //var orderPlaceEvent = new OrderPlacedEventDTO
                //{
                //    OrderId = order.Id,
                //    UserId = request.UserId,
                //    UserEmail = user.Email,
                //    ProductNames = orderItems.Select(oi => oi.ProductId.ToString()).ToArray(),
                //    TotalAmount = order.TotalAmount,
                //    OrderDate = order.OrderDate
                //};

                //_rabbitMqPublisher.Publish("OrderPlacedQueue", orderPlaceEvent, Guid.NewGuid().ToString(), null);


                // Add OrdersItems
                foreach (var orderItem in orderItems)
                {
                    orderItem.OrderId = order.Id;
                    await _orderItemRepository.AddAsync(orderItem, cancellationToken);
                }
                await _orderItemRepository.SaveChangesAsync(cancellationToken);

                // create provisonal event usage records
                foreach (var eventUsage in eventUsagesToCreate)
                {
                    eventUsage.OrderId = order.Id;
                    var createResult = await _eventUsageService.CreateEventUsageAsync(eventUsage);

                    if (!createResult.Succeeded)
                    {
                        Console.WriteLine($"Warning: Failed to create event usage: {createResult.Message}");
                    }
                }

                // Clear cart 
                await _cartItemRepository.DeleteByUserIdAsync(request.UserId);
                // clear cache
                // Invalidate user's cart cache 
                await _cacheService.InvalidateUserCartCacheAsync(request.UserId, cancellationToken);

                var user = await _userRepository.FindByIdAsync(order.UserId);
                if (user == null)
                {
                    return Result<OrderDTO>.Failure("User id not found");
                }


                await _mediator.Publish(new OrderPlacedEvent
                {
                    Order = order,
                    User = user,
                    ProductNames = orderItems.Select(oi => oi.ProductId.ToString()).ToArray()
                }, cancellationToken);



                // Return OrderDTO
                var orderDto = new OrderDTO
                {
                    Id = order.Id,
                    UserId = request.UserId,
                    User = user.ToDTO(),
                    OrderDate = order.OrderDate,
                    PaymentStatus = order.PaymentStatus,
                    OrderStatus = order.OrderStatus,
                    TotalAmount = order.TotalAmount,
                    GrandTotal = order.GrandTotal,
                    EventDiscountAmount = order.EventDiscountAmount ?? 0 ,
                    ShippingAddress = order.ShippingAddress,
                    ShippingCity = order.ShippingCity,
                    ShippingCost = order.ShippingCost,
                    ShippingId = order.ShippingId ?? 1,
                    DeliveryLatitude = order.DeliveryLatitude,
                    DeliveryLongitude = order.DeliveryLongitude,
                    Notes = order.Notes,
                    IsConfirmed = order.IsConfirmed,
                    Items = orderItems.Select(oi => new OrderItemDTO
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.ProductName,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        EventDiscountAmount = oi.EventDiscountAmount,
                        RegularDiscountAmount = oi.RegularDiscountAmount
                    }).ToList()

                };


                return Result<OrderDTO>.Success(orderDto, "Order placed successfully.");
            }
            catch (Exception ex)
            {
                return Result<OrderDTO>.Failure($"Failed to place order: {ex.Message}");
            }
        }
    }

}
