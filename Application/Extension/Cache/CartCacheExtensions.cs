using Application.Dto.CartItemDTOs;
using Application.Interfaces.Services;

namespace Application.Extension.Cache
{
    public static class CartCacheExtensions
    {
        public static async Task<List<CartItemDTO>> GetCachedCartAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cart:user:{userId}";
            return await cacheService.GetAsync<List<CartItemDTO>>(cacheKey, cancellationToken) ?? new List<CartItemDTO>();
        }

        public static async Task SetCartCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            List<CartItemDTO> cartItems,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cart:user:{userId}";
            await cacheService.SetAsync(cacheKey, cartItems, TimeSpan.FromMinutes(30), cancellationToken);
        }

        public static async Task<List<CartItemDTO>> GetCachedCartPageAsync(
        this IHybridCacheService cacheService,
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cart:user:{userId}:page:{pageNumber}:size:{pageSize}";
            return await cacheService.GetAsync<List<CartItemDTO>>(cacheKey, cancellationToken) ?? new List<CartItemDTO>();
        }

        public static async Task SetCartPageCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            int pageNumber,
            int pageSize,
            List<CartItemDTO> cartItems,
            CancellationToken cancellationToken = default)
        {
            var cachekey = $"cart:user:{userId}:page:{pageNumber}:size:{pageSize}";
            await cacheService.SetAsync(cachekey, cartItems, TimeSpan.FromMinutes(15), cancellationToken);

        }

        public static async Task<CartSummaryDTO?> GetCachedCartSummaryAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"cart:summary:user:{userId}";
            return await cacheService.GetAsync<CartSummaryDTO>(cacheKey, cancellationToken);
        }

        public static async Task SetCartSummaryCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            CartSummaryDTO summary,
            CancellationToken cancellationToken = default
        )
        {
            var cacheKey = $"cart:summary:{userId}";
            await cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(10), cancellationToken);
        }

        // cache invalidation for cart changes
        public static async Task InvalidateUserCartCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            // Invalidate all cart-related cache for this year
            var patterns = new[]
                {
                    $"cart:user:{userId}",
                    $"cart:user:{userId}:page:*",
                    $"cart:summary:user:{userId}"
                };
            foreach (var pattern in patterns)
            {
                await cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            }
        } 


        
    }
}
