using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class LocationService : ILocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocationService> _logger;
        private readonly IIPLocationService _ipLocationService;
        private readonly IGoogleMapsService _googleMapsService;

        public LocationService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<LocationService> logger,
            IIPLocationService ipLocationService,
            IGoogleMapsService googleMapsService)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
            _ipLocationService = ipLocationService;
            _googleMapsService = googleMapsService;
        }

        public async Task<Result<LocationValidationResponseDTO>> ValidateLocationAsync(LocationRequestDTO request, int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating location: Lat={Latitude}, Lon={Longitude}, UserId={UserId}, UseIP={UseIP}",
                    request.Latitude, request.Longitude, userId, request.UserIPLocation);

                // STRATEGY 1: GPS Location (Most Accurate)
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    return await ValidateGPSLocationAsync(request, userId, cancellationToken);
                }

                // STRATEGY 2: IP Location (Fallback)
                if (request.UserIPLocation && !string.IsNullOrEmpty(request.IPAddress))
                {
                    var ipResult = await ValidateIPLocationAsync(request.IPAddress, userId, cancellationToken);

                    //  IP result with disclaimer
                    if (ipResult.Succeeded && ipResult.Data != null)
                    {
                        ipResult.Data.Message += " Note: Location detected from IP may not be exact. For precise service, please enable GPS.";
                        ipResult.Data.MessageType = "warning";
                        ipResult.Data.IsIPBasedLocation = true;
                        ipResult.Data.LocationAccuracy = 10000; // 10km accuracy for IP-based
                    }

                    return ipResult;
                }

                return Result<LocationValidationResponseDTO>.Failure("Location coordinates or IP address required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating location");
                return Result<LocationValidationResponseDTO>.Failure($"Location validation failed: {ex.Message}");
            }
                
        }

        private async Task<Result<LocationValidationResponseDTO>> ValidateGPSLocationAsync(LocationRequestDTO request, int? userId, CancellationToken cancellationToken)
            {
                var latitude = request.Latitude!.Value;
                var longitude = request.Longitude!.Value;

                // Find actual service area by GPS coordinates
                var serviceAreaResult = await FindServiceAreaByLocationAsync(latitude, longitude, cancellationToken);
                var serviceArea = serviceAreaResult.Data;

                var response = new LocationValidationResponseDTO();

                if (serviceArea != null)
                {
                    var distanceFromCenter = CalculateDistanceKm(
                        latitude, longitude,
                        serviceArea.CenterLatitude, serviceArea.CenterLongitude);

                    var isWithinServiceArea = distanceFromCenter <= serviceArea.RadiusKm;

                    response = new LocationValidationResponseDTO
                    {
                        IsServiceAvailable = serviceArea.IsActive && isWithinServiceArea,
                        IsWithinServiceArea = isWithinServiceArea,
                        CityName = serviceArea.CityName,
                        ServiceAreaName = serviceArea.DisplayName,
                        DistanceFromCenterKm = distanceFromCenter,
                        LocationSource = "GPS",
                        IsIPBasedLocation = false,
                        LocationAccuracy = request.AccuracyMeters,
                        CanPlaceOrder = serviceArea.IsActive && isWithinServiceArea,
                        CanViewProducts = true,
                        MinOrderAmount = serviceArea.MinOrderAmount,
                        MaxDeliveryDistanceKm = serviceArea.MaxDeliveryDistanceKm,
                        EstimatedDeliveryDays = isWithinServiceArea ? 1 : 0
                    };

                    if (isWithinServiceArea)
                    {
                        response.Message = $" GPS Location: You're in {serviceArea.CityName}! Orders and delivery available.";
                        response.MessageType = "success";
                    }
                    else
                    {
                        response.Message = $" GPS Location: You're {distanceFromCenter:F1}km from {serviceArea.CityName}. Outside delivery area.";
                        response.MessageType = "warning";
                        response.IsComingSoon = true;
                        response.ComingSoonMessage = $"We're expanding to your area soon! Currently serving within {serviceArea.RadiusKm}km of {serviceArea.CityName}.";
                    }

                    // Find nearby stores
                    var nearbyStoresResult = await FindNearbyStoresAsync(latitude, longitude, 50, cancellationToken);
                    if (nearbyStoresResult.Succeeded)
                    {
                        response.NearbyStores = nearbyStoresResult.Data ?? new List<NearbyStoreDTO>();
                    }
                }
                else
                {
                    // No service area found - use reverse geocoding to get city name
                    var geocodeResult = await _googleMapsService.ReverseGeocodeAsync(latitude, longitude, cancellationToken);
                    var detectedCity = geocodeResult.Data?.City ?? "Unknown";

                    response = new LocationValidationResponseDTO
                    {
                        IsServiceAvailable = false,
                        IsWithinServiceArea = false,
                        CityName = detectedCity,
                        ServiceAreaName = "",
                        DistanceFromCenterKm = 0,
                        Message = $" GPS Location: {detectedCity}. Service not available in your area yet.",
                        MessageType = "info",
                        LocationSource = "GPS",
                        IsIPBasedLocation = false,
                        LocationAccuracy = request.AccuracyMeters,
                        CanPlaceOrder = false,
                        CanViewProducts = true,
                        IsComingSoon = true,
                        ComingSoonMessage = $"We're planning to expand to {detectedCity}! Stay tuned for updates."
                    };
                }

                return Result<LocationValidationResponseDTO>.Success(response);
            }

        public async Task<Result<LocationValidationResponseDTO>> ValidateIPLocationAsync(string ipAddress, int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating IP location: IP={IPAddress}, UserId={UserId}", ipAddress, userId);

                // Get location from IP
                var ipLocationResult = await _ipLocationService.GetLocationFromIPAsync(ipAddress, cancellationToken);
                if (!ipLocationResult.Succeeded || ipLocationResult.Data == null)
                {
                    return Result<LocationValidationResponseDTO>.Failure($"Could not determine location from IP: {ipLocationResult.Message}");
                }

                var ipLocation = ipLocationResult.Data;

                // Check if IP is from Nepal
                if (!ipLocation.Country.Equals("Nepal", StringComparison.OrdinalIgnoreCase))
                {
                    var response = new LocationValidationResponseDTO
                    {
                        IsServiceAvailable = false,
                        IsWithinServiceArea = false,
                        CityName = ipLocation.City,
                        DistanceFromCenterKm = 0,
                        CanPlaceOrder = false,
                        CanViewProducts = true,
                        Message = $"Service is currently only available in Nepal. Detected location: {ipLocation.City}, {ipLocation.Country}",
                        MessageType = "warning",
                        IsIPBasedLocation = true,
                        LocationSource = "IP",
                        ComingSoonMessage = "We're working to expand our services internationally!"
                    };

                    return Result<LocationValidationResponseDTO>.Success(response, "IP location outside service area");
                }

                // Validate Nepal IP location using GPS validation logic
                var locationRequest = new LocationRequestDTO
                {
                    Latitude = ipLocation.Latitude,
                    Longitude = ipLocation.Longitude,
                    City = ipLocation.City,
                    Address = $"{ipLocation.City}, {ipLocation.Region}, {ipLocation.Country}",
                    IPAddress = ipAddress,
                    UserIPLocation = true
                };

                var validationResult = await ValidateLocationAsync(locationRequest, userId, cancellationToken);
                if (validationResult.Succeeded && validationResult.Data != null)
                {
                    // Mark as IP-based location
                    validationResult.Data.IsIPBasedLocation = true;
                    validationResult.Data.LocationSource = "IP";
                    validationResult.Data.AdditionalInfo["IPAddress"] = ipAddress;
                    validationResult.Data.AdditionalInfo["ISP"] = ipLocation.ISP;
                    validationResult.Data.AdditionalInfo["TimeZone"] = ipLocation.TimeZone;
                }

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating IP location: IP={IPAddress}", ipAddress);
                return Result<LocationValidationResponseDTO>.Failure($"Failed to validate IP location: {ex.Message}");
            }
        }

        public async Task<Result<LocationValidationResponseDTO>> ValidateAccessByIPAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                // Quick IP-based access validation
                var ipLocationResult = await _ipLocationService.GetLocationFromIPAsync(ipAddress, cancellationToken);
                if (!ipLocationResult.Succeeded || ipLocationResult.Data == null)
                {
                    // Allow access if can't determine IP location (fail open)
                    return Result<LocationValidationResponseDTO>.Success(new LocationValidationResponseDTO
                    {
                        IsServiceAvailable = true,
                        CanViewProducts = true,
                        CanPlaceOrder = false,
                        Message = "Location could not be determined. Limited access granted.",
                        MessageType = "warning"
                    });
                }

                var ipLocation = ipLocationResult.Data;

                // Block access if not from Nepal (or allow based on your business rules)
                if (!ipLocation.Country.Equals("Nepal", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<LocationValidationResponseDTO>.Success(new LocationValidationResponseDTO
                    {
                        IsServiceAvailable = false,
                        CanViewProducts = true, // Allow viewing products
                        CanPlaceOrder = false,  // But not placing orders
                        Message = $"Service currently limited to Nepal. Detected: {ipLocation.Country}",
                        MessageType = "info",
                        CityName = ipLocation.City,
                        ComingSoonMessage = "International service coming soon!"
                    });
                }

                // Nepal IP - allow access and provide location info
                return Result<LocationValidationResponseDTO>.Success(new LocationValidationResponseDTO
                {
                    IsServiceAvailable = true,
                    CanViewProducts = true,
                    CanPlaceOrder = true,
                    Message = $"Welcome from {ipLocation.City}, Nepal!",
                    MessageType = "success",
                    CityName = ipLocation.City,
                    IsIPBasedLocation = true,
                    LocationSource = "IP"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access by IP: {IPAddress}", ipAddress);
                // Fail open - allow access if error occurs
                return Result<LocationValidationResponseDTO>.Success(new LocationValidationResponseDTO
                {
                    IsServiceAvailable = true,
                    CanViewProducts = true,
                    CanPlaceOrder = false,
                    Message = "Access validation temporarily unavailable.",
                    MessageType = "warning"
                });
            }
        }

        public async Task<Result<IPLocationDTO>> GetLocationFromIPAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            return await _ipLocationService.GetLocationFromIPAsync(ipAddress, cancellationToken);
        }

        public async Task<Result<Address>> SaveUserLocationAsync(int userId, LocationRequestDTO request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Find service area for the location
                var serviceAreaResult = await FindServiceAreaByLocationAsync(
                    request.Latitude ?? 0, request.Longitude ?? 0, cancellationToken);
                var serviceArea = serviceAreaResult.Data;

                double? distanceFromCenter = null;
                bool isWithinServiceArea = false;

                if (serviceArea != null && request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    distanceFromCenter = CalculateDistanceKm(
                        request.Latitude.Value, request.Longitude.Value,
                        serviceArea.CenterLatitude, serviceArea.CenterLongitude);
                    
                    isWithinServiceArea = distanceFromCenter <= serviceArea.RadiusKm;
                }

                // Check if user already has a primary location
                var existingAddress = await _unitOfWork.Addresses.GetAsync(
                    predicate: a => a.UserId == userId && a.IsDefault && !a.IsDeleted,
                    cancellationToken: cancellationToken);

                Address userAddress;

                if (existingAddress != null)
                {
                    // Update existing primary address
                    existingAddress.Latitude = request.Latitude ?? existingAddress.Latitude;
                    existingAddress.Longitude = request.Longitude ?? existingAddress.Longitude;
                    existingAddress.Street = request.Address ?? existingAddress.Street;
                    existingAddress.City = request.City ?? existingAddress.City;
                    existingAddress.Province = request.Province ?? existingAddress.Province;
                    existingAddress.ServiceAreaId = serviceArea?.Id;
                    existingAddress.IsWithinServiceArea = isWithinServiceArea;
                    existingAddress.DistanceFromNearestStore = distanceFromCenter;
                    existingAddress.LastValidated = DateTime.UtcNow;
                    existingAddress.IsServiceAvailable = isWithinServiceArea && (serviceArea?.IsActive == true);
                    existingAddress.DetectedFromIP = request.IPAddress;
                    existingAddress.IsIPBasedLocation = request.UserIPLocation;
                    existingAddress.LocationAccuracy = request.AccuracyMeters;

                    await _unitOfWork.Addresses.UpdateAsync(existingAddress, cancellationToken);
                    userAddress = existingAddress;
                }
                else
                {
                    // Create new primary address
                    userAddress = new Address
                    {
                        UserId = userId,
                        Label = "Primary Location",
                        Latitude = request.Latitude ?? 0,
                        Longitude = request.Longitude ?? 0,
                        Street = request.Address ?? "",
                        City = request.City ?? "",
                        Province = request.Province ?? "",
                        ServiceAreaId = serviceArea?.Id,
                        IsWithinServiceArea = isWithinServiceArea,
                        DistanceFromNearestStore = distanceFromCenter,
                        LastValidated = DateTime.UtcNow,
                        IsServiceAvailable = isWithinServiceArea && (serviceArea?.IsActive == true),
                        DetectedFromIP = request.IPAddress,
                        IsIPBasedLocation = request.UserIPLocation,
                        LocationAccuracy = request.AccuracyMeters,
                        IsDefault = true
                    };

                    await _unitOfWork.Addresses.AddAsync(userAddress, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User location saved: UserId={UserId}, ServiceArea={ServiceArea}, WithinArea={WithinArea}",
                    userId, serviceArea?.CityName ?? "None", isWithinServiceArea);

                return Result<Address>.Success(userAddress, "User location saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user location: UserId={UserId}", userId);
                return Result<Address>.Failure($"Failed to save user location: {ex.Message}");
            }
        }

        public async Task<Result<Address?>> GetUserPrimaryLocationAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var address = await _unitOfWork.Addresses.GetAsync(
                    predicate: a => a.UserId == userId && a.IsDefault && !a.IsDeleted,
                    includeProperties: "ServiceArea",
                    cancellationToken: cancellationToken);

                return Result<Address?>.Success(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user primary location: UserId={UserId}", userId);
                return Result<Address?>.Failure($"Failed to get user location: {ex.Message}");
            }
        }

        // Implement remaining interface methods...
        public async Task<Result<bool>> IsLocationWithinServiceAreaAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            var serviceAreaResult = await FindServiceAreaByLocationAsync(latitude, longitude, cancellationToken);
            
            if (!serviceAreaResult.Succeeded || serviceAreaResult.Data == null)
                return Result<bool>.Success(false);

            var serviceArea = serviceAreaResult.Data;
            var distance = CalculateDistanceKm(latitude, longitude, serviceArea.CenterLatitude, serviceArea.CenterLongitude);
            
            return Result<bool>.Success(serviceArea.IsActive && distance <= serviceArea.RadiusKm);
        }

        public async Task<Result<ServiceArea?>> FindServiceAreaByLocationAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceAreas = await _unitOfWork.ServiceAreas.GetAllAsync(
                    predicate: sa => !sa.IsDeleted,
                    cancellationToken: cancellationToken);

                ServiceArea? closestServiceArea = null;
                double closestDistance = double.MaxValue;

                foreach (var area in serviceAreas)
                {
                    var distance = CalculateDistanceKm(latitude, longitude, area.CenterLatitude, area.CenterLongitude);
                    
                    if (distance <= area.RadiusKm && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestServiceArea = area;
                    }
                }

                return Result<ServiceArea?>.Success(closestServiceArea);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding service area for location: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
                return Result<ServiceArea?>.Failure($"Failed to find service area: {ex.Message}");
            }
        }

        public async Task<Result<List<NearbyStoreDTO>>> FindNearbyStoresAsync(double latitude, double longitude, double radiusKm = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var stores = await _unitOfWork.Stores.GetAllAsync(
                    predicate: s => !s.IsDeleted,
                    includeProperties: "Address",
                    cancellationToken: cancellationToken);

                var nearbyStores = new List<NearbyStoreDTO>();

                foreach (var store in stores)
                {
                    if (store.Address == null) continue;

                    var distance = CalculateDistanceKm(latitude, longitude, store.Address.Latitude, store.Address.Longitude);
                    
                    if (distance <= radiusKm)
                    {
                        var isWithinDeliveryRadius = distance <= store.Address.DeliveryRadiusKm;
                        var isCurrentlyOpen = IsStoreOpen(store.Address);
                        
                        nearbyStores.Add(new NearbyStoreDTO
                        {
                            StoreId = store.Id,
                            StoreName = store.Name,
                            OwnerName = store.OwnerName,
                            Address = FormatStoreAddress(store.Address),
                            DistanceKm = Math.Round(distance, 2),
                            OffersDelivery = true, // From StoreAddress
                            OffersPickup = true,   // From StoreAddress
                            DeliveryRadiusKm = store.Address.DeliveryRadiusKm,
                            IsWithinDeliveryRadius = isWithinDeliveryRadius,
                            FormattedDistance = $"{distance:F1} km",
                            EstimatedDeliveryMinutes = CalculateEstimatedDeliveryTime(distance),
                            IsCurrentlyOpen = isCurrentlyOpen,
                            BusinessHours = $"{store.Address.ServiceStartTime:hh\\:mm} - {store.Address.ServiceEndTime:hh\\:mm}"
                        });
                    }
                }

                // Sort by distance
                nearbyStores = nearbyStores.OrderBy(s => s.DistanceKm).ToList();

                _logger.LogInformation("Found {StoreCount} nearby stores within {Radius}km of location", 
                    nearbyStores.Count, radiusKm);

                return Result<List<NearbyStoreDTO>>.Success(nearbyStores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding nearby stores for location: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
                return Result<List<NearbyStoreDTO>>.Failure($"Failed to find nearby stores: {ex.Message}");
            }
        }

        public async Task<Result<NearbyStoreDTO?>> FindNearestStoreAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            var nearbyStoresResult = await FindNearbyStoresAsync(latitude, longitude, 50, cancellationToken);
            
            if (!nearbyStoresResult.Succeeded || !nearbyStoresResult.Data?.Any() == true)
                return Result<NearbyStoreDTO?>.Success(null, "No nearby stores found");

            var nearestStore = nearbyStoresResult.Data.FirstOrDefault();
            return Result<NearbyStoreDTO?>.Success(nearestStore, "Nearest store found");
        }

        // Continue implementing remaining methods...
        public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
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

        // Google Maps integration methods
        public async Task<Result<LocationRequestDTO>> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await _googleMapsService.ReverseGeocodeAsync(latitude, longitude, cancellationToken);
        }

        public async Task<Result<LocationRequestDTO>> ForwardGeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            return await _googleMapsService.ForwardGeocodeAsync(address, cancellationToken);
        }

        // Additional methods implementation continued...
        public async Task<Result<List<ServiceAreaDTO>>> GetAllServiceAreasAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceAreas = await _unitOfWork.ServiceAreas.GetAllAsync(
                    predicate: sa => !sa.IsDeleted && (!activeOnly || sa.IsActive),
                    includeProperties: "Stores",
                    cancellationToken: cancellationToken);

                var serviceAreaDTOs = serviceAreas.Select(sa => new ServiceAreaDTO
                {
                    Id = sa.Id,
                    CityName = sa.CityName,
                    DisplayName = sa.DisplayName,
                    Province = sa.Province,
                    IsActive = sa.IsActive,
                    IsComingSoon = sa.IsComingSoon,
                    RadiusKm = sa.RadiusKm,
                    Description = sa.Description,
                    StoreCount = sa.Stores?.Count(s => !s.IsDeleted) ?? 0,
                    DeliveryStartTime = sa.DeliveryStartTime,
                    DeliveryEndTime = sa.DeliveryEndTime,
                    MinOrderAmount = sa.MinOrderAmount,
                    MaxDeliveryDistancekm = sa.MaxDeliveryDistanceKm
                }).ToList();

                return Result<List<ServiceAreaDTO>>.Success(serviceAreaDTOs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service areas");
                return Result<List<ServiceAreaDTO>>.Failure($"Failed to get service areas: {ex.Message}");
            }
        }

        public async Task<Result<ServiceAreaDTO?>> GetServiceAreaDetailsAsync(int serviceAreaId, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceArea = await _unitOfWork.ServiceAreas.GetAsync(
                    predicate: sa => sa.Id == serviceAreaId && !sa.IsDeleted,
                    includeProperties: "Stores",
                    cancellationToken: cancellationToken);

                if (serviceArea == null)
                    return Result<ServiceAreaDTO?>.Success(null, "Service area not found");

                var serviceAreaDTO = new ServiceAreaDTO
                {
                    Id = serviceArea.Id,
                    CityName = serviceArea.CityName,
                    DisplayName = serviceArea.DisplayName,
                    Province = serviceArea.Province,
                    IsActive = serviceArea.IsActive,
                    IsComingSoon = serviceArea.IsComingSoon,
                    RadiusKm = serviceArea.RadiusKm,
                    Description = serviceArea.Description,
                    StoreCount = serviceArea.Stores?.Count(s => !s.IsDeleted) ?? 0,
                    DeliveryStartTime = serviceArea.DeliveryStartTime,
                    DeliveryEndTime = serviceArea.DeliveryEndTime,
                    MinOrderAmount = serviceArea.MinOrderAmount,
                    MaxDeliveryDistancekm = serviceArea.MaxDeliveryDistanceKm
                };

                return Result<ServiceAreaDTO?>.Success(serviceAreaDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service area details: ServiceAreaId={ServiceAreaId}", serviceAreaId);
                return Result<ServiceAreaDTO?>.Failure($"Failed to get service area details: {ex.Message}");
            }
        }

        public async Task<Result<ServiceArea>> CreateServiceAreaAsync(ServiceArea serviceArea, CancellationToken cancellationToken = default)
        {
            try
            {
                await _unitOfWork.ServiceAreas.AddAsync(serviceArea, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Service area created: {CityName}", serviceArea.CityName);
                return Result<ServiceArea>.Success(serviceArea, "Service area created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service area: {CityName}", serviceArea.CityName);
                return Result<ServiceArea>.Failure($"Failed to create service area: {ex.Message}");
            }
        }


       
        public async Task<Result<bool>> UpdateServiceAreaAsync(ServiceArea serviceArea, CancellationToken cancellationToken = default)
        {
            try
            {
                await _unitOfWork.ServiceAreas.UpdateAsync(serviceArea, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Service area updated: {CityName}", serviceArea.CityName);
                return Result<bool>.Success(true, "Service area updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service area: {Id}", serviceArea.Id);
                return Result<bool>.Failure($"Failed to update service area: {ex.Message}");
            }
        }

       
        public async Task<Result<double>> CalculateDeliveryDistanceAsync(int storeId, double deliveryLatitude, double deliveryLongitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var store = await _unitOfWork.Stores.GetAsync(
                    predicate: s => s.Id == storeId && !s.IsDeleted,
                    includeProperties: "Address",
                    cancellationToken: cancellationToken);

                if (store?.Address == null)
                    return Result<double>.Failure("Store or store address not found");

                var distance = CalculateDistanceKm(
                    store.Address.Latitude, store.Address.Longitude,
                    deliveryLatitude, deliveryLongitude);

                return Result<double>.Success(distance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery distance: StoreId={StoreId}", storeId);
                return Result<double>.Failure($"Failed to calculate delivery distance: {ex.Message}");
            }
        }

       public async Task<Result<bool>> CanUserPlaceOrderAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userLocationResult = await GetUserPrimaryLocationAsync(userId, cancellationToken);
                
                if (!userLocationResult.Succeeded || userLocationResult.Data == null)
                    return Result<bool>.Success(false, "User location not available");

                var userLocation = userLocationResult.Data;
                return Result<bool>.Success(userLocation.IsWithinServiceArea && userLocation.IsServiceAvailable, 
                    userLocation.IsWithinServiceArea ? "User can place orders" : "User outside service area");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can place order: UserId={UserId}", userId);
                return Result<bool>.Success(false, "Error checking order eligibility");
            }
        }

       public async Task<Result<string>> GetLocationRestrictionMessageAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            var validationResult = await ValidateLocationAsync(new LocationRequestDTO 
            { 
                Latitude = latitude, 
                Longitude = longitude 
            }, null, cancellationToken);

            if (!validationResult.Succeeded)
                return Result<string>.Success("Location validation failed");

            return Result<string>.Success(validationResult.Data!.Message);
        }


      
        public async Task<Result<bool>> UpdateUserLocationAsync(int userId, LocationRequestDTO request, CancellationToken cancellationToken = default)
        {
            var result = await SaveUserLocationAsync(userId, request, cancellationToken);
            return Result<bool>.Success(result.Succeeded, result.Message);
        }

        // Private helper methods
        private static void SetLocationMessage(LocationValidationResponseDTO response, ServiceArea? serviceArea, bool isWithinServiceArea, double distanceFromCenter)
        {
            if (serviceArea == null)
            {
                response.Message = "Service not available in your area yet. We're working to expand our coverage!";
                response.MessageType = "warning";
                response.ComingSoonMessage = "Stay tuned! We'll notify you when we launch in your city.";
            }
            else if (!serviceArea.IsActive)
            {
                response.Message = serviceArea.IsComingSoon 
                    ? $"We're launching in {serviceArea.CityName} soon! You can browse products but orders are not available yet."
                    : serviceArea.NotAvailableMessage;
                response.MessageType = serviceArea.IsComingSoon ? "info" : "warning";
                response.ComingSoonMessage = serviceArea.IsComingSoon 
                    ? $"Get ready! {serviceArea.CityName} service launching soon."
                    : "";
            }
            else if (!isWithinServiceArea)
            {
                response.Message = $"You're outside our {serviceArea.CityName} service area ({distanceFromCenter:F1}km from center). " +
                                 $"We deliver within {serviceArea.RadiusKm}km radius.";
                response.MessageType = "warning";
            }
            else
            {
                response.Message = $"Great! We deliver to your location in {serviceArea.CityName}. " +
                                 $"{response.NearbyStores.Count} stores nearby.";
                response.MessageType = "success";
            }
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        private static string FormatStoreAddress(StoreAddress? address)
        {
            if (address == null) return "Address not available";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(address.Street)) parts.Add(address.Street);
            if (!string.IsNullOrEmpty(address.City)) parts.Add(address.City);

            return parts.Any() ? string.Join(", ", parts) : "Address not available";
        }

        private static int CalculateEstimatedDeliveryTime(double distanceKm)
        {
            // Estimate delivery time based on distance
            // Base time: 30 minutes + 5 minutes per km
            return (int)(30 + (distanceKm * 5));
        }

        private static bool IsStoreOpen(StoreAddress address)
        {
            var now = DateTime.Now.TimeOfDay;
            return now >= address.ServiceStartTime && now <= address.ServiceEndTime;
        }
    }
}