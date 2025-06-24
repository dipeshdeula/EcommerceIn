using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;

namespace Domain.Entities
{
    public class BannerEventSpecial
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TagLine { get; set; } // "Up to 70 % OFF"
        public EventType EventType { get; set; } // Seasonal, Festive, Flash , etc
        public PromotionType PromotionType { get; set; } // Percentage, Fixed, BOGO

        public decimal DiscountValue { get; set; } 
        public decimal? MaxDiscountAmount { get; set; } // Cap for percentage discounts
        public decimal? MinOrderValue { get; set; } // Minimum order requirement
        public int Priority { get; set; } = 1; // Higher priority events override lower ones

        // Enhanced date management
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? ActiveTimeSlot { get; set; } // For flash sales (e.g., 2PM-4PM daily)


        // Usage tracking
        public int MaxUsageCount { get; set; } = int.MaxValue; // Unlimited by default
        public int CurrentUsageCount { get; set; } = 0;
        public int MaxUsagePerUser { get; set; } = int.MaxValue;

        // PaymentStatus management
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public EventStatus Status { get; set; } = EventStatus.Draft;

         //  Computed property for debugging
        public bool IsTimeActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

        // Relationships
        public ICollection<BannerImage> Images { get; set; } = new List<BannerImage>();
        public ICollection<EventRule> Rules { get; set; } = new List<EventRule>();
        public ICollection<EventProduct> EventProducts { get; set; } = new List<EventProduct>();
        public ICollection<EventUsage> UsageHistory { get; set; } = new List<EventUsage>();

        // Helper methods (for internal entity logic)

        // Check if event has capacity for more usage
        public bool HasCapacity => CurrentUsageCount < MaxUsageCount;

        // check if user can use this event (simplifed, real logic in service)
        public bool CanUserUse(int currentUserUsate) => currentUserUsate < MaxUsageCount && HasCapacity;

        // Get remaining usage count
        public int RemainingUsage => Math.Max(0, MaxUsageCount - CurrentUsageCount);

        // Calculate usage percentage
        public decimal UsagePercentage => MaxUsageCount > 0
            ? Math.Round((decimal)CurrentUsageCount / MaxUsageCount * 100, 2)
            : 0;





    }
}
