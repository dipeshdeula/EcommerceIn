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

        public Store Store { get; set; }

    }
}
