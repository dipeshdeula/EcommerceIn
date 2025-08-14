using Application.Common;
using Application.Dto.LocationDTOs;
using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface ILocationService
    {
        //  Core location validation
        Task<Result<LocationValidationResponseDTO>> ValidateLocationAsync(LocationRequestDTO request, int? userId = null, CancellationToken cancellationToken = default);
        Task<Result<LocationValidationResponseDTO>> ValidateIPLocationAsync(string ipAddress, int? userId = null, CancellationToken cancellationToken = default);
        Task<Result<LocationValidationResponseDTO>> ValidateAccessByIPAsync(string ipAddress, CancellationToken cancellationToken = default);
        Task<Result<bool>> IsLocationWithinServiceAreaAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        Task<Result<ServiceArea?>> FindServiceAreaByLocationAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        
        //  User location management
        Task<Result<Address>> SaveUserLocationAsync(int userId, LocationRequestDTO request, CancellationToken cancellationToken = default);
        Task<Result<Address?>> GetUserPrimaryLocationAsync(int userId, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateUserLocationAsync(int userId, LocationRequestDTO request, CancellationToken cancellationToken = default);
        
        //  Store discovery
        Task<Result<List<NearbyStoreDTO>>> FindNearbyStoresAsync(double latitude, double longitude, double radiusKm = 10, CancellationToken cancellationToken = default);
        Task<Result<NearbyStoreDTO?>> FindNearestStoreAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        
        //  Service area management
        Task<Result<IEnumerable<ServiceAreaDTO>>> GetAllServiceAreasAsync(int pageNumber,int pageSize,bool activeOnly = true, CancellationToken cancellationToken = default);
        Task<Result<ServiceAreaDTO?>> GetServiceAreaDetailsAsync(int serviceAreaId, CancellationToken cancellationToken = default);
        Task<Result<ServiceArea>> CreateServiceAreaAsync(ServiceArea serviceArea, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateServiceAreaAsync(ServiceArea serviceArea, CancellationToken cancellationToken = default);
        
        //  Distance calculations
        double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
        Task<Result<double>> CalculateDeliveryDistanceAsync(int storeId, double deliveryLatitude, double deliveryLongitude, CancellationToken cancellationToken = default);
        
        //  Business logic
        Task<Result<bool>> CanUserPlaceOrderAsync(int userId, CancellationToken cancellationToken = default);
        Task<Result<string>> GetLocationRestrictionMessageAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        
        //  Google Maps integration
        Task<Result<IPLocationDTO>> GetLocationFromIPAsync(string ipAddress, CancellationToken cancellationToken = default);
        Task<Result<LocationRequestDTO>> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
        Task<Result<LocationRequestDTO>> ForwardGeocodeAsync(string address, CancellationToken cancellationToken = default);
    }
}