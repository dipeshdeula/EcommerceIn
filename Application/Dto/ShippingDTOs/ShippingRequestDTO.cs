using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ShippingDTOs
{
    public class ShippingRequestDTO
    {
        public int UserId { get; set; }
        public decimal OrderTotal { get; set; }
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
        public bool RequestRushDelivery { get; set; } = false;
        public DateTime? RequestedDeliveryDate { get; set; }
        public int? PreferredConfigurationId { get; set; } // Override default config
    }
}
