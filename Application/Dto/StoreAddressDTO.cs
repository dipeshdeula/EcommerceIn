using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class StoreAddressDTO
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = String.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }
}
