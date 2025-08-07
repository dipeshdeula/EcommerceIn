using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ServiceArea : BaseEntity
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Country { get; set; } = "Nepal";

        // Geolocation
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; } = 10.0;

        // Service Configuration
        public bool IsActive { get; set; } = true;
        public bool IsComingSoon { get; set; } = false;
        public double MaxDeliveryDistanceKm { get; set; } = 5.0;
        public decimal MinOrderAmount { get; set; } = 0;

        // Delivery Timing
        public TimeSpan DeliveryStartTime { get; set; } = new TimeSpan(9, 0, 0); // 9 AM
        public TimeSpan DeliveryEndTime { get; set; } = new TimeSpan(21, 0, 0); // 9 PM
        public int EstimatedDeliveryDays { get; set; } = 1;

        // Display Information
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NotAvailableMessage { get; set; } = "Service not available in your area yet";

        // Navigation Properties
        public ICollection<Store> Stores { get; set; } = new List<Store>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
