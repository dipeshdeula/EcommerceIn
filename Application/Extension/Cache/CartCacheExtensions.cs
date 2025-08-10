using Application.Dto.CartItemDTOs;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Application.Extension.Cache
{
    public static class CartCacheExtensions
    {
        //  CONSISTENT KEY PATTERNS - Match your existing implementation
        private const string CART_USER_KEY = "EcommerceAPICart:user:{0}";
        private const string CART_PAGE_KEY = "EcommerceAPICart:user:{0}:page:{1}:size:{2}";
        private const string CART_SUMMARY_KEY = "EcommerceAPICart:summary:user:{0}";
        private const string CART_COUNT_KEY = "EcommerceAPICart:count:user:{0}";

        public static async Task<List<CartItemDTO>> GetCachedCartAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_USER_KEY, userId);
            return await cacheService.GetAsync<List<CartItemDTO>>(cacheKey, cancellationToken) ?? new List<CartItemDTO>();
        }

        public static async Task SetCartCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            List<CartItemDTO> cartItems,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_USER_KEY, userId);
            await cacheService.SetAsync(cacheKey, cartItems, TimeSpan.FromMinutes(30), cancellationToken);
        }

        public static async Task<List<CartItemDTO>> GetCachedCartPageAsync(
            this IHybridCacheService cacheService,
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_PAGE_KEY, userId, pageNumber, pageSize);
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
            var cacheKey = string.Format(CART_PAGE_KEY, userId, pageNumber, pageSize);
            await cacheService.SetAsync(cacheKey, cartItems, TimeSpan.FromMinutes(15), cancellationToken);
        }

        public static async Task<CartSummaryDTO?> GetCachedCartSummaryAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_SUMMARY_KEY, userId);
            return await cacheService.GetAsync<CartSummaryDTO>(cacheKey, cancellationToken);
        }

        public static async Task SetCartSummaryCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            CartSummaryDTO summary,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_SUMMARY_KEY, userId);
            await cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(10), cancellationToken);
        }

        public static async Task<int> GetCachedCartCountAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_COUNT_KEY, userId);
            return await cacheService.GetAsync<int>(cacheKey, cancellationToken);
        }

        public static async Task SetCartCountCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            int count,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = string.Format(CART_COUNT_KEY, userId);
            await cacheService.SetAsync(cacheKey, count, TimeSpan.FromMinutes(10), cancellationToken);
        }

        /// <summary>
        ///  COMPREHENSIVE CACHE INVALIDATION - Remove all cart-related cache for a user
        /// </summary>
        public static async Task InvalidateUserCartCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                //  SPECIFIC KEY REMOVAL (more reliable than patterns)
                var specificKeysToRemove = new List<string>
                {
                    string.Format(CART_USER_KEY, userId),
                    string.Format(CART_SUMMARY_KEY, userId),
                    string.Format(CART_COUNT_KEY, userId)
                };

                //  COMMON PAGINATION KEYS
                var commonPageSizes = new[] { 5, 10, 15, 20, 25, 50 };
                var commonPages = new[] { 1, 2, 3, 4, 5 };

                foreach (var pageSize in commonPageSizes)
                {
                    foreach (var pageNumber in commonPages)
                    {
                        specificKeysToRemove.Add(string.Format(CART_PAGE_KEY, userId, pageNumber, pageSize));
                    }
                }

                // Remove specific keys first
                await cacheService.RemoveBulkAsync(specificKeysToRemove, cancellationToken);

                //  PATTERN REMOVAL (as backup for any missed keys)
                var patterns = new[]
                {
                    $"EcommerceAPICart:user:{userId}:*",
                    $"EcommerceAPICart:*:user:{userId}*",
                    $"*user*{userId}*cart*",
                    $"*cart*user*{userId}*"
                };

                foreach (var pattern in patterns)
                {
                    await cacheService.RemoveByPatternAsync(pattern, cancellationToken);
                }

              

               
            }
            catch (Exception ex)
            {
                // Don't throw - cache invalidation failure shouldn't break the operation
                System.Diagnostics.Debug.WriteLine($"❌ Cache invalidation failed for user {userId}: {ex.Message}");
            }
        }

        /// <summary>
        ///  INVALIDATE INDIVIDUAL CART ITEM
        /// </summary>
        public static async Task InvalidateCartItemCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            int cartItemId,
            CancellationToken cancellationToken = default)
        {
            // Remove all user cart cache when an item changes
            await InvalidateUserCartCacheAsync(cacheService, userId, cancellationToken);
            
            // Also remove any item-specific cache if you have it
            var itemSpecificKeys = new[]
            {
                $"cartitem:{cartItemId}",
                $"cart:item:{cartItemId}",
                $"EcommerceAPICart:item:{cartItemId}"
            };
            
            await cacheService.RemoveBulkAsync(itemSpecificKeys, cancellationToken);
        }

        /// <summary>
        ///  FORCE REFRESH - Clear cache and force database query
        /// </summary>
        public static async Task ForceRefreshUserCartAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await InvalidateUserCartCacheAsync(cacheService, userId, cancellationToken);
            
            // Set a temporary flag to skip cache for next few requests
            var skipCacheKey = $"EcommerceAPICart:skip:user:{userId}";
            await cacheService.SetAsync(skipCacheKey, DateTime.UtcNow.ToString(), TimeSpan.FromMinutes(2), cancellationToken);
        }

        /// <summary>
        ///  CHECK IF CACHE SHOULD BE SKIPPED
        /// </summary>
        public static async Task<bool> ShouldSkipCacheAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var skipCacheKey = $"EcommerceAPICart:skip:user:{userId}";
            var skipFlag = await cacheService.GetAsync<string>(skipCacheKey, cancellationToken);
            return !string.IsNullOrEmpty(skipFlag);
        }

        /// <summary>
        ///  GENERATE CACHE KEY - Centralized key generation
        /// </summary>
        public static string GenerateCartCacheKey(int userId, int? pageNumber = null, int? pageSize = null, string? suffix = null)
        {
            if (pageNumber.HasValue && pageSize.HasValue)
            {
                var key = string.Format(CART_PAGE_KEY, userId, pageNumber.Value, pageSize.Value);
                return suffix != null ? $"{key}:{suffix}" : key;
            }
            
            var baseKey = string.Format(CART_USER_KEY, userId);
            return suffix != null ? $"{baseKey}:{suffix}" : baseKey;
        }
    }
}