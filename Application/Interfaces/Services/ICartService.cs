using Application.Common;
using Application.Dto.CartItemDTOs;

namespace Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<Result<CartItemDTO>> AddItemToCartAsync(int userId, AddToCartItemDTO request);
        Task<Result<CartItemDTO>> UpdateCartItemAsync(int userId, int cartItemId, int newQuantity);
        Task<Result<CartItemDTO>> RemoveCartItemAsync(int userId, int cartItemId);  // ✅ Fixed return type
        Task<CartSummaryDTO> GetCartSummaryAsync(int userId,CancellationToken cancellationToken = default);  // ✅ No Result wrapper for summary
        Task<Result<string>> ClearCartAsync(int userId,CancellationToken cancellationToken=default);  // ✅ String result for clear operation
        Task<bool> ValidateCartAsync(int userId,CancellationToken cancellationToken= default);  // ✅ Simple bool for validation
    }
}
