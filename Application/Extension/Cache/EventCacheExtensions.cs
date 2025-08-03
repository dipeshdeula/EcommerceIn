using Application.Dto.BannerEventSpecialDTOs;
using Application.Interfaces.Services;

namespace Application.Extension.Cache
{
    public static class EventCacheExtensions
    {
        public static async Task<List<BannerEventSpecialDTO>> GetActiveEventsAsync(
            this IHybridCacheService cacheService,
            CancellationToken cancellationToken = default)
        {
            const string cacheKey = "events:active:current";
            return await cacheService.GetAsync<List<BannerEventSpecialDTO>>(cacheKey, cancellationToken)
                   ?? new List<BannerEventSpecialDTO>();
        }
    }
}
