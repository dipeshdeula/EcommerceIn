using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Dto.ShippingDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Features.ShippingFeat.Commands
{
    public record CalculateShippingCommand(
        int UserId,
        decimal OrderTotal,
        double? DeliveryLatitude,
        double? DeliveryLongitude,
        bool RequestRushDelivery,
        DateTime? RequestedDeliveryDate,
        int? PreferredConfigurationId
    ) : IRequest<Result<ShippingCalculationDetailDTO>>;

    public class CalculateShippingCommandHandler : IRequestHandler<CalculateShippingCommand, Result<ShippingCalculationDetailDTO>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly ILocationService _locationService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CalculateShippingCommandHandler> _logger;

        public CalculateShippingCommandHandler(
            IShippingRepository shippingRepository,
            ILocationService locationService,
            IDistributedCache cache,
            ILogger<CalculateShippingCommandHandler> logger)
        {
            _shippingRepository = shippingRepository;
            _locationService = locationService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<ShippingCalculationDetailDTO>> Handle(CalculateShippingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Calculating shipping for UserId: {UserId}, OrderTotal: ₨{OrderTotal}",
                    request.UserId, request.OrderTotal);

                // Get active configuration
                var activeConfigResult = await GetActiveConfigurationAsync();
                if (!activeConfigResult.Succeeded || activeConfigResult.Data == null)
                {
                    return Result<ShippingCalculationDetailDTO>.Failure("No active shipping configuration found");
                }

                var config = activeConfigResult.Data;
                var result = new ShippingCalculationDetailDTO
                {
                    OrderSubtotal = request.OrderTotal,
                    IsShippingAvailable = false,
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

                return Result<ShippingCalculationDetailDTO>.Success(result, "Shipping calculated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping for order total ₨{OrderTotal}", request.OrderTotal);
                return Result<ShippingCalculationDetailDTO>.Failure($"Error calculating shipping: {ex.Message}");
            }
        }

        private async Task<Result<Shipping>> GetActiveConfigurationAsync()
        {
            try
            {
                // Try cache first
                var cacheKey = "shipping_config_active";
                var cachedConfig = await _cache.GetStringAsync(cacheKey);

                if (cachedConfig != null)
                {
                    var cached = JsonSerializer.Deserialize<Shipping>(cachedConfig);
                    if (cached != null)
                    {
                        return Result<Shipping>.Success(cached);
                    }
                }

                // Get from database
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

                // Cache for 15 minutes
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(activeConfig), cacheOptions);

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