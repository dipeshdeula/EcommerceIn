using Application.Dto.BannerEventSpecialDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class BannerEventAnalyticsService : IBannerEventAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BannerEventAnalyticsService> _logger;

        public BannerEventAnalyticsService(IUnitOfWork unitOfWork, ILogger<BannerEventAnalyticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EventPerformanceReportDTO> GetEventPerformanceAsync(int eventId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var fromUtc = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var toUtc = toDate ?? DateTime.UtcNow;

                var eventUsages = await _unitOfWork.EventUsages.GetAllAsync(
                    predicate: u => u.BannerEventId == eventId &&
                                   u.UsedAt >= fromUtc &&
                                   u.UsedAt <= toUtc &&
                                   !u.IsDeleted,
                    includeProperties: "User,BannerEvent",
                    cancellationToken:default);

                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(eventId);

                var report = new EventPerformanceReportDTO
                {
                    EventId = eventId,
                    EventName = bannerEvent?.Name ?? "Unknown Event",
                    ReportPeriod = $"{fromUtc:yyyy-MM-dd} to {toUtc:yyyy-MM-dd}",
                    TotalUsages = eventUsages.Count(),
                    UniqueUsers = eventUsages.Select(u => u.UserId).Distinct().Count(),
                    TotalDiscountGiven = eventUsages.Sum(u => u.DiscountApplied),
                    AverageDiscountPerUser = eventUsages.Any() ? eventUsages.Average(u => u.DiscountApplied) : 0,
                    ConversionRate = await CalculateConversionRate(eventId, fromUtc, toUtc),
                    DailyBreakdown = GetDailyBreakdown(eventUsages.ToList())
                };

                _logger.LogInformation("Generated performance report for event {EventId}: {TotalUsages} usages, {UniqueUsers} users",
                    eventId, report.TotalUsages, report.UniqueUsers);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance report for event {EventId}", eventId);
                return new EventPerformanceReportDTO
                {
                    EventId = eventId,
                    EventName = "Error",
                    ReportPeriod = "Error generating report"
                };
            }
        }

        public async Task<List<EventUsageStatisticsDTO>> GetTopPerformingEventsAsync(int count = 10)
        {
            try
            {
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                var eventStats = await _unitOfWork.EventUsages.GetAllAsync(
                    predicate: u => u.UsedAt >= thirtyDaysAgo && !u.IsDeleted,
                    includeProperties: "BannerEvent",
                    cancellationToken:default);

                var result = eventStats
                    .GroupBy(u => u.BannerEventId)
                    .Select(g => new EventUsageStatisticsDTO
                    {
                        EventId = g.Key,
                        EventName = g.First().BannerEvent?.Name ?? "Unknown",
                        TotalUsages = g.Count(),
                        TotalDiscount = g.Sum(u => u.DiscountApplied),
                        UniqueUsers = g.Select(u => u.UserId).Distinct().Count(),
                        AverageDiscount = g.Average(u => u.DiscountApplied)
                    })
                    .OrderByDescending(s => s.PerformanceScore)
                    .Take(count)
                    .ToList();

                _logger.LogInformation("Retrieved top {Count} performing events", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top performing events");
                return new List<EventUsageStatisticsDTO>();
            }
        }

        public async Task<decimal> GetTotalDiscountsGivenAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var eventUsages = await _unitOfWork.EventUsages.GetAllAsync(
                    predicate: u => u.UsedAt >= fromDate &&
                                   u.UsedAt <= toDate &&
                                   !u.IsDeleted,
                                   cancellationToken:default);

                var totalDiscount = eventUsages.Sum(u => u.DiscountApplied);
                
                _logger.LogInformation("Total discounts given from {FromDate} to {ToDate}: Rs.{TotalDiscount:F2}",
                    fromDate, toDate, totalDiscount);

                return totalDiscount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total discounts given from {FromDate} to {ToDate}", fromDate, toDate);
                return 0;
            }
        }

        // Calculate conversion rate
        private async Task<decimal> CalculateConversionRate(int eventId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Get total event views/impressions (you might want to track this separately)
                // For now, we'll use a simple metric based on event usage vs active users
                var eventUsages = await _unitOfWork.EventUsages.CountAsync(
                    predicate: u => u.BannerEventId == eventId &&
                                   u.UsedAt >= fromDate &&
                                   u.UsedAt <= toDate &&
                                   !u.IsDeleted);

                if (eventUsages == 0) return 0;

                // Get total orders in the same period (approximate conversion base)
                var totalOrders = await _unitOfWork.Orders.CountAsync(
                    predicate: o => o.OrderDate >= fromDate && o.OrderDate <= toDate);

                if (totalOrders == 0) return 0;

                return Math.Round((decimal)eventUsages / totalOrders * 100, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating conversion rate for event {EventId}", eventId);
                return 0;
            }
        }

        //  Generate daily breakdown
        private List<DailyUsageBreakdown> GetDailyBreakdown(List<EventUsage> eventUsages)
        {
            try
            {
                return eventUsages
                    .GroupBy(u => u.UsedAt.Date)
                    .Select(g => new DailyUsageBreakdown
                    {
                        Date = g.Key,
                        UsageCount = g.Count(),
                        TotalDiscount = g.Sum(u => u.DiscountApplied),
                        UniqueUsers = g.Select(u => u.UserId).Distinct().Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily breakdown");
                return new List<DailyUsageBreakdown>();
            }
        }
    }
}