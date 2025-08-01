using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventPerformanceReportDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string ReportPeriod { get; set; } = string.Empty;
        public int TotalUsages { get; set; }
        public int UniqueUsers { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal AverageDiscountPerUser { get; set; }
        public decimal ConversionRate { get; set; }
        public List<DailyUsageBreakdown> DailyBreakdown { get; set; } = new();
    }

    public class DailyUsageBreakdown
    {
        public DateTime Date { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalDiscount { get; set; }
        public int UniqueUsers { get; set; }
    }
}
