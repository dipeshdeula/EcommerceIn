using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public record CalculateShippingForCartRequest(
        decimal OrderTotal,
        double? DeliveryLatitude = null,
        double? DeliveryLongitude = null,
        bool RequestRushDelivery = false
    );
}
