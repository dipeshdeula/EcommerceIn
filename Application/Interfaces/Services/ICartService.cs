using Application.Common;
using Application.Dto.CartItemDTOs;

namespace Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<Result<CartItemDTO>> AddItemToCartAsync(int userId, AddToCartItemDTO request);
        Task<Result<CartItemDTO>> UpdateCartItemAsync(int userId, int cartItemId, int newQuantity);
        Task<Result<CartItemDTO>> RemoveCartItemAsync(int userId, int cartItemId);
        // Task<CartSummaryDTO> GetCartSummaryAsync(int userId,CancellationToken cancellationToken = default);  
        Task<Result<IEnumerable<CartItemDTO>>> GetCartItemsAsync(int userId, CancellationToken cancellationToken = default);
        Task<Result<string>> ClearCartAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> ValidateCartAsync(int userId, CancellationToken cancellationToken = default);
    }
}
