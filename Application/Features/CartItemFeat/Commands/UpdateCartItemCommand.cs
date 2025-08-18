using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.CartItemFeat.Commands
{
    public record UpdateCartItemCommand (
        int Id,int UserId,int? ProductId, int? Quantity) : IRequest<Result<CartItemDTO>>;

    public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<CartItemDTO>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        public UpdateCartItemCommandHandler(ICartItemRepository cartItemRepository)
        {
            _cartItemRepository = cartItemRepository;            
        }
        public async Task<Result<CartItemDTO>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
        {
            var cartItem = await _cartItemRepository.FindByIdAsync(request.Id);
            var user = await _cartItemRepository.FindByIdAsync(request.Id);
            
            if (cartItem == null)
                return Result<CartItemDTO>.Failure($"CartItem with Id : {request.Id} is not found");
            if (user == null)
                return Result<CartItemDTO>.Failure($"User is not found");

            cartItem.ProductId = request.ProductId ?? cartItem.ProductId;
            cartItem.Quantity = request.Quantity ?? cartItem.Quantity;

            await _cartItemRepository.UpdateAsync(cartItem,cancellationToken);
            await _cartItemRepository.SaveChangesAsync(cancellationToken);

            return Result<CartItemDTO>.Success(cartItem.ToDTO(), "Cart item updated successfully");
        }
    }

}
