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
        public double DiscountValue { get; set; }
        public double? MaxDiscountAmount { get; set; }
        public double? MinOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? ActiveTimeSlot { get; set; }
        public int? MaxUsageCount { get; set; }
        public int? MaxUsagePerUser { get; set; }
        public int? Priority { get; set; }
    }
}
