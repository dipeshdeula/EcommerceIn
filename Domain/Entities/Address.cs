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
        public string Label { get; set; } = string.Empty; // e.g. "Home", "Work"
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; } = false; // Soft delete flag

        //  Enhanced service area integration
        public int? ServiceAreaId { get; set; }
        public bool IsWithinServiceArea { get; set; } = false;
        public double? DistanceFromNearestStore { get; set; }
        public DateTime? LastValidated { get; set; }

        // Service availability 
        public bool IsServiceAvailable { get; set; } = false;
        public string? ServiceRestrictionMessage { get; set; }

        //  IP-based location tracking
        public string? DetectedFromIP { get; set; }
        public bool IsIPBasedLocation { get; set; } = false;
        public double? LocationAccuracy { get; set; }

        // Navigation properties
        public User User { get; set; } = null!; // Navigation property to User entity
        public ServiceArea? ServiceArea { get; set; }
    }
}
