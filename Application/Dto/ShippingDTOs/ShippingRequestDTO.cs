using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.ShippingDTOs
{
    public class ShippingRequestDTO
    {
        [JsonIgnore]
        public int UserId { get; set; }
        [JsonIgnore]
        public decimal OrderTotal { get; set; }
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool RequestRushDelivery { get; set; } = false;
        public DateTime? RequestedDeliveryDate { get; set; }

        [JsonIgnore]
        public int? PreferredConfigurationId { get; set; } = 1; // Override default config
    }
}
