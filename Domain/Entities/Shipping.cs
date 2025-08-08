using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Shipping
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Default"; // "Default", "Holiday Special", etc.
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        // Order Thresholds and Costs
        public decimal LowOrderThreshold { get; set; } = 300;
        public decimal LowOrderShippingCost { get; set; } = 50;
        public decimal HighOrderShippingCost { get; set; } = 100;
        public decimal FreeShippingThreshold { get; set; } = 1000;

        // Delivery Settings
        public int EstimatedDeliveryDays { get; set; } = 2;
        public double MaxDeliveryDistanceKm { get; set; } = 15;
        public bool EnableFreeShippingEvents { get; set; } = true;

        // Special Promotions
        public bool IsFreeShippingActive { get; set; } = false;
        public DateTime? FreeShippingStartDate { get; set; }
        public DateTime? FreeShippingEndDate { get; set; }
        public string FreeShippingDescription { get; set; } = string.Empty;

        // Business Rules
        public decimal MinOrderAmountForShipping { get; set; } = 0;
        public decimal MaxOrderAmountForShipping { get; set; } = 0; // 0 = unlimited
        public bool RequireLocationValidation { get; set; } = true;

        // Additional Charges
        public decimal WeekendSurcharge { get; set; } = 0;
        public decimal HolidaySurcharge { get; set; } = 0;
        public decimal RushDeliverySurcharge { get; set; } = 0;

        // Time Restrictions
        public TimeSpan? DeliveryStartTime { get; set; }
        public TimeSpan? DeliveryEndTime { get; set; }
        public string AvailableDays { get; set; } = "1,2,3,4,5,6,7"; // Days of week (1=Monday)

        // Messages
        public string CustomerMessage { get; set; } = string.Empty;
        public string AdminNotes { get; set; } = string.Empty;

        // Audit
        public int CreatedByUserId { get; set; }
        public int? LastModifiedByUserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public User CreatedByUser { get; set; } = null!;
        public User? LastModifiedByUser { get; set; }
    }
}
