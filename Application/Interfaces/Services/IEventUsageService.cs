using Application.Common;
using Domain.Entities.Common;

namespace Application.Interfaces.Services
{
    public interface IEventUsageService
    {
        Task<bool> CanUserUseEventAsync(int eventId, int userId);
        Task<Result<string>> RecordEventUsageAsync(int eventId, int userId, int orderId, decimal discountApplied);
        Task<int> GetUserEventUsageCountAsync(int eventId, int userId, int productId);
        Task<int> GetUserProductEventUsageCountAsync(int eventId, int userId, int productId);
        Task<bool> HasUserReachedEventLimitAsync(int eventId, int userId);
        Task<Result<EventUsage>> CreateEventUsageAsync(EventUsage eventUsage);
        Task<int> GetTotalUserEventUsageAsync(int eventId, int userId);
        Task<Result<string>> CanUserAddQuantityToCartAsync(int eventId, int userId, int requestedQuantity);

        Task<Result<bool>> CanUserAddQuantityToCartForProductAsync(
            int eventId,
            int userId,
            int requestedQuantity,
            int productId
        );

        /// <summary>
        /// Get all event usage records for a specific order
        /// </summary>
        Task<IEnumerable<EventUsage>> GetEventUsagesByOrderIdAsync(int orderId);

        /// <summary>
        /// Reverse/deactivate event usage for cancelled/unconfirmed orders
        /// </summary>
        Task<Result<string>> ReverseEventUsageForOrderAsync(int orderId, int userId);

        /// <summary>
        /// Mark event usage as active for confirmed orders
        /// </summary>
        Task<Result<string>> MarkEventUsageAsActiveAsync(int orderId, int userId);
    }
}
