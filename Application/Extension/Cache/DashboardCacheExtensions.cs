using Application.Dto.AdminDashboardDTOs;
using Application.Interfaces.Services;

namespace Application.Extension.Cache
{
    public static class DashboardCacheExtensions
    {
        public static async Task<AdminDashboardDTO?> GetCachedDashboardAsync(
            this IHybridCacheService cacheService,
            CancellationToken cancellationToken = default)
        {
            const string cacheKey = "dashboard:admin:summary";
            return await cacheService.GetAsync<AdminDashboardDTO>(cacheKey, cancellationToken);
        }
    }
}
