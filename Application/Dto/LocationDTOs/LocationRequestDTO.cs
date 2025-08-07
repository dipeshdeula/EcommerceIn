using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.LocationDTOs
{
    public class LocationRequestDTO
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public double? AccuracyMeters { get; set; }

        // Ip-based location support
        public string? IPAddress { get; set; }
        public bool UserIPLocation { get; set; } = false;
    }

    public class LocationValidationResponseDTO
    {
        public bool IsServiceAvailable { get; set; }
        public bool IsWithinServiceArea { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string ServiceAreaName { get; set; } = string.Empty;
        public double DistanceFromCenterKm { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "info"; // success, warning, error, info

        // Service Information
        public bool CanPlaceOrder { get; set; }
        public bool CanViewProducts { get; set; } = true;
        public decimal MinOrderAmount { get; set; }
        public double MaxDeliveryDistanceKm { get; set; }
        public int EstimatedDeliveryDays { get; set; }

        // Location Detection
        public bool IsIPBasedLocation { get; set; }
        public string LocationSource { get; set; } = "GPS"; // "GPS", "IP", "Manual"
        public double? LocationAccuracy { get; set; }

        // Available Stores
        public List<NearbyStoreDTO> NearbyStores { get; set; } = new();

        // Coming Soon Information
        public bool IsComingSoon { get; set; }
        public string ComingSoonMessage { get; set; } = string.Empty;
        // Additional Info
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }
    public class NearbyStoreDTO
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public bool OffersDelivery { get; set; }
        public bool OffersPickup { get; set; }
        public double DeliveryRadiusKm { get; set; }
        public bool IsWithinDeliveryRadius { get; set; }
        public string FormattedDistance { get; set; } = string.Empty;
        public int EstimatedDeliveryMinutes { get; set; }
        public bool IsCurrentlyOpen {get;set;}
        public string BusinessHours { get; set; } = string.Empty;
    }
    public class ServiceAreaDTO
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsComingSoon { get; set; }
        public double RadiusKm { get; set; }
        public string Description { get; set; } = string.Empty;
        public int StoreCount { get; set; }
        public TimeSpan DeliveryStartTime { get; set; }
        public TimeSpan DeliveryEndTime { get; set; }
        public decimal MinOrderAmount {get;set;}
        public double MaxDeliveryDistancekm{ get; set; }
    }

    public class IPLocationDTO
    {
        public string IP { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ISP { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public bool IsSuccess {get;set;}
        public string ErrorMessage { get; set; } = string.Empty;
        
    }
}
