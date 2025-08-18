using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums.BannerEventSpecial;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Persistence.Services
{
    public class ShippingService : IShippingService
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILocationService _locationService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ShippingService> _logger;

        public ShippingService(
            IShippingRepository shippingRepository,
            ILocationService locationService,
            IDistributedCache cache,
            IUnitOfWork unitOfwork,
            ILogger<ShippingService> logger)
        {
            _shippingRepository = shippingRepository;
            _locationService = locationService;
            _unitOfWork = unitOfwork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<ShippingCalculationDetailDTO>> CalculateShippingAsync(
            ShippingRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Calculating shipping for UserId: {UserId}, OrderTotal: ₨{OrderTotal}",
                    request.UserId, request.OrderTotal);

                // check for active free shipping banner event
                var freeShippingEvent = await CheckActiveFreeShippingBannerEventsAsync(cancellationToken);
                if (freeShippingEvent != null)
                {

                    _logger.LogInformation("Free shipping banner event active: {EventName}", freeShippingEvent.Name);
                    return Result<ShippingCalculationDetailDTO>.Success(CreateFreeShippingEventResult(request.OrderTotal, freeShippingEvent));
                }

                // Get active configuration
                var activeConfigResult = await GetActiveConfigurationInternalAsync();
                if (!activeConfigResult.Succeeded || activeConfigResult.Data == null)
                {
                    return Result<ShippingCalculationDetailDTO>.Failure("No active shipping configuration found");
                }

                var config = activeConfigResult.Data;
                var result = new ShippingCalculationDetailDTO
                {
                    OrderSubtotal = request.OrderTotal,
                    IsShippingAvailable = true,
                    Configuration = new ShippingSummaryDTO
                    {
                        Id = config.Id,
                        Name = config.Name,
                        IsFreeShippingActive = config.IsFreeShippingActive,
                        FreeShippingDescription = config.FreeShippingDescription
                    }
                };

                // Validate delivery location if coordinates provided
                if (config.RequireLocationValidation &&
                    request.DeliveryLatitude.HasValue &&
                    request.DeliveryLongitude.HasValue)
                {
                    var locationValidation = await _locationService.ValidateLocationAsync(new LocationRequestDTO
                    {
                        Latitude = request.DeliveryLatitude,
                        Longitude = request.DeliveryLongitude,
                        UserIPLocation = false
                    }, request.UserId, cancellationToken);

                    if (!locationValidation.Succeeded ||
                        locationValidation.Data?.IsWithinServiceArea != true)
                    {
                        result.CustomerMessage = "Delivery not available in your area";
                        return Result<ShippingCalculationDetailDTO>.Success(result);
                    }
                }

                result.IsShippingAvailable = true;

                // Calculate base shipping cost
                decimal baseShippingCost = 0;
                string shippingReason = "";

                // Check for active free shipping promotion
                bool isFreeShippingPromoActive = config.IsFreeShippingActive &&
                    (!config.FreeShippingStartDate.HasValue || DateTime.UtcNow >= config.FreeShippingStartDate) &&
                    (!config.FreeShippingEndDate.HasValue || DateTime.UtcNow <= config.FreeShippingEndDate);

                if (isFreeShippingPromoActive)
                {
                    baseShippingCost = 0;
                    shippingReason = string.IsNullOrEmpty(config.FreeShippingDescription) ?
                        "Free shipping promotion active! " : config.FreeShippingDescription;
                    result.IsFreeShipping = true;
                    result.AppliedPromotions.Add("Free Shipping Event");
                }
                // Check for free shipping threshold
                else if (config.FreeShippingThreshold > 0 && request.OrderTotal >= config.FreeShippingThreshold)
                {
                    baseShippingCost = 0;
                    shippingReason = $"Free shipping for orders over ₨{config.FreeShippingThreshold}";
                    result.IsFreeShipping = true;
                    result.AppliedPromotions.Add($"Free Shipping Threshold (₨{config.FreeShippingThreshold}+)");
                }
                // Apply dynamic pricing rules
                else if (request.OrderTotal < config.LowOrderThreshold)
                {
                    baseShippingCost = config.LowOrderShippingCost;
                    shippingReason = $"Standard shipping (order under ₨{config.LowOrderThreshold})";
                }
                else
                {
                    baseShippingCost = config.HighOrderShippingCost;
                    shippingReason = $"Standard shipping (order ₨{config.LowOrderThreshold}+)";
                }

                result.BaseShippingCost = baseShippingCost;
                result.ShippingReason = shippingReason;

                // Calculate surcharges
                decimal totalSurcharges = 0;
                var appliedSurcharges = new List<string>();

                // Weekend surcharge
                if (config.WeekendSurcharge > 0 && IsWeekend())
                {
                    result.WeekendSurcharge = config.WeekendSurcharge;
                    totalSurcharges += config.WeekendSurcharge;
                    appliedSurcharges.Add($"Weekend Delivery (+₨{config.WeekendSurcharge})");
                }

                // Holiday surcharge
                if (config.HolidaySurcharge > 0 && IsHoliday())
                {
                    result.HolidaySurcharge = config.HolidaySurcharge;
                    totalSurcharges += config.HolidaySurcharge;
                    appliedSurcharges.Add($"Holiday Delivery (+₨{config.HolidaySurcharge})");
                }

                // Rush delivery surcharge
                if (request.RequestRushDelivery && config.RushDeliverySurcharge > 0)
                {
                    result.RushSurcharge = config.RushDeliverySurcharge;
                    totalSurcharges += config.RushDeliverySurcharge;
                    appliedSurcharges.Add($"Rush Delivery (+₨{config.RushDeliverySurcharge})");
                }

                result.TotalSurcharges = totalSurcharges;
                result.AppliedSurcharges = appliedSurcharges;

                // Calculate final shipping cost
                var finalShippingCost = result.IsFreeShipping ? totalSurcharges : baseShippingCost + totalSurcharges;
                result.FinalShippingCost = finalShippingCost;
                result.TotalAmount = request.OrderTotal + finalShippingCost;

                // Set delivery estimate
                var deliveryDays = config.EstimatedDeliveryDays;
                if (request.RequestRushDelivery && deliveryDays > 1)
                {
                    deliveryDays = Math.Max(1, deliveryDays - 1);
                }
                result.DeliveryEstimate = deliveryDays == 1 ? "Next day delivery" : $"Delivery in {deliveryDays} days";

                // Set customer message
                result.CustomerMessage = string.IsNullOrEmpty(config.CustomerMessage) ?
                    (result.IsFreeShipping ? " Free shipping applied!" : $" Shipping: ₨{finalShippingCost}") :
                    config.CustomerMessage;

                _logger.LogInformation("Shipping calculated: ₨{ShippingCost} for order ₨{OrderTotal}",
                    finalShippingCost, request.OrderTotal);

                return Result<ShippingCalculationDetailDTO>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping for order total ₨{OrderTotal}", request.OrderTotal);
                return Result<ShippingCalculationDetailDTO>.Failure($"Error calculating shipping: {ex.Message}");
            }
        }
        
        /// <summary>
        ///  CREATE FREE SHIPPING EVENT RESULT
        /// </summary>
        private ShippingCalculationDetailDTO CreateFreeShippingEventResult(decimal orderTotal, BannerEventSpecial freeShippingEvent)
        {
            return new ShippingCalculationDetailDTO
            {
                OrderSubtotal = orderTotal,
                IsShippingAvailable = true,
                IsFreeShipping = true,
                BaseShippingCost = 0,
                FinalShippingCost = 0,
                TotalAmount = orderTotal,
                ShippingReason = $" Free shipping event: {freeShippingEvent.Name}",
                CustomerMessage = $" Free shipping applied from {freeShippingEvent.Name}!",
                AppliedPromotions = new List<string> { $"Free Shipping Event: {freeShippingEvent.Name}" },
                DeliveryEstimate = "Standard delivery time applies",
                Configuration = new ShippingSummaryDTO
                {
                    Id = 0, // Event-based, not config-based
                    Name = $"Event: {freeShippingEvent.Name}",
                    IsFreeShippingActive = true,
                    FreeShippingDescription = freeShippingEvent.Name
                }
            };
        }

         /// <summary>
        ///  CHECK FOR ACTIVE FREE SHIPPING BANNER EVENTS
        /// </summary>
        private async Task<BannerEventSpecial?> CheckActiveFreeShippingBannerEventsAsync(CancellationToken cancellationToken)
        {
            try
            {               
                var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                    predicate: e => e.IsActive &&
                                  !e.IsDeleted &&
                                  e.StartDate <= DateTime.UtcNow &&
                                  e.EndDate >= DateTime.UtcNow &&
                                  e.PromotionType == PromotionType.FreeShipping,                    
                    cancellationToken: cancellationToken
                );

                 var result = activeEvents.FirstOrDefault();
        
                if (result != null)
                {
                    _logger.LogInformation(" Found active free shipping event: {EventName} (Id: {EventId})", 
                        result.Name, result.Id);
                }
                else
                {
                    _logger.LogDebug("No active free shipping events found");
                }

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for free shipping banner events");
                return null;
            }
        }


        public async Task<Result<List<ShippingDTO>>> GetAllsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                //  Using your existing GetWithIncludesAsync method with proper parameters
                var configurations = await _shippingRepository.GetWithIncludesAsync(
                    predicate: s => !s.IsDeleted,
                    orderBy: q => q.OrderByDescending(s => s.IsDefault).ThenBy(s => s.Name),
                    s => s.CreatedByUser,
                    s => s.LastModifiedByUser!
                );

                var result = configurations.Select(c => c.ToShippingDTO()).ToList();
                return Result<List<ShippingDTO>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all shipping configurations");
                return Result<List<ShippingDTO>>.Failure($"Error retrieving configurations: {ex.Message}");
            }
        }

        public async Task<Result<ShippingDTO>> GetByIdAsync(int Id, CancellationToken cancellationToken = default)
        {
            try
            {
                //  Using your existing GetAsync method properly
                var configuration = await _shippingRepository.GetAsync(
                    predicate: s => s.Id == Id && !s.IsDeleted,
                    includeProperties: "CreatedByUser,LastModifiedByUser",
                    includeDeleted: false,
                    cancellationToken: cancellationToken
                );

                if (configuration == null)
                {
                    return Result<ShippingDTO>.Failure("Shipping configuration not found");
                }

                var result = configuration.ToShippingDTO();
                return Result<ShippingDTO>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping configuration {Id}", Id);
                return Result<ShippingDTO>.Failure($"Error retrieving configuration: {ex.Message}");
            }
        }

        public async Task<Result<ShippingDTO>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetActiveConfigurationInternalAsync();
            if (!result.Succeeded || result.Data == null)
            {
                return Result<ShippingDTO>.Failure("No active shipping configuration found");
            }

            return Result<ShippingDTO>.Success(result.Data.ToShippingDTO());
        }

        public async Task<Result<ShippingDTO>> CreateAsync(
            CreateShippingDTO request,
            int createdByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // If setting as default, deactivate current default
                if (request.SetAsDefault)
                {
                    //  Using your existing GetAllAsync method
                    var currentDefaults = await _shippingRepository.GetAllAsync(
                        predicate: c => c.IsDefault && c.IsActive && !c.IsDeleted,
                        includeDeleted: false
                    );

                    foreach (var defaultConfig in currentDefaults)
                    {
                        defaultConfig.IsDefault = false;
                        await _shippingRepository.UpdateAsync(defaultConfig);
                    }
                }

                var configuration = new Shipping
                {
                    Name = request.Name!,
                    IsActive = true,
                    IsDefault = request.SetAsDefault,
                    LowOrderThreshold = request.LowOrderThreshold,
                    LowOrderShippingCost = request.LowOrderShippingCost,
                    HighOrderShippingCost = request.HighOrderShippingCost,
                    FreeShippingThreshold = request.FreeShippingThreshold,
                    EstimatedDeliveryDays = request.EstimatedDeliveryDays,
                    MaxDeliveryDistanceKm = request.MaxDeliveryDistanceKm,
                    EnableFreeShippingEvents = request.EnableFreeShippingEvents,
                    IsFreeShippingActive = request.IsFreeShippingActive,
                    FreeShippingStartDate = request.FreeShippingStartDate,
                    FreeShippingEndDate = request.FreeShippingEndDate,
                    FreeShippingDescription = request.FreeShippingDescription,
                    WeekendSurcharge = request.WeekendSurcharge,
                    HolidaySurcharge = request.HolidaySurcharge,
                    RushDeliverySurcharge = request.RushDeliverySurcharge,
                    CustomerMessage = request.CustomerMessage,
                    AdminNotes = request.AdminNotes,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RequireLocationValidation = true
                };

                await _shippingRepository.AddAsync(configuration, cancellationToken);
                //  Save changes explicitly
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                var result = configuration.ToShippingDTO();
                _logger.LogInformation("Created shipping configuration '{ConfigName}' by user {UserId}",
                    request.Name, createdByUserId);

                return Result<ShippingDTO>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping configuration '{ConfigName}'", request.Name);
                return Result<ShippingDTO>.Failure($"Error creating configuration: {ex.Message}");
            }
        }

        public async Task<Result<ShippingDTO>> UpdateAsync(
            int Id,
            CreateShippingDTO request,
            int modifiedByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(Id, cancellationToken);
                if (configuration == null || configuration.IsDeleted)
                {
                    return Result<ShippingDTO>.Failure("Shipping configuration not found");
                }

                // If setting as default, deactivate current default
                if (request.SetAsDefault && !configuration.IsDefault)
                {
                    var currentDefaults = await _shippingRepository.GetAllAsync(
                        predicate: c => c.IsDefault && c.IsActive && !c.IsDeleted && c.Id != Id,
                        includeDeleted: false
                    );

                    foreach (var defaultConfig in currentDefaults)
                    {
                        defaultConfig.IsDefault = false;
                        await _shippingRepository.UpdateAsync(defaultConfig, cancellationToken);
                    }
                }

                // Update configuration
                configuration.Name = request.Name!;
                configuration.IsDefault = request.SetAsDefault;
                configuration.LowOrderThreshold = request.LowOrderThreshold;
                configuration.LowOrderShippingCost = request.LowOrderShippingCost;
                configuration.HighOrderShippingCost = request.HighOrderShippingCost;
                configuration.FreeShippingThreshold = request.FreeShippingThreshold;
                configuration.EstimatedDeliveryDays = request.EstimatedDeliveryDays;
                configuration.MaxDeliveryDistanceKm = request.MaxDeliveryDistanceKm;
                configuration.EnableFreeShippingEvents = request.EnableFreeShippingEvents;
                configuration.IsFreeShippingActive = request.IsFreeShippingActive;
                configuration.FreeShippingStartDate = request.FreeShippingStartDate;
                configuration.FreeShippingEndDate = request.FreeShippingEndDate;
                configuration.FreeShippingDescription = request.FreeShippingDescription;
                configuration.WeekendSurcharge = request.WeekendSurcharge;
                configuration.HolidaySurcharge = request.HolidaySurcharge;
                configuration.RushDeliverySurcharge = request.RushDeliverySurcharge;
                configuration.CustomerMessage = request.CustomerMessage;
                configuration.AdminNotes = request.AdminNotes;
                configuration.LastModifiedByUserId = modifiedByUserId;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                //  Save changes explicitly
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                var result = configuration.ToShippingDTO();
                _logger.LogInformation("Updated shipping configuration {Id} by user {UserId}", Id, modifiedByUserId);

                return Result<ShippingDTO>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping configuration {Id}", Id);
                return Result<ShippingDTO>.Failure($"Error updating configuration: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SetDefaultAsync(int Id, int modifiedByUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Remove default from all others
                var allConfigurations = await _shippingRepository.GetAllAsync(
                    predicate: c => c.IsActive && !c.IsDeleted,
                    includeDeleted: false
                );

                foreach (var config in allConfigurations)
                {
                    config.IsDefault = config.Id == Id;
                    config.LastModifiedByUserId = modifiedByUserId;
                    config.UpdatedAt = DateTime.UtcNow;
                    await _shippingRepository.UpdateAsync(config, cancellationToken);
                }

                //  Save changes explicitly
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                _logger.LogInformation("Set shipping configuration {Id} as default by user {UserId}", Id, modifiedByUserId);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default shipping configuration {Id}", Id);
                return Result<bool>.Failure($"Error setting default: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ActivateAsync(int Id, int modifiedByUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(Id, cancellationToken);
                if (configuration == null || configuration.IsDeleted)
                {
                    return Result<bool>.Failure("Configuration not found");
                }

                configuration.IsActive = true;
                configuration.CreatedAt = DateTime.UtcNow;
                configuration.LastModifiedByUserId = modifiedByUserId;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating shipping configuration {Id}", Id);
                return Result<bool>.Failure($"Error activating configuration: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeactivateAsync(int Id, int modifiedByUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(Id, cancellationToken);
                if (configuration == null || configuration.IsDeleted)
                {
                    return Result<bool>.Failure("Configuration not found");
                }

                configuration.IsActive = false;
                configuration.UpdatedAt = DateTime.UtcNow;
                configuration.LastModifiedByUserId = modifiedByUserId;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating shipping configuration {Id}", Id);
                return Result<bool>.Failure($"Error deactivating configuration: {ex.Message}");
            }
        }

        public async Task<Result<bool>> EnableFreeShippingPromotionAsync(
            int Id,
            DateTime startDate,
            DateTime endDate,
            string description,
            int modifiedByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(Id, cancellationToken);
                if (configuration == null || configuration.IsDeleted)
                {
                    return Result<bool>.Failure("Configuration not found");
                }

                configuration.IsFreeShippingActive = true;
                configuration.FreeShippingStartDate = startDate;
                configuration.FreeShippingEndDate = endDate;
                configuration.FreeShippingDescription = description;
                configuration.LastModifiedByUserId = modifiedByUserId;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling free shipping promotion for configuration {Id}", Id);
                return Result<bool>.Failure($"Error enabling promotion: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DisableFreeShippingPromotionAsync(
            int Id,
            int modifiedByUserId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(Id, cancellationToken);
                if (configuration == null || configuration.IsDeleted)
                {
                    return Result<bool>.Failure("Configuration not found");
                }

                configuration.IsFreeShippingActive = false;
                configuration.FreeShippingStartDate = null;
                configuration.FreeShippingEndDate = null;
                configuration.FreeShippingDescription = string.Empty;
                configuration.LastModifiedByUserId = modifiedByUserId;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling free shipping promotion for configuration {Id}", Id);
                return Result<bool>.Failure($"Error disabling promotion: {ex.Message}");
            }
        }

        public async Task<Result<object>> GetShippingInfoForCustomersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var activeConfigResult = await GetActiveConfigurationInternalAsync();
                if (!activeConfigResult.Succeeded || activeConfigResult.Data == null)
                {
                    return Result<object>.Success(new
                    {
                        isShippingAvailable = false,
                        message = "Shipping temporarily unavailable"
                    });
                }

                var config = activeConfigResult.Data;

                return Result<object>.Success(new
                {
                    isShippingAvailable = true,
                    Name = config.Name,
                    lowOrderThreshold = config.LowOrderThreshold,
                    lowOrderCost = config.LowOrderShippingCost,
                    highOrderCost = config.HighOrderShippingCost,
                    freeShippingThreshold = config.FreeShippingThreshold,
                    deliveryDays = config.EstimatedDeliveryDays,
                    maxDeliveryDistance = config.MaxDeliveryDistanceKm,
                    isFreeShippingActive = config.IsFreeShippingActive &&
                        (!config.FreeShippingStartDate.HasValue || DateTime.UtcNow >= config.FreeShippingStartDate) &&
                        (!config.FreeShippingEndDate.HasValue || DateTime.UtcNow <= config.FreeShippingEndDate),
                    freeShippingDescription = config.FreeShippingDescription,
                    customerMessage = config.CustomerMessage,
                    weekendSurcharge = config.WeekendSurcharge,
                    holidaySurcharge = config.HolidaySurcharge,
                    rushDeliverySurcharge = config.RushDeliverySurcharge,
                    message = $"Shipping: ₨{config.LowOrderShippingCost} for orders under ₨{config.LowOrderThreshold}, ₨{config.HighOrderShippingCost} for orders above ₨{config.LowOrderThreshold}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping info for customers");
                return Result<object>.Failure("Error retrieving shipping information");
            }
        }

        // Helper methods
        private async Task<Result<Shipping>> GetActiveConfigurationInternalAsync()
        {
            try
            {
                // // Try cache first
                // var cacheKey = "shipping_config_active";
                // var cachedConfig = await _cache.GetStringAsync(cacheKey);

                // if (cachedConfig != null)
                // {
                //     var cached = JsonSerializer.Deserialize<Shipping>(cachedConfig);
                //     if (cached != null)
                //     {
                //         return Result<Shipping>.Success(cached);
                //     }
                // }

                //  Get from database using FirstOrDefaultAsync with proper parameters
                var activeConfig = await _shippingRepository.FirstOrDefaultAsync(
                    predicate: c => c.IsActive && c.IsDefault && !c.IsDeleted
                );

                if (activeConfig == null)
                {
                    // Fallback to any active configuration
                    activeConfig = await _shippingRepository.FirstOrDefaultAsync(
                        predicate: c => c.IsActive && !c.IsDeleted,
                        orderBy: c => c.Id,
                        sortDirection: "desc"
                    );
                }

                if (activeConfig == null)
                {
                    return Result<Shipping>.Failure("No active shipping configuration found");
                }

                // // Cache for 15 minutes
                // var cacheOptions = new DistributedCacheEntryOptions
                // {
                //     AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                // };
                // await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(activeConfig), cacheOptions);

                return Result<Shipping>.Success(activeConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active shipping configuration");
                return Result<Shipping>.Failure("Error retrieving active configuration");
            }
        }

        private static bool IsWeekend()
        {
            var today = DateTime.Now.DayOfWeek;
            return today == DayOfWeek.Saturday || today == DayOfWeek.Sunday;
        }

        private static bool IsHoliday()
        {
            // Simple Nepal holiday check
            var today = DateTime.Now.Date;
            var nepaliHolidays = new[]
            {
                new DateTime(today.Year, 1, 1),  // New Year
                new DateTime(today.Year, 4, 14), // Nepali New Year (approximate)
            };

            return nepaliHolidays.Contains(today);
        }
       
        

    }
}