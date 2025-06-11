using Application.Dto.BannerEventSpecialDTOs;
using Application.Dto.CartItemDTOs;
using Application.Dto.ProductDTOs;
using Domain.Entities;
namespace Application.Interfaces.Services
{
    public interface IProductPricingService
    {
        Task<ProductPriceInfoDTO> GetEffectivePriceAsync(int productId, int? userId = null,CancellationToken cancellationToken = default);
        Task<ProductPriceInfoDTO> GetEffectivePriceAsync(Product product, int? userId = null,CancellationToken cancellationToken = default);
        Task<List<ProductPriceInfoDTO>> GetEffectivePricesAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default);
        Task<BannerEventSpecial?> GetBestActiveEventForProductAsync(int productId, int? userId = null, CancellationToken cancellationToken = default);
        // Usage validation
        Task<bool> CanUserUseEventAsync(int eventId, int? userId, CancellationToken cancellationToken = default);

        //  Background service support for price refresh
        Task RefreshPricesForEventAsync(int eventId, CancellationToken cancellationToken = default);
        Task InvalidatePriceCacheAsync(int productId);
        Task InvalidateAllPriceCacheAsync();

        //  Event-specific product queries
        Task<List<int>> GetEventHighlightedProductIdsAsync(CancellationToken cancellationToken = default);
        Task<bool> IsProductOnSaleAsync(int productId, CancellationToken cancellationToken = default);

        // Cart-specific methods 

        Task<ProductPriceInfoDTO> GetCartPriceAsync(int productId, int quantity, int? userId = null, CancellationToken cancellationToken = default);
        Task<List<ProductPriceInfoDTO>> GetCartPricesAsync(List<CartPriceRequestDTO> requests, int? userId = null, CancellationToken cancellationToken = default);
        Task<bool> ValidateCartPriceAsync(int productId, decimal expectedPrice, int? userId = null, CancellationToken cancellationToken = default);

    }
}
