using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ShippingDTOs
{
    public class ShippingDTO
    {
        public int Id { get; set; }
        public string ConfigurationName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        // Order Thresholds and Costs
        public decimal LowOrderThreshold { get; set; }
        public decimal LowOrderShippingCost { get; set; }
        public decimal HighOrderShippingCost { get; set; }
        public decimal FreeShippingThreshold { get; set; }

        // Delivery Settings
        public int EstimatedDeliveryDays { get; set; }
        public double MaxDeliveryDistanceKm { get; set; }
        public bool EnableFreeShippingEvents { get; set; }

        // Special Promotions
        public bool IsFreeShippingActive { get; set; }
        public DateTime? FreeShippingStartDate { get; set; }
        public DateTime? FreeShippingEndDate { get; set; }
        public string FreeShippingDescription { get; set; } = string.Empty;

        // Additional Charges
        public decimal WeekendSurcharge { get; set; }
        public decimal HolidaySurcharge { get; set; }
        public decimal RushDeliverySurcharge { get; set; }

        // Messages
        public string CustomerMessage { get; set; } = string.Empty;
        public string AdminNotes { get; set; } = string.Empty;

        // Audit Info
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public string? LastModifiedByUserName { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }
}
