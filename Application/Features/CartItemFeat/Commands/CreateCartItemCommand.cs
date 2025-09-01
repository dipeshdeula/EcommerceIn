using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Dto.ProductDTOs;
using Application.Dto.ShippingDTOs;
using Application.Extension.Cache;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Commands
{
    public record CreateCartItemCommand(
        int UserId,
        int ProductId,
        int Quantity,
        ShippingRequestDTO? ShippingRequest = null 
    ) : IRequest<Result<CartItemDTO>>;

    public class CreateCartItemCommandHandler : IRequestHandler<CreateCartItemCommand, Result<CartItemDTO>>
    {
        private readonly ICartService _cartService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<CreateCartItemCommandHandler> _logger;
        private readonly IEventUsageService _eventUsageService;
        private readonly IShippingService _shippingService;
        private readonly IProductPricingService _productPricingService;
        private readonly ILocationService _locationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;

        public CreateCartItemCommandHandler(
            ICartService cartService,
            IHybridCacheService cacheService,
            IEventUsageService eventUsageService,
            IProductPricingService productPricingService,
            IShippingService shippingService,
            ILocationService locationService,
            IUserRepository userRepository,
            ILogger<CreateCartItemCommandHandler> logger,
            IUnitOfWork unitOfWork)
        {
            _cartService = cartService;
            _cacheService = cacheService;
            _productPricingService = productPricingService;
            _eventUsageService = eventUsageService;
            _shippingService = shippingService;
            _locationService = locationService;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<CartItemDTO>> Handle(CreateCartItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(" Processing add to cart: UserId={UserId}, ProductId={ProductId}, Quantity={Quantity}",
                    request.UserId, request.ProductId, request.Quantity);

                //  STEP 1: Get product pricing (includes event discounts)
                var pricing = await _productPricingService.GetEffectivePriceAsync(request.ProductId, request.UserId, cancellationToken);
                
                //  STEP 2: Validate event usage if event is applied
                if (pricing.HasActiveEvent && pricing.ActiveEventId.HasValue)
                {
                    var validationResult = await _eventUsageService.CanUserAddQuantityToCartForProductAsync(
                        pricing.ActiveEventId.Value,
                        request.UserId,
                        request.ProductId,
                        request.Quantity
                    );

                    if (!validationResult.Succeeded)
                    {
                        _logger.LogWarning("User {UserId} cannot add {Quantity}x product {ProductId}: {Reason}",
                            request.UserId, request.Quantity, request.ProductId, validationResult.Message);
                        return Result<CartItemDTO>.Failure(validationResult.Message);
                    }
                }

                //  STEP 3: Calculate shipping cost with auto-location detection
                var shippingResult = await CalculateShippingForCartItemAsync(request, pricing, cancellationToken);
                if (!shippingResult.Succeeded)
                {
                    _logger.LogWarning("Failed to calculate shipping: {Error}", shippingResult.Message);
                    
                    return Result<CartItemDTO>.Failure("Failed to calculate shipping: " + shippingResult.Message);
                }

                var shippingCost = shippingResult.Data?.FinalShippingCost ?? 0;
                var shippingConfigId = shippingResult.Data?.Configuration?.Id ?? 1;

                

                //  STEP 4: Create cart item request with shipping
                var addToCartRequest = new AddToCartItemDTO
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    // Include shipping info in the request if your DTO supports it
                    RequestedShippingCost = shippingCost,
                    ShippingConfigId = shippingConfigId,
                    DeliveryLatitude = request.ShippingRequest?.DeliveryLatitude,
                    DeliveryLongitude = request.ShippingRequest?.DeliveryLongitude,
                    ShippingAddress = request.ShippingRequest?.Address,
                    ShippingCity = request.ShippingRequest?.City,
                    RequestRushDelivery = request.ShippingRequest?.RequestRushDelivery ?? false
                };

                //  STEP 5: Add item to cart via service
                var result = await _cartService.AddItemToCartAsync(request.UserId, addToCartRequest);

                if (result.Succeeded && result.Data != null)
                {
                    //  STEP 6: Update cart item with calculated shipping
                    await UpdateCartItemShippingAsync(result.Data.Id, shippingCost, shippingConfigId, cancellationToken);

                    //  STEP 7: Invalidate user's cart cache
                    await _cacheService.InvalidateUserCartCacheAsync(request.UserId, cancellationToken);

                    _logger.LogInformation(" Cart item added successfully: CartItemId={CartItemId}, ShippingCost=Rs.{ShippingCost}",
                        result.Data.Id, shippingCost);

                    //  STEP 8: Background events (non-blocking)
                    _ = Task.Run(async () => await PublishBackgroundEvents(request, result.Data, shippingResult.Data), cancellationToken);

                    //result.Data.ShippingInfo = shippingResult.Data;
                    //if (request.ShippingRequest != null)
                    //{
                    //    result.Data.shipping.DeliveryLatitude = request.ShippingRequest.DeliveryLatitude;
                    //    result.Data.shipping.DeliveryLongitude = request.ShippingRequest.DeliveryLongitude;
                    //    result.Data.shipping.ShippingAddress = request.ShippingRequest.Address;
                    //    result.Data.shipping.ShippingCity = request.ShippingRequest.City;
                    //    result.Data.shipping.ShippingMessage = shippingCost == 0 ? "Free shipping applied!" : $"Shipping: Rs. {shippingCost:F2}";
                    //}
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item to cart: UserId={UserId}, ProductId={ProductId}",
                    request.UserId, request.ProductId);
                return Result<CartItemDTO>.Failure($"Failed to add item to cart: {ex.Message}");
            }
        }

        /// <summary>
        ///  CALCULATE SHIPPING with automatic location detection and free shipping events
        /// </summary>
        private async Task<Result<ShippingCalculationDetailDTO>> CalculateShippingForCartItemAsync(
            CreateCartItemCommand request, 
            ProductPriceInfoDTO pricing, 
            CancellationToken cancellationToken)
        {
            try
            {

                var itemTotal = pricing.EffectivePrice * request.Quantity;

                var freeShippingEvent = await _unitOfWork.BannerEventSpecials.FirstOrDefaultAsync(
                predicate: e => e.IsActive &&
                          !e.IsDeleted &&
                          e.StartDate <= DateTime.UtcNow &&
                          e.EndDate >= DateTime.UtcNow &&
                          e.PromotionType == PromotionType.FreeShipping);


                if (freeShippingEvent != null)
                {
                    _logger.LogInformation(" Free shipping active: {EventName}", freeShippingEvent.Name);
                    return Result<ShippingCalculationDetailDTO>.Success(CreateFreeShippingResult(itemTotal, freeShippingEvent.Name));
                }

                var userLocation = await GetUserLocationAsync(request.UserId, request.ShippingRequest, cancellationToken);


                //  STEP 3: Create shipping request
                var shippingRequest = new ShippingRequestDTO
                {
                    UserId = request.UserId,
                    OrderTotal = itemTotal,
                    DeliveryLatitude = userLocation?.Latitude,
                    DeliveryLongitude = userLocation?.Longitude,
                    Address = request.ShippingRequest?.Address ?? string.Empty,
                    City = userLocation?.City ?? request.ShippingRequest?.City ?? string.Empty,
                    RequestRushDelivery = request.ShippingRequest?.RequestRushDelivery ?? false,
                    RequestedDeliveryDate = request.ShippingRequest?.RequestedDeliveryDate,
                    PreferredConfigurationId = request.ShippingRequest?.PreferredConfigurationId
                };


                var result = await _shippingService.CalculateShippingAsync(shippingRequest, cancellationToken);

                if (result.Succeeded)
                {
                    _logger.LogInformation("💰 Shipping calculated: Rs.{Cost} for order Rs.{Total}",
                        result.Data?.FinalShippingCost ?? 0, itemTotal);
                }

                return result;     
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping for cart item");
                return Result<ShippingCalculationDetailDTO>.Failure($"Shipping calculation failed: {ex.Message}");
            }
        }

        /// <summary>
        ///  GET USER LOCATION with fallback strategies
        /// </summary>
        private async Task<UserLocationInfo?> GetUserLocationAsync(int userId, ShippingRequestDTO? shippingRequest, CancellationToken cancellationToken)
        {
            try
            {
                //  Priority 1: Manual location from request
                if (shippingRequest?.DeliveryLatitude.HasValue == true && shippingRequest?.DeliveryLongitude.HasValue == true &&
                    shippingRequest?.DeliveryLatitude > 0 && shippingRequest?.DeliveryLongitude > 0)
                {
                    _logger.LogInformation(" Using manual location: Lat={Lat}, Lng={Lng}", 
                        shippingRequest.DeliveryLatitude, shippingRequest.DeliveryLongitude);
                    
                    return new UserLocationInfo
                    {
                        Latitude = shippingRequest.DeliveryLatitude.Value,
                        Longitude = shippingRequest.DeliveryLongitude.Value,
                        Source = "Manual"
                    };
                }

                //  Priority 2: User's default address
                var user = await _userRepository.GetAsync(
                    predicate: u => u.Id == userId,
                    includeProperties: "Addresses",
                    cancellationToken: cancellationToken
                );

                var defaultAddress = user?.Addresses?.FirstOrDefault(a => a.IsDefault);
                if (defaultAddress?.Latitude != null && defaultAddress?.Longitude != null && 
                    defaultAddress.Latitude != 0 && defaultAddress.Longitude != 0)
                {
                    _logger.LogInformation(" Using user's default address: {City}", defaultAddress.City);
                    
                    return new UserLocationInfo
                    {
                        Latitude = defaultAddress.Latitude,
                        Longitude = defaultAddress.Longitude,
                        City = defaultAddress.City,
                        Source = "DefaultAddress"
                    };
                }

                //  Priority 3: IP-based location (if location service supports it)
                // This would require implementing IP geolocation in your location service
                
                _logger.LogInformation("❓ No location found for user {UserId}, using default service area", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user location for userId {UserId}", userId);
                return null;
            }
        }

        
        /// <summary>
        ///  CREATE FREE SHIPPING RESULT
        /// </summary>
        private ShippingCalculationDetailDTO CreateFreeShippingResult(decimal orderTotal, string eventName)
        {
            return new ShippingCalculationDetailDTO
            {
                OrderSubtotal = orderTotal,
                IsShippingAvailable = true,
                IsFreeShipping = true,
                BaseShippingCost = 0,
                FinalShippingCost = 0,
                TotalAmount = orderTotal,
                ShippingReason = $" Free shipping event: {eventName}",
                CustomerMessage = $" Free shipping applied from {eventName}!",
                AppliedPromotions = new List<string> { $"Free Shipping Event: {eventName}" },
                DeliveryEstimate = "Standard delivery time applies"
            };
        }

        /// <summary>
        ///  UPDATE CART ITEM with calculated shipping
        /// </summary>
        private async Task UpdateCartItemShippingAsync(int cartItemId, decimal shippingCost, int shippingConfigId, CancellationToken cancellationToken)
        {
            try
            {
                var cartItem = await _unitOfWork.CartItems.GetByIdAsync(cartItemId, cancellationToken);
                if (cartItem != null)
                {
                    cartItem.ShippingCost = shippingCost;
                    cartItem.ShippingId = shippingConfigId;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                    
                    await _unitOfWork.CartItems.UpdateAsync(cartItem, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cart item {CartItemId} with shipping", cartItemId);
                // Don't fail the main operation for this
            }
        }

        /// <summary>
        ///  BACKGROUND EVENTS (Non-blocking analytics, notifications)
        /// </summary>
        private async Task PublishBackgroundEvents(CreateCartItemCommand request, CartItemDTO cartItem, ShippingCalculationDetailDTO? shippingInfo)
        {
            try
            {
                _logger.LogInformation(" Cart analytics: UserId={UserId}, ProductId={ProductId}, Price=Rs.{Price}",
                    request.UserId, request.ProductId, cartItem.ReservedPrice);

                // TODO: Implement proper background events (RabbitMQ, SignalR notifications, etc.)
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish background events for cart item: {CartItemId}", cartItem.Id);
            }
        }
    }

    //  HELPER CLASS for user location info
    public class UserLocationInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? City { get; set; }
        public string Source { get; set; } = "Unknown"; // Manual, DefaultAddress, IPLocation, etc.
    }
}