using Application.Common;
using Application.Dto.OrderDTOs;
using Application.Features.OrderFeat.Events;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.OrderFeat.Commands
{
    public record CreatePlaceOrderCommand(
            int UserId,
            string ShippingAddress,
            string ShippingCity
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
        public CreatePlaceOrderCommandHandler(
            ICartItemRepository cartItemRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            IUserRepository userRepository,
            IRabbitMqPublisher rabbitMqPublisher,
            IMediator mediator)
        {
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _userRepository = userRepository;
            _rabbitMqPublisher = rabbitMqPublisher;
            _mediator = mediator;
        }
        public async Task<Result<OrderDTO>> Handle(CreatePlaceOrderCommand request, CancellationToken cancellationToken)
        {
            //Fetch cart items for the user
            var cartItems = await _cartItemRepository.GetByUserIdAsync(request.UserId);
            if (!cartItems.Any())
            {
                return Result<OrderDTO>.Failure("Cart is empty");
            }

            // Validate stock and calcualte total amount
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cartItems)
            {
                var product = await _productRepository.FindByIdAsync(cartItem.ProductId);
                if (product == null || product.ReservedStock < cartItem.Quantity)
                {
                    return Result<OrderDTO>.Failure($"Insufficient reserve stock for product: {cartItem.Product.Name}");
                }

                // Calculate total amount
                totalAmount += product.CostPrice * cartItem.Quantity;

                // create OrderItem
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Product.CostPrice
                };
                orderItems.Add(orderItem);

                // Deduct stock
                product.StockQuantity -= cartItem.Quantity;
                product.ReservedStock -= cartItem.Quantity;
                await _productRepository.UpdateAsync(product);

            }

            // Create Order
            var order = new Order
            {
                UserId = request.UserId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = totalAmount,
                ShippingAddress = request.ShippingAddress,
                ShippingCity = request.ShippingCity,

            };

            await _orderRepository.AddAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            var user = await _userRepository.FindByIdAsync(order.UserId);
            if (user == null)
            {
                return Result<OrderDTO>.Failure("User id not found");
            }

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

            await _mediator.Publish(new OrderPlacedEvent
            {
                Order = order,
                User = user,
                ProductNames = orderItems.Select(oi => oi.ProductId.ToString()).ToArray()
            });

            // Add OrdersItems
            foreach (var orderItem in orderItems)
            {
                orderItem.OrderId = order.Id;
                await _orderItemRepository.AddAsync(orderItem, cancellationToken);
            }

            // Clear cart 
            await _cartItemRepository.DeleteByUserIdAsync(request.UserId);



            // Return OrderDTO
            var orderDto = new OrderDTO
            {
                Id = order.Id,
                UserId = request.UserId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,

            };

            return Result<OrderDTO>.Success(orderDto, "Order placed successfully.");
        }
    }

}
