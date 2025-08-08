using Application.Common;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Application.Features.ShippingFeat.Queries
{
    public record GetActiveShippingQuery() : IRequest<Result<ShippingDTO>>;

    public class GetActiveShippingQueryHandler : IRequestHandler<GetActiveShippingQuery, Result<ShippingDTO>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public GetActiveShippingQueryHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<ShippingDTO>> Handle(GetActiveShippingQuery request, CancellationToken cancellationToken)
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
                    // Get from database
                    activeConfig = await _shippingRepository.FirstOrDefaultAsync(
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
                        return Result<ShippingDTO>.Failure("No active shipping configuration found");
                    }

                    // Cache for 15 minutes
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                    };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(activeConfig), cacheOptions);
                }

                var result = activeConfig.ToShippingDTO();
                return Result<ShippingDTO>.Success(result, "Active shipping configuration retrieved successfully");
            }
            catch (Exception ex)
            {
                return Result<ShippingDTO>.Failure($"Error retrieving active configuration: {ex.Message}");
            }
        }
    }
}