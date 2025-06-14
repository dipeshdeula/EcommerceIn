using Application.Common.Helper;
using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class BannerEventSpecialDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TagLine { get; set; }
        public EventType EventType { get; set; }
        public PromotionType PromotionType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }

        [JsonIgnore]
        public DateTime StartDate { get; set; }
        [JsonIgnore]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("startDateNepal")]
        public DateTime StartDateNepal { get; set; }

        [JsonPropertyName("endDateNepal")]
        public DateTime EndDateNepal { get; set; }

        //  FORMATTED DATE STRINGS
        [JsonPropertyName("startDateNepalString")]
        public string StartDateNepalString => StartDateNepal.ToString("yyyy-MM-dd HH:mm:ss");

        [JsonPropertyName("endDateNepalString")]
        public string EndDateNepalString => EndDateNepal.ToString("yyyy-MM-dd HH:mm:ss");

        [JsonPropertyName("startDateUtcString")]
        public string StartDateUtcString => StartDate.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

        [JsonPropertyName("endDateUtcString")]
        public string EndDateUtcString => EndDate.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";


        [JsonPropertyName("activeTimeSlot")]
        public string? ActiveTimeSlotString 
        { 
            get => ActiveTimeSlot?.ToString(@"hh\:mm\:ss");
            set 
            {
                if (string.IsNullOrEmpty(value))
                {
                    ActiveTimeSlot = null;
                }
                else if (TimeSpan.TryParse(value, out var timeSpan))
                {
                    ActiveTimeSlot = timeSpan;
                }
                else
                {
                    ActiveTimeSlot = null;
                }
            }
        }
        
        [JsonIgnore]
        public TimeSpan? ActiveTimeSlot { get; set; }
        public int MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public int MaxUsagePerUser { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public EventStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }     

        // Related Data
        public List<int>? ProductIds { get; set; }
        public int TotalProductsCount { get; set; }
        public int TotalRulesCount { get; set; }


        // ADDED: Computed properties for better UX
        [JsonPropertyName("hasActiveTimeSlot")]
        public bool HasActiveTimeSlot => ActiveTimeSlot.HasValue;

        [JsonPropertyName("isCurrentlyActive")]
        public bool IsCurrentlyActive { get; set; }

        [JsonPropertyName("isExpired")]
        public bool IsExpired { get; set; }

        [JsonPropertyName("daysRemaining")]
        public int DaysRemaining { get; set; }

        [JsonPropertyName("timeStatus")]
        public string TimeStatus { get; set; } = string.Empty;


        //  Formatted Display Properties
        [JsonPropertyName("formattedDiscount")]
        public string FormattedDiscount => PromotionType == PromotionType.Percentage ? $"{DiscountValue}% OFF" : $"Rs.{DiscountValue} OFF";

        [JsonPropertyName("formattedDateRangeNepal")]
        public string FormattedDateRange => $"{StartDateNepal:MMM dd, yyyy HH:mm} - {EndDate:MMM dd, yyyy HH:mm} NPT";

        [JsonPropertyName("fromattedDateRangeUtc")]
        public string FormattedDateRangeUtc => $"{StartDate:MMM dd, yyyy HH:mm} - {EndDate:MMM dd, yyyy HH:mm} UTC";

        public string StatusBadge => Status.ToString().ToUpper();

        [JsonPropertyName("priorityBadge")]
        public string PriorityBadge => Priority switch
        {
            >= 80 => "HIGH",
            >= 50 => "MEDIUM",
            >= 20 => "NORMAL",
            _ => "LOW"
        };

        [JsonPropertyName("usagePercentage")]
        public decimal UsagePercentage => MaxUsageCount > 0
           ? Math.Round((decimal)CurrentUsageCount / MaxUsageCount * 100, 2)
           : 0;

        [JsonPropertyName("remainingUsage")]
        public int RemainingUsage => Math.Max(0, MaxUsageCount - CurrentUsageCount);

        [JsonPropertyName("isUsageLimitReached")]
        public bool IsUsageLimitReached => CurrentUsageCount >= MaxUsageCount;


        // Timezone Info
        [JsonPropertyName("timeZoneInfo")]
        public TimeZoneDisplayInfo TimeZoneInfo { get; set; } = new();

        public ICollection<BannerImageDTO> Images { get; set; } = new List<BannerImageDTO>();
        public ICollection<EventRuleDTO> Rules { get; set; } = new List<EventRuleDTO>();
        public ICollection<EventProductDTO> EventProducts { get; set; } = new List<EventProductDTO>();
    }


}
