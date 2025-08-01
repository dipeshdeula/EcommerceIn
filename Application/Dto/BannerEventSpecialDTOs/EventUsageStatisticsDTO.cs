using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventUsageStatisticsDTO
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int TotalUsages { get; set; }
        public decimal TotalDiscount { get; set; }
        public int UniqueUsers { get; set; }
        public decimal AverageDiscount { get; set; }
        public decimal PerformanceScore => TotalUsages * 0.4m + UniqueUsers * 0.6m; // Weighted score
    }
}
