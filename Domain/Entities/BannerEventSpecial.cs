using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;

namespace Domain.Entities
{
    public class BannerEventSpecial
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? TagLine { get; set; } // "Up to 70 % OFF"
        public EventType EventType { get; set; } // Seasonal, Festive, Flash , etc
        public PromotionType PromotionType { get; set; } // Percentage, Fixed, BOGO

        public double DiscountValue { get; set; } 
        public double? MaxDiscountAmount { get; set; } // Cap for percentage discounts
        public double? MinOrderValue { get; set; } // Minimum order requirement
        public int Priority { get; set; } = 1; // Higher priority events override lower ones

        // Enhanced date management
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? ActiveTimeSlot { get; set; } // For flash sales (e.g., 2PM-4PM daily)


        // Usage tracking
        public int MaxUsageCount { get; set; } = int.MaxValue; // Unlimited by default
        public int CurrentUsageCount { get; set; } = 0;
        public int MaxUsagePerUser { get; set; } = int.MaxValue;

        // Status management
        public bool IsActive { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public EventStatus Status { get; set; } = EventStatus.Draft;

        // Relationships
        public ICollection<BannerImage> Images { get; set; } = new List<BannerImage>();
        public ICollection<EventRule> Rules { get; set; } = new List<EventRule>();
        public ICollection<EventProduct> EventProducts { get; set; } = new List<EventProduct>();
        public ICollection<EventUsage> UsageHistory { get; set; } = new List<EventUsage>();

    }
}
