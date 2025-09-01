using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.CartItemFeat.Commands
{
    public record UpdateCartItemCommand (
        int Id,int UserId,int? ProductId, int? Quantity, ShippingRequestDTO? ShippingRequest = null) : IRequest<Result<CartItemDTO>>;

    public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<CartItemDTO>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IShippingService _shippingService;
        public UpdateCartItemCommandHandler(
            ICartItemRepository cartItemRepository,
            IShippingService shippingService
            )
        {
            _cartItemRepository = cartItemRepository;
            _shippingService = shippingService;        
        }
        public async Task<Result<CartItemDTO>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
        {
            var cartItem = await _cartItemRepository.FindByIdAsync(request.Id);
                        
            if (cartItem == null)
                return Result<CartItemDTO>.Failure($"CartItem with Id : {request.Id} is not found");
            

            cartItem.ProductId = request.ProductId ?? cartItem.ProductId;
            cartItem.Quantity = request.Quantity ?? cartItem.Quantity;
            
            if (request.ShippingRequest != null)
            {
                var shippingRequest = new ShippingRequestDTO
                {
                    UserId = request.UserId,
                    OrderTotal = cartItem.ReservedPrice * cartItem.Quantity,
                    DeliveryLatitude = request.ShippingRequest.DeliveryLatitude,
                    DeliveryLongitude = request.ShippingRequest.DeliveryLongitude,
                    Address = request.ShippingRequest.Address,
                    City = request.ShippingRequest.City,
                    RequestRushDelivery = request.ShippingRequest.RequestRushDelivery,
                    RequestedDeliveryDate = request.ShippingRequest.RequestedDeliveryDate
                };

                var shippingResult = await _shippingService.CalculateShippingAsync(shippingRequest, cancellationToken);
                if (shippingResult.Succeeded && shippingResult.Data != null)
                {
                    cartItem.ShippingCost = shippingResult.Data.FinalShippingCost;
                    cartItem.ShippingId = shippingResult.Data.Configuration?.Id ?? cartItem.ShippingId;
                    cartItem.DeliveryLatitude = shippingRequest.DeliveryLatitude;
                    cartItem.DeliveryLongitude = shippingRequest.DeliveryLongitude;
                    cartItem.ShippingAddress = shippingRequest.Address;
                    cartItem.ShippingCity = shippingRequest.City;
                }
            }

            cartItem.UpdatedAt = DateTime.UtcNow;

            await _cartItemRepository.UpdateAsync(cartItem, cancellationToken);
            await _cartItemRepository.SaveChangesAsync(cancellationToken);
            await _cartItemRepository.LoadNavigationProperties(cartItem);


            var dto = cartItem.ToDTO();
            dto.shipping!.ShippingMessage = cartItem.ShippingCost == 0 ? "Free shipping applied!" : $"Shipping: Rs. {cartItem.ShippingCost:F2}";

            return Result<CartItemDTO>.Success(dto, "Cart item updated successfully");

            
        }
    }

}
