using Application.Common;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Commands
{
    public record RemovePromoCodeFromCartCommand(
        int UserId,
        string PromoCode
    ) : IRequest<Result<string>>;

    public class RemovePromoCodeFromCartCommandHandler : IRequestHandler<RemovePromoCodeFromCartCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RemovePromoCodeFromCartCommandHandler> _logger;
        
        public RemovePromoCodeFromCartCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<RemovePromoCodeFromCartCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        
        public async Task<Result<string>> Handle(RemovePromoCodeFromCartCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //  Get cart items with this promo code applied
                var cartItems = await _unitOfWork.CartItems.GetAllAsync(
                    predicate: c => c.UserId == request.UserId && 
                                  !c.IsDeleted && 
                                  c.ExpiresAt > DateTime.UtcNow &&
                                  c.AppliedPromoCode == request.PromoCode,
                    cancellationToken: cancellationToken
                );
                
                if (!cartItems.Any())
                {
                    return Result<string>.Failure("No cart items found with this promo code");
                }
                
                //  Remove promo code from each cart item
                foreach (var cartItem in cartItems)
                {
                    cartItem.RemovePromoCode();
                    await _unitOfWork.CartItems.UpdateAsync(cartItem, cancellationToken);
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
               
                
                return Result<string>.Success($"Promo code '{request.PromoCode}' removed from items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing promo code '{Code}' from cart for user {UserId}", 
                    request.PromoCode, request.UserId);
                return Result<string>.Failure($"Error removing promo code: {ex.Message}");
            }
        }
    }
}