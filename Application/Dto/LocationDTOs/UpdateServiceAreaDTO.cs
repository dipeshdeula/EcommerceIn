using Application.Dto.OrderDTOs;
using Application.Dto.StoreDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.LocationDTOs
{
    public class UpdateServiceAreaDTO
    {
        
            public int Id { get; set; }
            public string? CityName { get; set; }
            public string? Province { get; set; }
            public string? Country { get; set; }
            public double? CenterLatitude { get; set; }
            public double? CenterLongitude { get; set; }
            public string? DisplayName { get; set; }
            public string? Description { get; set; }
            public string? NotAvailableMessage { get; set; }
            public bool? IsActive { get; set; } = false;
            public bool? IsComingSoon { get; set; } = false;
            public double? RadiusKm { get; set; }
            public TimeSpan? DeliveryStartTime { get; set; }
            public TimeSpan? DeliveryEndTime { get; set; }
            public int EstimatedDeliveryDays { get; set; }
            public decimal MinOrderAmount { get; set; }
            public double MaxDeliveryDistancekm { get; set; }

        
    }
}
