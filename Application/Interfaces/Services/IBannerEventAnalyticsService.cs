using Application.Common;
using Application.Dto.BannerEventSpecialDTOs;

namespace Application.Interfaces.Services
{
    public interface IBannerEventAnalyticsService
    {
        Task<EventPerformanceReportDTO> GetEventPerformanceAsync(int eventId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<EventUsageStatisticsDTO>> GetTopPerformingEventsAsync(int count = 10);
        Task<decimal> GetTotalDiscountsGivenAsync(DateTime fromDate, DateTime toDate);
        Task<Result<EventUsageStatisticsDTO>> GetEventUsageStatisticsAsync(int eventId, CancellationToken cancellationToken = default);
    }
}
