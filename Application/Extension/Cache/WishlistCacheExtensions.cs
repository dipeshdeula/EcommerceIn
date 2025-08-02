using Application.Dto.WhishListDTOs;
using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extension.Cache
{
    public static class WishlistCacheExtensions
    {
        public static async Task<WishlistSummaryDTO?> GetCachedWishlistAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"wishlist:user:{userId}";
            return await cacheService.GetAsync<WishlistSummaryDTO>(cacheKey, cancellationToken);
        }
    }
}
