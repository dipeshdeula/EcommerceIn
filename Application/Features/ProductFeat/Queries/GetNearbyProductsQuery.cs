using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.ProductFeat.Queries
{
    public record GetNearbyProductsQuery(
        double? Latitude = null,           
        double? Longitude = null,          
        double RadiusKm = 5.0,           
        int Skip = 0,
        int Take = 20,
        int? UserAddressId = null,       
        bool UseUserLocation = true      
    ) : IRequest<Result<IEnumerable<NearbyProductDto>>>;

    public class GetNearbyProductQueryHandler : IRequestHandler<GetNearbyProductsQuery, Result<IEnumerable<NearbyProductDto>>>
    {
        private readonly IProductStoreRepository _productStoreRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;
        private readonly IProductPricingService _productPricingService;
        private readonly ILogger<GetNearbyProductQueryHandler> _logger;

        public GetNearbyProductQueryHandler(
            IProductStoreRepository productStoreRepository,
            ICurrentUserService currentUserService,
            IUserRepository userRepository,
            IProductPricingService productPricingService,
            ILogger<GetNearbyProductQueryHandler> logger)
        {
            _productStoreRepository = productStoreRepository;
            _currentUserService = currentUserService;
            _userRepository = userRepository;
            _productPricingService = productPricingService;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<NearbyProductDto>>> Handle(GetNearbyProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // STEP 1: Determine coordinates to use
                var coordinates = await DetermineCoordinatesAsync(request, cancellationToken);
                if (!coordinates.Succeeded)
                {
                    return Result<IEnumerable<NearbyProductDto>>.Failure(coordinates.Message);
                }

                var (latitude, longitude, locationSource) = coordinates.Data;

                _logger.LogInformation(" Searching nearby products: Lat={Latitude}, Lon={Longitude}, Radius={RadiusKm}km, Source={LocationSource}",
                    latitude, longitude, request.RadiusKm, locationSource);

                // STEP 2: Get nearby products from repository
                var nearbyProducts = await _productStoreRepository.GetNearbyProductsAsync(
                    latitude, longitude, request.RadiusKm, request.Skip, request.Take);

                if (!nearbyProducts.Any())
                {
                    return Result<IEnumerable<NearbyProductDto>>.Success(
                        new List<NearbyProductDto>(),
                        $"No products found within {request.RadiusKm}km of your location.");
                }

                // STEP 3: Apply dynamic pricing to product
                var enrichedProducts = await ApplyDynamicPricingAsync(nearbyProducts.ToList(), cancellationToken);

                _logger.LogInformation("Found {Count} products within {RadiusKm}km. Location: {LocationSource}",
                    enrichedProducts.Count, request.RadiusKm, locationSource);

                return Result<IEnumerable<NearbyProductDto>>.Success(
                    enrichedProducts,
                    $"Found {enrichedProducts.Count} products within {request.RadiusKm}km radius.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get nearby products for user {UserId}", _currentUserService.UserId);
                return Result<IEnumerable<NearbyProductDto>>.Failure($"Failed to get nearby products: {ex.Message}");
            }
        }

        /// <summary>
        /// SMART COORDINATE DETERMINATION with multiple fallbacks
        /// </summary>
        private async Task<Result<(double latitude, double longitude, string source)>> DetermineCoordinatesAsync(
            GetNearbyProductsQuery request, CancellationToken cancellationToken)
        {
            // PRIORITY 1: Manual coordinates provided (highest priority)
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                if (IsValidCoordinate(request.Latitude.Value, request.Longitude.Value))
                {
                    return Result<(double, double, string)>.Success(
                        (request.Latitude.Value, request.Longitude.Value, "Manual Input"));
                }
                return Result<(double, double, string)>.Failure("Invalid coordinates provided.");
            }

            //  PRIORITY 2: Use user location if requested and user is authenticated
            if (request.UseUserLocation && _currentUserService.IsAuthenticated)
            {
                var userCoordinates = await GetUserCoordinatesAsync(request.UserAddressId, cancellationToken);
                if (userCoordinates.Succeeded)
                {
                    return userCoordinates;
                }

                _logger.LogWarning(" User location not available for user {UserId}: {Error}",
                    _currentUserService.UserId, userCoordinates.Message);
            }

            // PRIORITY 3: Default fallback location (e.g., Hetauda city center)
            return Result<(double, double, string)>.Success((27.4287, 85.0301, "Default (Hetauda)"));
        }

        /// <summary>
        ///  GET USER COORDINATES with smart address selection
        /// </summary>
        private async Task<Result<(double latitude, double longitude, string source)>> GetUserCoordinatesAsync(
            int? specificAddressId, CancellationToken cancellationToken)
        {
            try
            {
                if (!int.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<(double, double, string)>.Failure("Invalid user ID.");
                }

                var user = await _userRepository.GetAsync(
                    predicate: u => u.Id == userId && !u.IsDeleted,
                    includeProperties: "Addresses",
                    cancellationToken: cancellationToken);

                if (user == null)
                {
                    return Result<(double, double, string)>.Failure("User not found.");
                }

                var addresses = user.Addresses?.Where(a => !a.IsDeleted).ToList();
                if (!addresses?.Any() == true)
                {
                    return Result<(double, double, string)>.Failure("User has no valid addresses.");
                }

                // SMART ADDRESS SELECTION LOGIC
                Address selectedAddress;
                string addressSource;

                if (specificAddressId.HasValue)
                {
                    // Use specific address if requested
                    selectedAddress = addresses.FirstOrDefault(a => a.Id == specificAddressId.Value);
                    if (selectedAddress == null)
                    {
                        return Result<(double, double, string)>.Failure($"Address ID {specificAddressId.Value} not found.");
                    }
                    addressSource = $"Specific Address ({selectedAddress.Label})";
                }
                else
                {
                    // Use default address, or first available
                    selectedAddress = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.First();
                    addressSource = selectedAddress.IsDefault ? $"Default Address ({selectedAddress.Label})" : $"Primary Address ({selectedAddress.Label})";
                }

                // VALIDATE COORDINATES
                if (!IsValidCoordinate(selectedAddress.Latitude, selectedAddress.Longitude))
                {
                    return Result<(double, double, string)>.Failure($"Invalid coordinates in address: {selectedAddress.Label}");
                }

                return Result<(double, double, string)>.Success(
                    (selectedAddress.Latitude, selectedAddress.Longitude, addressSource));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to get user coordinates for user {UserId}", _currentUserService.UserId);
                return Result<(double, double, string)>.Failure($"Failed to retrieve user location: {ex.Message}");
            }
        }

        /// <summary>
        /// APPLY DYNAMIC PRICING following your banner event pattern
        /// </summary>
        private async Task<List<NearbyProductDto>> ApplyDynamicPricingAsync(
            List<NearbyProductDto> products, CancellationToken cancellationToken)
        {
            if (!products.Any()) return products;

            try
            {
                // Get current user ID for personalized pricing
                var userId = _currentUserService.IsAuthenticated && int.TryParse(_currentUserService.UserId, out var uid) ? uid : (int?)null;

                //  Get product IDs for batch pricing
                var productIds = products.Select(p => p.ProductId).Distinct().ToList();

                //  Get current pricing information 
                var pricingInfos = await _productPricingService.GetEffectivePricesAsync(productIds, userId, cancellationToken);

                // Apply pricing to each product
                foreach (var product in products)
                {
                    var priceInfo = pricingInfos.FirstOrDefault(p => p.ProductId == product.ProductId);
                    if (priceInfo != null)
                    {
                        //  Update pricing with current effective pricing
                        product.CurrentPrice = priceInfo.OriginalPrice;
                        product.EffectivePrice = priceInfo.EffectivePrice;
                        product.DiscountAmount = priceInfo.OriginalPrice - priceInfo.EffectivePrice;
                        product.DiscountPercentage = priceInfo.OriginalPrice > 0
                            ? ((priceInfo.OriginalPrice - priceInfo.EffectivePrice) / priceInfo.OriginalPrice) * 100
                            : 0;
                        product.HasActiveEvent = priceInfo.AppliedEventId.HasValue;
                        product.ActiveEventName = priceInfo.AppliedEventName;

                        // Update discount status
                        product.HasDiscount = product.DiscountAmount > 0;
                        product.DiscountPrice = priceInfo.EffectivePrice;
                    }
                    else
                    {
                        // Fallback to market price if no pricing service data
                        product.CurrentPrice = product.MarketPrice;
                        product.EffectivePrice = product.MarketPrice;
                        product.DiscountAmount = 0;
                        product.DiscountPercentage = 0;
                        product.HasActiveEvent = false;
                        product.ActiveEventName = null;
                        product.HasDiscount = false;
                        product.DiscountPrice = product.MarketPrice;
                    }
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply dynamic pricing to nearby products. Using fallback pricing.");
                foreach (var product in products)
                {
                    product.CurrentPrice = product.MarketPrice;
                    product.EffectivePrice = product.MarketPrice;
                    product.DiscountAmount = 0;
                    product.DiscountPercentage = 0;
                    product.HasActiveEvent = false;
                    product.ActiveEventName = null;
                    product.HasDiscount = false;
                    product.DiscountPrice = product.MarketPrice;
                }
                return products; // Return products without dynamic pricing
            }
        }

        /// <summary>
        ///  VALIDATE COORDINATES (Nepal bounds + basic validation)
        /// </summary>
        private static bool IsValidCoordinate(double latitude, double longitude)
        {
            //  Basic coordinate validation
            if (latitude < -90 || latitude > 90) return false;
            if (longitude < -180 || longitude > 180) return false;

            // Nepal approximate bounds validation (optional)
            // Nepal: roughly 26°-31°N, 80°-89°E
            var isInNepal = latitude >= 26.0 && latitude <= 31.0 && longitude >= 80.0 && longitude <= 89.0;

            if (!isInNepal)
            {
                // Allow coordinates outside Nepal but log warning
                // You might want to adjust this based on business requirements
            }

            return true;
        }
    }
}