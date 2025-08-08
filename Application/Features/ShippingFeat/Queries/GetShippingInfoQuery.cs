using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Application.Features.ShippingFeat.Queries
{
    public record GetShippingInfoQuery() : IRequest<Result<object>>;

    public class GetShippingInfoQueryHandler : IRequestHandler<GetShippingInfoQuery, Result<object>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public GetShippingInfoQueryHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<object>> Handle(GetShippingInfoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Try cache first
                var cacheKey = "shipping_config_active";
                var cachedConfig = await _cache.GetStringAsync(cacheKey);
                
                Domain.Entities.Shipping? activeConfig = null;

                if (cachedConfig != null)
                {
                    activeConfig = JsonSerializer.Deserialize<Domain.Entities.Shipping>(cachedConfig);
                }

                if (activeConfig == null)
                {
                    activeConfig = await _shippingRepository.FirstOrDefaultAsync(
                        predicate: c => c.IsActive && c.IsDefault && !c.IsDeleted
                    );

                    if (activeConfig == null)
                    {
                        activeConfig = await _shippingRepository.FirstOrDefaultAsync(
                            predicate: c => c.IsActive && !c.IsDeleted,
                            orderBy: c => c.Id,
                            sortDirection: "desc"
                        );
                    }
                }

                if (activeConfig == null)
                {
                    return Result<object>.Success(new
                    {
                        isShippingAvailable = false,
                        message = "Shipping temporarily unavailable"
                    });
                }

                var result = new
                {
                    isShippingAvailable = true,
                    configurationName = activeConfig.Name,
                    lowOrderThreshold = activeConfig.LowOrderThreshold,
                    lowOrderCost = activeConfig.LowOrderShippingCost,
                    highOrderCost = activeConfig.HighOrderShippingCost,
                    freeShippingThreshold = activeConfig.FreeShippingThreshold,
                    deliveryDays = activeConfig.EstimatedDeliveryDays,
                    maxDeliveryDistance = activeConfig.MaxDeliveryDistanceKm,
                    isFreeShippingActive = activeConfig.IsFreeShippingActive &&
                        (!activeConfig.FreeShippingStartDate.HasValue || DateTime.UtcNow >= activeConfig.FreeShippingStartDate) &&
                        (!activeConfig.FreeShippingEndDate.HasValue || DateTime.UtcNow <= activeConfig.FreeShippingEndDate),
                    freeShippingDescription = activeConfig.FreeShippingDescription,
                    customerMessage = activeConfig.CustomerMessage,
                    weekendSurcharge = activeConfig.WeekendSurcharge,
                    holidaySurcharge = activeConfig.HolidaySurcharge,
                    rushDeliverySurcharge = activeConfig.RushDeliverySurcharge,
                    message = $"Shipping: ₨{activeConfig.LowOrderShippingCost} for orders under ₨{activeConfig.LowOrderThreshold}, ₨{activeConfig.HighOrderShippingCost} for orders above ₨{activeConfig.LowOrderThreshold}"
                };

                return Result<object>.Success(result, "Shipping information retrieved successfully");
            }
            catch (Exception ex)
            {
                return Result<object>.Failure("Error retrieving shipping information");
            }
        }
    }
}