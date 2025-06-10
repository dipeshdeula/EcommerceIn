
using Domain.Enums.BannerEventSpecial;

namespace Application.Dto.ProductDTOs
{
    public class ProductPriceInfoDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal EffectivePrice { get; set; }

        // ✅ Calculated properties
        public decimal DiscountAmount => OriginalPrice - EffectivePrice;
        public decimal DiscountPercentage => OriginalPrice > 0 ? Math.Round((DiscountAmount / OriginalPrice) * 100, 2) : 0;
        public bool HasDiscount => DiscountAmount > 0;

        // ✅ Event information
        public int? AppliedEventId { get; set; }
        public string? AppliedEventName { get; set; }
        public string? EventTagLine { get; set; }
        public PromotionType? PromotionType { get; set; }
        public DateTime? EventEndDate { get; set; }

        // ✅ User context
        // public bool IsEligible { get; set; } = true;
        // public string? IneligibilityReason { get; set; }
        // public int? UserUsageCount { get; set; }
        // public int? MaxUsagePerUser { get; set; }
        // public bool CanUseEvent => UserUsageCount < MaxUsagePerUser;


        public string FormattedOriginalPrice => $"Rs.{OriginalPrice:F2}";
        public string FormattedEffectivePrice => $"Rs.{EffectivePrice:F2}";
        public string FormattedSavings => HasDiscount ? $"Save Rs.{DiscountAmount:F2}" : string.Empty;
        public bool IsOnSale => HasDiscount; // alias for UI convenience
    }
}
