using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Dto.PromoCodeDTOs
{
    public class PromoCodeDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public PromoCodeType Type { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }

        public int? MaxTotalUsage { get; set; }
        public int? MaxUsagePerUser { get; set; }
        public int CurrentUsageCount { get; set; }
        public int RemainingUsage => MaxTotalUsage.HasValue ? Math.Max(0, MaxTotalUsage.Value - CurrentUsageCount) : int.MaxValue;

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("startDateNepal")]
        public DateTime StartDateNepal { get; set; }

        [JsonPropertyName("endDateNepal")]
        public DateTime EndDateNepal { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool ApplyToShipping { get; set; }
        public bool StackableWithEvents { get; set; }

        public string? CustomerTier { get; set; }
        public string? AdminNotes { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? LastModifiedByUserName { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        [JsonPropertyName("isCurrentlyActive")]
        public bool IsCurrentlyActive { get; set; }

        [JsonPropertyName("isExpired")]
        public bool IsExpired { get; set; }

        [JsonPropertyName("timeStatus")]
        public string TimeStatus { get; set; } = string.Empty;

        [JsonPropertyName("daysRemaining")]
        public int DaysRemaining { get; set; }



        // Computed Properties
        public PromoCodeStatus Status => GetStatus();
        public string FormattedDiscount => Type switch
        {
            PromoCodeType.Percentage => $"{DiscountValue}% OFF",
            PromoCodeType.FixedAmount => $"Rs.{DiscountValue} OFF",
            PromoCodeType.FreeShipping => "FREE SHIPPING",
            _ => "SPECIAL DISCOUNT"
        };

        [JsonPropertyName("formattedValidPeriod")]
        public string FormattedValidPeriod => $"{StartDateNepal:MMM dd} - {EndDateNepal:MMM dd, yyyy}";
        public bool IsNotStarted => DateTime.UtcNow < StartDate;
        public bool IsCurrentlyValid => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && IsActive;

        //  FORMATTED STRINGS
        [JsonPropertyName("formattedStartDate")]
        public string FormattedStartDate => StartDateNepal.ToString("yyyy-MM-dd HH:mm");

        [JsonPropertyName("formattedEndDate")]
        public string FormattedEndDate => EndDateNepal.ToString("yyyy-MM-dd HH:mm");

        [JsonPropertyName("formattedDuration")]
        public string FormattedDuration => $"{(EndDateNepal - StartDateNepal).Days} days";

        private PromoCodeStatus GetStatus()
        {
            if (!IsActive) return PromoCodeStatus.Suspended;
            if (DateTime.UtcNow > EndDate) return PromoCodeStatus.Expired;
            if (MaxTotalUsage.HasValue && CurrentUsageCount >= MaxTotalUsage.Value) return PromoCodeStatus.ExhaustedUsage;
            if (DateTime.UtcNow < StartDate) return PromoCodeStatus.Draft;
            return PromoCodeStatus.Active;
        }
    }  
    
}
