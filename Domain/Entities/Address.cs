using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Address
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Label { get; set; } // e.g. "Home", "Work"
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool IsDeleted { get; set; } = false; // Soft delete flag

        public User User { get; set; } // Navigation property to User entity
    }
}
