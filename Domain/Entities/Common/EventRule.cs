using Domain.Enums.BannerEventSpecial;

namespace Domain.Entities.Common
{
    public class EventRule 
    {
        public int Id { get; set; }
        public int BannerEventId { get; set; }

        public RuleType Type { get; set; }
        public string TargetValue { get; set; } = string.Empty; // JSON for complex rules
        public string? Conditions { get; set; } // JSON for advanced conditions

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public PromotionType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }

        // soft delete Support
        public bool IsDeleted { get; set; } = false;
        public int Priority { get; set; } = 1;

        public  BannerEventSpecial BannerEvent { get; set; } = null!;

    }
}
