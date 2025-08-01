using Application.Common;
using Domain.Entities.Common;

namespace Application.Interfaces.Services
{
    public interface IEventUsageService
    {
        Task<bool> CanUserUseEventAsync(int eventId, int userId);
        Task<Result<string>> RecordEventUsageAsync(int eventId, int userId, int orderId, decimal discountApplied);
        Task<int> GetUserEventUsageCountAsync(int eventId, int userId);
        Task<bool> HasUserReachedEventLimitAsync(int eventId, int userId);
        Task<Result<EventUsage>> CreateEventUsageAsync(EventUsage eventUsage);
    }
}
