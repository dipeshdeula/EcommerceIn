using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Common;
using Application.Dto.LocationDTOs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class IPLocationService : IIPLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IPLocationService> _logger;
        private readonly IConfiguration _configuration;

        public IPLocationService(HttpClient httpClient, ILogger<IPLocationService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

        }
        public async Task<Result<IPLocationDTO>> GetLocationFromIPAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                 _logger.LogInformation("Getting location for IP: {IPAddress}", ipAddress);
                // skip localhost/private IPs - use default Hetauda location
                if (IsLocalOrPrivateIP(ipAddress))
                {
                    _logger.LogInformation("Local/Private IP detected, using default location");
                    return Result<IPLocationDTO>.Success(new IPLocationDTO
                    {
                        IP = ipAddress,
                        Country = "Nepal",
                        Region = "Bagmati",
                        City = "Hetauda",
                        Latitude = 27.4239,
                        Longitude = 85.0478,
                        ISP = "Local Network",
                        TimeZone = "Asia/Kathmandu",
                        IsSuccess = true
                    });
                }
                // Use ip-api.com (free service)
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}?fields=status,message,country,regionName,city,lat,lon,isp,timezone", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<IPLocationDTO>.Failure($"IP location service returned {response.StatusCode}");

                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var ipData = JsonSerializer.Deserialize<IPApiResponse>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (ipData?.Status != "success")
                {
                    return Result<IPLocationDTO>.Failure($"IP location lookup failed: {ipData?.Message ?? "Unknown error"}");
                }

                var locationDTO = new IPLocationDTO
                {
                    IP = ipAddress,
                    Country = ipData.Country ?? "",
                    Region = ipData.RegionName ?? "",
                    City = ipData.City ?? "",
                    Latitude = ipData.Lat,
                    Longitude = ipData.Lon,
                    ISP = ipData.Isp ?? "",
                    TimeZone = ipData.Timezone ?? "",
                    IsSuccess = true
                };

                _logger.LogInformation("IP location found: {City}, {Country} ({Lat}, {Lon})",
                    locationDTO.City, locationDTO.Country, locationDTO.Latitude, locationDTO.Longitude);             

                return Result<IPLocationDTO>.Success(locationDTO);

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error getting IP location for {IPAddress}", ipAddress);
                return Result<IPLocationDTO>.Failure($"Network error getting IP location: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IP location for {IPAddress}", ipAddress);
                return Result<IPLocationDTO>.Failure($"Error getting IP location: {ex.Message}");
            }
        }

        public async Task<Result<bool>> IsIPFromNepalAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            var locationResult = await GetLocationFromIPAsync(ipAddress, cancellationToken);

            if (!locationResult.Succeeded || locationResult.Data == null)
                return Result<bool>.Success(false);

            return Result<bool>.Success(locationResult.Data.Country.Equals("Nepal", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsLocalOrPrivateIP(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress)) return true;

            // Local addresses
            if (ipAddress == "127.0.0.1" || ipAddress == "::1" || ipAddress == "localhost")
                return true;

            // Private IP ranges
            if (ipAddress.StartsWith("192.168.") ||
                ipAddress.StartsWith("10.") ||
                ipAddress.StartsWith("172."))
                return true;

            return false;
        }
        
         private class IPApiResponse
        {
            public string? Status { get; set; }
            public string? Message { get; set; }
            public string? Country { get; set; }
            public string? RegionName { get; set; }
            public string? City { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string? Isp { get; set; }
            public string? Timezone { get; set; }
        }
    }
}
