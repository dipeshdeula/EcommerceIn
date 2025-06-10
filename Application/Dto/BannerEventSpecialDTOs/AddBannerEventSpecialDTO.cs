using System.Text.Json.Serialization;
using Domain.Enums.BannerEventSpecial;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class AddBannerEventSpecialDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TagLine { get; set; }
        public EventType EventType { get; set; } = EventType.Seasonal;
        public PromotionType PromotionType { get; set; } = PromotionType.Percentage;
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }

        //  NEPAL TIME INPUTS - Using STRING format to avoid DateTime.Kind issues
        /// <summary>Event start time in Nepal Standard Time (ISO format: YYYY-MM-DDTHH:mm:ss)</summary>
        [JsonPropertyName("startDateNepal")]
        public string StartDateNepal { get; set; } = string.Empty;

        /// <summary>Event end time in Nepal Standard Time (ISO format: YYYY-MM-DDTHH:mm:ss)</summary>
        [JsonPropertyName("endDateNepal")]
        public string EndDateNepal { get; set; } = string.Empty;

        //  ACTIVE TIME SLOT
        [JsonPropertyName("activeTimeSlot")]
        public string? ActiveTimeSlot { get; set; }

        public int? MaxUsageCount { get; set; }
        public int? MaxUsagePerUser { get; set; }
        public int? Priority { get; set; }

        // HELPER PROPERTIES (COMPUTED)
        [JsonIgnore]
        public DateTime StartDateParsed 
        { 
            get 
            {
                if (DateTime.TryParse(StartDateNepal, out var date))
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                return DateTime.MinValue;
            }
        }

        [JsonIgnore]
        public DateTime EndDateParsed 
        { 
            get 
            {
                if (DateTime.TryParse(EndDateNepal, out var date))
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                return DateTime.MinValue;
            }
        }

        [JsonIgnore]
        public TimeSpan? ActiveTimeSlotParsed
        {
            get
            {
                if (string.IsNullOrEmpty(ActiveTimeSlot))
                    return null;
                return TimeSpan.TryParse(ActiveTimeSlot, out var timeSpan) ? timeSpan : null;
            }
        }

        [JsonIgnore]
        public bool IsValidDateRange => EndDateParsed > StartDateParsed && StartDateParsed != DateTime.MinValue;

        [JsonIgnore]
        public double DurationHours => IsValidDateRange ? (EndDateParsed - StartDateParsed).TotalHours : 0;

        [JsonIgnore]
        public bool IsValidDuration => DurationHours >= 0.5 && DurationHours <= 8760; // 30 min to 1 year

        //  DISPLAY HELPERS
        [JsonPropertyName("dateRangeDisplayNepal")]
        public string DateRangeDisplayNepal => IsValidDateRange 
            ? $"{StartDateParsed:yyyy-MM-dd HH:mm} to {EndDateParsed:yyyy-MM-dd HH:mm} NPT"
            : "Invalid date range";

        [JsonPropertyName("durationDisplay")]
        public string DurationDisplay => DurationHours switch
        {
            0 => "Invalid duration",
            < 1 => $"{(int)(DurationHours * 60)} minutes",
            < 24 => $"{DurationHours:F1} hours",
            _ => $"{(DurationHours / 24):F1} days"
        };

        [JsonPropertyName("estimatedDiscountDisplay")]
        public string EstimatedDiscountDisplay => PromotionType == PromotionType.Percentage
            ? $"{DiscountValue}% OFF"
            : $"Rs.{DiscountValue} OFF";
    }
}