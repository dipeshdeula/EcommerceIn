using Domain.Enums.BannerEventSpecial;

namespace Domain.Entities.Common
{
    public class EventRule : BaseEntity
    {
        public int Id { get; set; }
        public int BannerEventId { get; set; }

        public RuleType Type { get; set; }
        public string TargetValue { get; set; } // JSON for complex rules
        public string? Conditions { get; set; } // JSON for advanced conditions

        public PromotionType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }

        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 1;
        public bool IsDeleted { get; set; } = false;

        public BannerEventSpecial BannerEvent { get; set; }

    }
}
