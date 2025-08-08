using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ShippingDTOs
{
    public class ShippingCalculationDetailDTO
    {
        public bool IsShippingAvailable { get; set; }
        public decimal OrderSubtotal { get; set; }
        public decimal BaseShippingCost { get; set; }
        public decimal WeekendSurcharge { get; set; }
        public decimal HolidaySurcharge { get; set; }
        public decimal RushSurcharge { get; set; }
        public decimal TotalSurcharges { get; set; }
        public decimal FinalShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsFreeShipping { get; set; }
        public string ShippingReason { get; set; } = string.Empty;
        public string DeliveryEstimate { get; set; } = string.Empty;
        public string CustomerMessage { get; set; } = string.Empty;
        public List<string> AppliedPromotions { get; set; } = new();
        public List<string> AppliedSurcharges { get; set; } = new();
        public ShippingSummaryDTO Configuration { get; set; } = new();
    }
}
