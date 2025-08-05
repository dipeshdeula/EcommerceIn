using System.Text.Json.Serialization;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventUsageSummaryDTO
    {
        [JsonPropertyName("totalUsages")]
        public int TotalUsages { get; set; }

        [JsonPropertyName("uniqueUsers")]
        public int UniqueUsers { get; set; }

        [JsonPropertyName("totalDiscountGiven")]
        public decimal TotalDiscountGiven { get; set; }

        [JsonPropertyName("averageDiscountPerUser")]
        public decimal AverageDiscountPerUser { get; set; }

        [JsonPropertyName("conversionRate")]
        public decimal ConversionRate { get; set; }

        [JsonPropertyName("lastUsedAt")]
        public DateTime? LastUsedAt { get; set; }

        [JsonPropertyName("mostActiveDay")]
        public string MostActiveDay { get; set; } = string.Empty;

        //  Computed properties for better UX
        [JsonPropertyName("formattedTotalDiscount")]
        public string FormattedTotalDiscount => $"Rs.{TotalDiscountGiven:F2}";

        [JsonPropertyName("formattedAverageDiscount")]
        public string FormattedAverageDiscount => $"Rs.{AverageDiscountPerUser:F2}";

        [JsonPropertyName("usageFrequency")]
        public string UsageFrequency => TotalUsages switch
        {
            0 => "No Usage",
            < 10 => "Low Usage",
            < 50 => "Moderate Usage",
            < 100 => "High Usage",
            _ => "Very High Usage"
        };

        [JsonPropertyName("performanceRating")]
        public string PerformanceRating => ConversionRate switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Average",
            >= 20 => "Below Average",
            _ => "Poor"
        };

        [JsonPropertyName("engagementMetrics")]
        public object EngagementMetrics => new
        {
            averageUsagePerUser = UniqueUsers > 0 ? Math.Round((decimal)TotalUsages / UniqueUsers, 2) : 0,
            userRetentionRate = ConversionRate,
            discountEfficiency = TotalUsages > 0 ? Math.Round(TotalDiscountGiven / TotalUsages, 2) : 0
        };
    }
}
