using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class AddToCartItemDTO
    {
        public int ProductId { get; set; }
        //public int UserId { get; set; }
        public int Quantity { get; set; }

         //  SHIPPING INTEGRATION
        public decimal? RequestedShippingCost { get; set; }
        public int? ShippingConfigId { get; set; }
        
        //  LOCATION OVERRIDE (Optional)
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
        public bool RequestRushDelivery { get; set; } = false;
        
        //  VALIDATION
        public bool ValidateStock { get; set; } = true;
        public bool ReserveStock { get; set; } = true;
        public int ExpirationMinutes { get; set; } = 30;
    }
}
