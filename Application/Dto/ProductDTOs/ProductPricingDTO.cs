using Application.Dto.Shared;
using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class ProductPricingDTO
    {
        public int ProductId { get; set; }

        //  CORE PRICING 
        public decimal OriginalPrice { get; set; }      // Market price
        public decimal BasePrice { get; set; }          // After product discount
        public decimal EffectivePrice { get; set; }     // Final price after all discounts

        //  DISCOUNT BREAKDOWN 
        public decimal ProductDiscountAmount { get; set; }
        public decimal EventDiscountAmount { get; set; }
        public decimal TotalDiscountAmount => ProductDiscountAmount + EventDiscountAmount;
        public decimal TotalDiscountPercentage => OriginalPrice > 0
            ? Math.Round((TotalDiscountAmount / OriginalPrice) * 100, 2) : 0;

        //  DISCOUNT FLAGS 
        public bool HasProductDiscount => ProductDiscountAmount > 0;
        public bool HasEventDiscount => EventDiscountAmount > 0;
        public bool HasAnyDiscount => TotalDiscountAmount > 0;
        public bool IsOnSale => HasAnyDiscount;

        //  EVENT INFORMATION 
        public int? ActiveEventId { get; set; }
        public string? ActiveEventName { get; set; }
        public string? EventTagLine { get; set; }
        public PromotionType? PromotionType { get; set; }
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }

        //  EVENT STATUS 
        public bool HasActiveEvent => ActiveEventId.HasValue;
        public bool IsEventActive => HasActiveEvent &&
                                   DateTime.UtcNow >= EventStartDate &&
                                   DateTime.UtcNow <= EventEndDate;
        public TimeSpan? EventTimeRemaining => EventEndDate.HasValue && EventEndDate > DateTime.UtcNow
            ? EventEndDate - DateTime.UtcNow : null;
        public bool IsEventExpiringSoon => EventTimeRemaining.HasValue &&
                                          EventTimeRemaining.Value.TotalHours <= 24;

        //  FORMATTED DISPLAY 
        public string FormattedOriginalPrice => $"Rs. {OriginalPrice:F2}";
        public string FormattedEffectivePrice => $"Rs. {EffectivePrice:F2}";
        public string FormattedSavings => HasAnyDiscount ? $"Save Rs. {TotalDiscountAmount:F2}" : string.Empty;

        public string FormattedDiscountBreakdown
        {
            get
            {
                if (!HasAnyDiscount) return string.Empty;

                var parts = new List<string>();
                if (HasProductDiscount) parts.Add($"Rs. {ProductDiscountAmount:F2} regular");
                if (HasEventDiscount) parts.Add($"Rs. {EventDiscountAmount:F2} event");

                var breakdown = parts.Any() ? $" ({string.Join(" + ", parts)})" : "";
                return $"Save Rs. {TotalDiscountAmount:F2}{breakdown}";
            }
        }

        public string EventStatus => !HasActiveEvent ? string.Empty :
                                   EventTimeRemaining.HasValue ?
                                   FormatTimeRemaining(EventTimeRemaining.Value) : "Event Active";

        //  PRICING VALIDATION 
        public bool IsPriceStable { get; set; } = true;
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPriceValidWithTolerance(decimal expectedPrice, decimal tolerance = 0.01m) =>
            Math.Abs(EffectivePrice - expectedPrice) <= tolerance;

        //  HELPER METHODS 
        private string FormatTimeRemaining(TimeSpan timeRemaining)
        {
            if (timeRemaining.TotalDays >= 1)
                return $"Ends in {(int)timeRemaining.TotalDays} day(s)";
            if (timeRemaining.TotalHours >= 1)
                return $"Ends in {(int)timeRemaining.TotalHours} hour(s)";
            if (timeRemaining.TotalMinutes >= 1)
                return $"Ends in {(int)timeRemaining.TotalMinutes} minute(s)";
            return "Ends soon";
        }

        //  PRICING BREAKDOWN FOR DETAILED ANALYSIS 
        public PricingBreakdown GetDetailedBreakdown() => new()
        {
            OriginalPrice = OriginalPrice,
            BasePrice = BasePrice,
            EffectivePrice = EffectivePrice,
            ProductDiscountAmount = ProductDiscountAmount,
            EventDiscountAmount = EventDiscountAmount,
            TotalSavings = TotalDiscountAmount,
            ProductDiscountPercentage = OriginalPrice > 0 && HasProductDiscount
                ? Math.Round((ProductDiscountAmount / OriginalPrice) * 100, 2) : 0,
            EventDiscountPercentage = BasePrice > 0 && HasEventDiscount
                ? Math.Round((EventDiscountAmount / BasePrice) * 100, 2) : 0,
            TotalDiscountPercentage = TotalDiscountPercentage,
            HasProductDiscount = HasProductDiscount,
            HasEventDiscount = HasEventDiscount,
            FormattedBreakdown = FormattedDiscountBreakdown
        };
    }
}
