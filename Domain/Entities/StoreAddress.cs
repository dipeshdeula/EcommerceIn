using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class StoreAddress
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = String.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Service configuration
        public double DeliveryRadiusKm { get; set; } = 5.0;
        public bool IsServiceActive { get; set; } = true;
        public TimeSpan ServiceStartTime { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan ServiceEndTime { get; set; } = new TimeSpan(21, 0, 0);

        //  Location verification
        public bool IsLocationVerified { get; set; } = false;
        public DateTime? LocationVerifiedAt { get; set; }

        public Store Store { get; set; }

    }
}
