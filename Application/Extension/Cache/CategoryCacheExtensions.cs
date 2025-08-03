using Application.Dto.CategoryDTOs;
using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extension.Cache
{
    public static class CategoryCacheExtensions
    {
        public static async Task<List<CategoryDTO>> GetCachedCategoriesAsync(
            this IHybridCacheService cacheService,
            CancellationToken cancellationToken = default)
        {
            const string cacheKey = "categories:all:active";
            var cached = await cacheService.GetAsync<List<CategoryDTO>>(cacheKey, cancellationToken);

            if (cached != null) return cached;

            // Fallback to database (implement in handler)
            return new List<CategoryDTO>();
        }
    }
}
