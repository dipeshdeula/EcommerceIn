using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Persistence.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleMapsService> _logger;
        private readonly string _apiKey;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["LocationSettings:GoogleMapsApiKey"] ?? "";
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google Maps API key not configured");
            }
        }

        public async Task<Result<LocationRequestDTO>> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return Result<LocationRequestDTO>.Failure("Google Maps API key not configured");
                }

                var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return Result<LocationRequestDTO>.Failure($"Google Maps API returned {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var geocodeResponse = JsonSerializer.Deserialize<GoogleMapsGeocodeResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (geocodeResponse?.Status != "OK" || geocodeResponse.Results?.Any() != true)
                {
                    return Result<LocationRequestDTO>.Failure($"Geocoding failed: {geocodeResponse?.Status ?? "Unknown error"}");
                }

                var result = geocodeResponse.Results.First();
                var locationDto = ExtractLocationFromResult(result);
                locationDto.Latitude = latitude;
                locationDto.Longitude = longitude;

                _logger.LogInformation("Reverse geocoding successful: {Address}", locationDto.Address);
                
                return Result<LocationRequestDTO>.Success(locationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reverse geocoding: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
                return Result<LocationRequestDTO>.Failure($"Reverse geocoding failed: {ex.Message}");
            }
        }

        public async Task<Result<LocationRequestDTO>> ForwardGeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return Result<LocationRequestDTO>.Failure("Google Maps API key not configured");
                }

                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return Result<LocationRequestDTO>.Failure($"Google Maps API returned {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var geocodeResponse = JsonSerializer.Deserialize<GoogleMapsGeocodeResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (geocodeResponse?.Status != "OK" || geocodeResponse.Results?.Any() != true)
                {
                    return Result<LocationRequestDTO>.Failure($"Geocoding failed: {geocodeResponse?.Status ?? "Unknown error"}");
                }

                var result = geocodeResponse.Results.First();
                var locationDto = ExtractLocationFromResult(result);

                _logger.LogInformation("Forward geocoding successful: {Address} -> {Lat}, {Lon}", address, locationDto.Latitude, locationDto.Longitude);
                
                return Result<LocationRequestDTO>.Success(locationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forward geocoding: Address={Address}", address);
                return Result<LocationRequestDTO>.Failure($"Forward geocoding failed: {ex.Message}");
            }
        }

        public async Task<Result<double>> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    // Fallback to Haversine calculation
                    var distance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);
                    return Result<double>.Success(distance);
                }

                var origins = $"{lat1},{lon1}";
                var destinations = $"{lat2},{lon2}";
                var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&units=metric&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to Haversine
                    var fallbackDistance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);
                    return Result<double>.Success(fallbackDistance);
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var distanceResponse = JsonSerializer.Deserialize<GoogleMapsDistanceResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (distanceResponse?.Status == "OK" && 
                    distanceResponse.Rows?.Any() == true && 
                    distanceResponse.Rows.First().Elements?.Any() == true)
                {
                    var element = distanceResponse.Rows.First().Elements.First();
                    if (element.Status == "OK" && element.Distance != null)
                    {
                        var distanceKm = element.Distance.Value / 1000.0; // Convert meters to kilometers
                        return Result<double>.Success(distanceKm);
                    }
                }

                // Fallback to Haversine if API fails
                var haversineDistance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);
                return Result<double>.Success(haversineDistance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance via Google Maps, falling back to Haversine");
                var fallbackDistance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);
                return Result<double>.Success(fallbackDistance);
            }
        }

        public async Task<Result<bool>> ValidateAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            var geocodeResult = await ForwardGeocodeAsync(address, cancellationToken);
            return Result<bool>.Success(geocodeResult.Succeeded);
        }

        //  Private helper methods
        private static LocationRequestDTO ExtractLocationFromResult(GoogleMapsResult result)
        {
            var locationDto = new LocationRequestDTO
            {
                Address = result.FormattedAddress,
                Latitude = result.Geometry?.Location?.Lat,
                Longitude = result.Geometry?.Location?.Lng
            };

            // Extract city and province from address components
            if (result.AddressComponents?.Any() == true)
            {
                foreach (var component in result.AddressComponents)
                {
                    if (component.Types?.Contains("locality") == true)
                    {
                        locationDto.City = component.LongName;
                    }
                    else if (component.Types?.Contains("administrative_area_level_1") == true)
                    {
                        locationDto.Province = component.LongName;
                    }
                }
            }

            return locationDto;
        }

        private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        //  Google Maps API Response Models
        private class GoogleMapsGeocodeResponse
        {
            public string? Status { get; set; }
            public List<GoogleMapsResult>? Results { get; set; }
        }

        private class GoogleMapsResult
        {
            public string? FormattedAddress { get; set; }
            public GoogleMapsGeometry? Geometry { get; set; }
            public List<GoogleMapsAddressComponent>? AddressComponents { get; set; }
        }

        private class GoogleMapsGeometry
        {
            public GoogleMapsLocation? Location { get; set; }
        }

        private class GoogleMapsLocation
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        private class GoogleMapsAddressComponent
        {
            public string? LongName { get; set; }
            public string? ShortName { get; set; }
            public List<string>? Types { get; set; }
        }

        private class GoogleMapsDistanceResponse
        {
            public string? Status { get; set; }
            public List<GoogleMapsDistanceRow>? Rows { get; set; }
        }

        private class GoogleMapsDistanceRow
        {
            public List<GoogleMapsDistanceElement>? Elements { get; set; }
        }

        private class GoogleMapsDistanceElement
        {
            public string? Status { get; set; }
            public GoogleMapsDistance? Distance { get; set; }
        }

        private class GoogleMapsDistance
        {
            public string? Text { get; set; }
            public int Value { get; set; }
        }
    }
}