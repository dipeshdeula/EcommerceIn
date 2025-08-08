using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ShippingDTOs
{
    public class CreateShippingDTO

        
    {
        public string? Name { get; set; }
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

        // Additional Charges
        public decimal WeekendSurcharge { get; set; } = 0;
        public decimal HolidaySurcharge { get; set; } = 0;
        public decimal RushDeliverySurcharge { get; set; } = 0;

        // Messages
        public string CustomerMessage { get; set; } = string.Empty;
        public string AdminNotes { get; set; } = string.Empty;

        // Set as default configuration
        public bool SetAsDefault { get; set; } = false;
    }
}
