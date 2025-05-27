using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
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

        public CreatePlaceOrderCommandHandler(
            ICartItemRepository cartItemRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository
            )
        {
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
                       
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
            double totalAmount = 0;
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
