using Application.Dto.UserDTOs;
using Application.Interfaces.Services;

namespace Application.Extension.Cache
{
    public static class UserCacheExtensions
    {
        public static async Task<UserDTO?> GetCachedUserAsync(
            this IHybridCacheService cacheService,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user:profile:{userId}";
            return await cacheService.GetAsync<UserDTO>(cacheKey, cancellationToken);
        }

        public static async Task SetUserCacheAsync(
            this IHybridCacheService cacheService,
            UserDTO user,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"user:profile:{user.Id}";
            await cacheService.SetAsync(cacheKey, user, TimeSpan.FromHours(2));
        }
    }
}
