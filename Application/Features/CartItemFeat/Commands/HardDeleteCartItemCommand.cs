using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Commands
{
    public record HardDeleteCartItemCommand(int CartItemId, int? UserId) : IRequest<Result<CartItemDTO>>;

    public class HardDeleteCartItemCommandHandler : IRequestHandler<HardDeleteCartItemCommand, Result<CartItemDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartStockService _stockService;
        private readonly ILogger<HardDeleteCartItemCommandHandler> _logger;
        private readonly ICurrentUserService _userService;

        public HardDeleteCartItemCommandHandler(
            IUnitOfWork unitOfWork,
            ICartStockService stockService,
            ILogger<HardDeleteCartItemCommandHandler> logger,
            ICurrentUserService userService
            )
        {
            _unitOfWork = unitOfWork;
            _stockService = stockService;
            _logger = logger;
            _userService = userService;
          
        }

        public async Task<Result<CartItemDTO>> Handle(HardDeleteCartItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var UserId = Convert.ToInt32(_userService.UserId);
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    _logger.LogInformation("🗑️ Deleting cart item: CartItemId={CartItemId}, UserId={UserId}",
                        request.CartItemId, request.UserId);

                    // 1. Find cart item using your repository pattern
                    var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                        predicate: c => c.Id == request.CartItemId && c.UserId == UserId && !c.IsDeleted);

                    if (cartItem == null)
                    {
                        return Result<CartItemDTO>.Failure($"Cart item with ID {request.CartItemId} not found for user {request.UserId}");
                    }

                    // 2. Release stock reservation
                    if (cartItem.IsStockReserved)
                    {
                        var stockReleased = await _stockService.ReleaseStockAsync(cartItem.ProductId, cartItem.Quantity, cancellationToken);
                        if (!stockReleased)
                        {
                            _logger.LogWarning("⚠️ Failed to release stock for ProductId={ProductId}, Quantity={Quantity}",
                                cartItem.ProductId, cartItem.Quantity);
                        }
                    }

                    // 3. Convert to DTO before deletion
                    var cartItemDto = cartItem.ToDTO();

                    // 4. ✅ Hard delete using your repository's RemoveAsync method (better performance)
                    await _unitOfWork.CartItems.RemoveAsync(cartItem, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("✅ Cart item deleted successfully: CartItemId={CartItemId}",
                        request.CartItemId);

                    return Result<CartItemDTO>.Success(cartItemDto, "Cart item removed successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete cart item: CartItemId={CartItemId}, UserId={UserId}",
                    request.CartItemId, request.UserId);
                return Result<CartItemDTO>.Failure($"Failed to remove cart item: {ex.Message}");
            }
        }
    }
}