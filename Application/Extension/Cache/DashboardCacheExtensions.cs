using Application.Dto.AdminDashboardDTOs;
using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
