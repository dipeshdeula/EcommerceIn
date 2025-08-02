using Application.Common.Helper;
using Application.Dto.ProductDTOs;

namespace Application.Interfaces.Services
{
    public interface IHybridCacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, string cacheType, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        Task<Dictionary<int, ProductDTO?>> GetProductsBulkAsync(List<int> productIds, CancellationToken cancellationToken = default);
        Task SetProductsBulkAsync(Dictionary<int, ProductDTO> products, CancellationToken cancellationToken = default);
        
        Task<Dictionary<int, ProductPriceInfoDTO?>> GetPricingBulkAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default);
        Task SetPricingBulkAsync(Dictionary<int, ProductPriceInfoDTO> pricing, int? userId = null, CancellationToken cancellationToken = default);

        //  E-COMMERCE SPECIFIC METHODS
        Task<List<ProductDTO>> GetProductsAsync(List<int> productIds, CancellationToken cancellationToken = default);
        Task SetProductsAsync(List<ProductDTO> products, CancellationToken cancellationToken = default);
        Task InvalidateProductsAsync(List<int> productIds, CancellationToken cancellationToken = default);

        Task<List<ProductPriceInfoDTO>> GetPricingAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default);
        Task SetPricingAsync(List<ProductPriceInfoDTO> pricing, int? userId = null, CancellationToken cancellationToken = default);

        Task<CacheHealthInfo> GetHealthAsync(CancellationToken cancellationToken = default);
        Task WarmUpAsync(CancellationToken cancellationToken = default);
    }
}
