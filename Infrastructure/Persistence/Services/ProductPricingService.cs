////filepath: e:\EcomerceDeployPostgres\EcommerceBackendAPI\Infrastructure\Persistence\Services\ProductPricingService.cs
using Application.Dto.ProductDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums.BannerEventSpecial;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class ProductPricingService : IProductPricingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductPricingService> _logger;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public ProductPricingService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            INepalTimeZoneService nepalTimeZoneService,
            ILogger<ProductPricingService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _nepalTimeZoneService= nepalTimeZoneService;
            _logger = logger;
        }

        public async Task<ProductPriceInfoDTO> GetEffectivePriceAsync(int productId, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(" Getting effective price for product ID: {ProductId}", productId);

            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", productId);
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            return await GetEffectivePriceAsync(product, userId, cancellationToken);
        }

        public async Task<ProductPriceInfoDTO> GetEffectivePriceAsync(Product product, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting effective price for product {ProductId} - {ProductName}", product.Id, product.Name);

            var priceInfo = new ProductPriceInfoDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OriginalPrice = product.MarketPrice,
                EffectivePrice = product.MarketPrice
            };

            try
            {
                // Get the best active event for this product
                var activeEvent = await GetBestActiveEventForProductAsync(product.Id, userId, cancellationToken);

                if (activeEvent != null)
                {
                    _logger.LogInformation(" Found active event for product {ProductId}: {EventName} (Discount: {DiscountValue}%, Type: {PromotionType})",
                        product.Id, activeEvent.Name, activeEvent.DiscountValue, activeEvent.PromotionType);

                    // Calculate Discount
                    var discountResult = CalculateDiscount(product, activeEvent);

                    _logger.LogDebug("Discount calculation result: Original={OriginalPrice}, Final={FinalPrice}, HasDiscount={HasDiscount}",
                        discountResult.OriginalPrice, discountResult.FinalPrice, discountResult.HasDiscount);

                    if (discountResult.HasDiscount)
                    {
                        priceInfo.EffectivePrice = discountResult.FinalPrice;
                        priceInfo.AppliedEventId = activeEvent.Id;
                        priceInfo.AppliedEventName = activeEvent.Name;
                        priceInfo.EventTagLine = activeEvent.TagLine;
                        priceInfo.PromotionType = activeEvent.PromotionType;
                        priceInfo.EventEndDate = activeEvent.EndDate;

                        _logger.LogInformation("Applied discount to product {ProductId}: {OriginalPrice} → {FinalPrice} (Saved: Rs.{DiscountAmount})",
                            product.Id, priceInfo.OriginalPrice, priceInfo.EffectivePrice, priceInfo.DiscountAmount);
                    }
                }
                else
                {
                    _logger.LogDebug(" No active events found for product {ProductId}", product.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating effective price for product {ProductId}", product.Id);
            }

            return priceInfo;
        }

        public async Task<List<ProductPriceInfoDTO>> GetEffectivePricesAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(" Getting effective prices for {Count} products", productIds.Count);

            var priceInfos = new List<ProductPriceInfoDTO>();

            foreach (var productId in productIds)
            {
                try
                {
                    var priceInfo = await GetEffectivePriceAsync(productId, userId, cancellationToken);
                    priceInfos.Add(priceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, " Failed to get price for product {ProductId}", productId);

                    // Add fallback price info
                    var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                    if (product != null)
                    {
                        priceInfos.Add(new ProductPriceInfoDTO
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            OriginalPrice = product.MarketPrice,
                            EffectivePrice = product.MarketPrice
                        });
                    }
                }
            }

            return priceInfos;
        }

        public async Task<BannerEventSpecial?> GetBestActiveEventForProductAsync(int productId, int? userId = null, CancellationToken cancellationToken = default)
        {
            // Nepal timezone service for accurate time comparisons
            var nowUtc = _nepalTimeZoneService.GetUtcCurrentTime();
            var nowNepal = _nepalTimeZoneService.GetNepalCurrentTime();
           

            _logger.LogDebug("🔍 Looking for active events for product {ProductId} at {UtcTime} (Nepal: {NepalTime})",
               productId, nowUtc.ToString("yyyy-MM-dd HH:mm:ss"), nowNepal.ToString("yyyy-MM-dd HH:mm:ss"));

            var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                predicate: e => e.IsActive &&
                               !e.IsDeleted &&
                               e.Status == EventStatus.Active &&
                               e.StartDate <= nowNepal &&
                               e.EndDate >= nowNepal &&
                               e.CurrentUsageCount < e.MaxUsageCount,
                includeProperties: "EventProducts",
                cancellationToken: cancellationToken);

            _logger.LogDebug("Found {Count} total active events", activeEvents.Count());

            var applicableEvents = activeEvents.Where(e =>
                !e.EventProducts.Any() || // Global event OR
                e.EventProducts.Any(ep => ep.ProductId == productId) // Product-specific event
            ).ToList();

            _logger.LogDebug("Found {Count} applicable events for product {ProductId}", applicableEvents.Count, productId);

            if (applicableEvents.Any())
            {
                var bestEvent = applicableEvents
                    .OrderByDescending(e => e.Priority)
                    .ThenByDescending(e => e.DiscountValue)
                    .First();

                _logger.LogInformation(" Selected best event for product {ProductId}: {EventName} (Priority: {Priority}, Discount: {DiscountValue}%)",
                    productId, bestEvent.Name, bestEvent.Priority, bestEvent.DiscountValue);

                return bestEvent;
            }

            _logger.LogDebug(" No applicable events found for product {ProductId}", productId);
            return null;
        }

        // THE CRITICAL METHOD THAT SHOULD BE CALLED
        private DiscountResult CalculateDiscount(Product product, BannerEventSpecial activeEvent)
        {
            _logger.LogInformation(" CALCULATE DISCOUNT CALLED! Product: {ProductId}, Event: {EventId}", product.Id, activeEvent.Id);

            var result = new DiscountResult
            {
                OriginalPrice = product.MarketPrice,
                FinalPrice = product.MarketPrice,
                HasDiscount = false
            };

            if (activeEvent == null)
            {
                _logger.LogDebug(" No active event provided for discount calculation");
                return result;
            }

            // Check for specific product discount
            var specificDiscount = activeEvent.EventProducts?.FirstOrDefault(ep => ep.ProductId == product.Id)?.SpecificDiscount;
            var discountValue = specificDiscount ?? activeEvent.DiscountValue;

            _logger.LogDebug("💡 Using discount value: {DiscountValue} (Specific: {SpecificDiscount}, Event: {EventDiscount})",
                discountValue, specificDiscount, activeEvent.DiscountValue);

            if (discountValue > 0)
            {
                decimal discountAmount = 0;

                if (activeEvent.PromotionType == PromotionType.Percentage)
                {
                    discountAmount = (product.MarketPrice * discountValue) / 100;
                    _logger.LogDebug("Percentage discount: {MarketPrice} * {DiscountValue}% = Rs.{DiscountAmount}",
                        product.MarketPrice, discountValue, discountAmount);
                }
                else if (activeEvent.PromotionType == PromotionType.FixedAmount)
                {
                    discountAmount = discountValue;
                    _logger.LogDebug("Fixed amount discount: Rs.{DiscountAmount}", discountAmount);
                }

                // Apply maximum discount cap if set
                if (activeEvent.MaxDiscountAmount.HasValue && discountAmount > activeEvent.MaxDiscountAmount.Value)
                {
                    _logger.LogDebug("Applying max discount cap: Rs.{DiscountAmount} → Rs.{MaxDiscount}",
                        discountAmount, activeEvent.MaxDiscountAmount.Value);
                    discountAmount = activeEvent.MaxDiscountAmount.Value;
                }

                result.FinalPrice = Math.Max(0, product.MarketPrice - discountAmount);
                result.HasDiscount = discountAmount > 0 && result.FinalPrice < product.MarketPrice;

                _logger.LogInformation("DISCOUNT CALCULATED: {OriginalPrice} - {DiscountAmount} = {FinalPrice} (HasDiscount: {HasDiscount})",
                    product.MarketPrice, discountAmount, result.FinalPrice, result.HasDiscount);
            }
            else
            {
                _logger.LogDebug(" No valid discount value found");
            }

            return result;
        }

        public async Task<bool> CanUserUseEventAsync(int eventId, int? userId, CancellationToken cancellationToken = default)
        {
            if (!userId.HasValue) return true;

            // Implementation for user-specific event usage validation
            return true;
        }

        public async Task<List<int>> GetEventHighlightedProductIdsAsync(CancellationToken cancellationToken = default)
        {
            var nowUtc = _nepalTimeZoneService.GetUtcCurrentTime();

            var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                predicate: e => e.IsActive &&
                               !e.IsDeleted &&
                               e.Status == EventStatus.Active &&
                               e.StartDate <= nowUtc &&
                               e.EndDate >= nowUtc,
                includeProperties: "EventProducts",
                cancellationToken: cancellationToken);

            var timeActiveEvents = activeEvents.Where(e => _nepalTimeZoneService.IsEventActiveNow(e.StartDate, e.EndDate)).ToList();

            var productIds = new List<int>();

            foreach (var eventItem in activeEvents)
            {
                if (eventItem.EventProducts?.Any() == true)
                {
                    // Event has specific products
                    productIds.AddRange(eventItem.EventProducts.Select(ep => ep.ProductId));
                }
                else
                {
                    // Global event - get all product IDs (limit for performance)
                    var allProducts = await _unitOfWork.Products.GetAllAsync(
                        predicate: p => !p.IsDeleted,
                        take: 50,
                        cancellationToken: cancellationToken);

                    productIds.AddRange(allProducts.Select(p => p.Id));
                }
            }

            return productIds.Distinct().ToList();
        }

        public async Task<bool> IsProductOnSaleAsync(int productId, CancellationToken cancellationToken = default)
        {
            var priceInfo = await GetEffectivePriceAsync(productId, null, cancellationToken);
            return priceInfo.HasDiscount;
        }

        public async Task RefreshPricesForEventAsync(int eventId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing prices for event {EventId}", eventId);
            await InvalidateAllPriceCacheAsync();
        }

        public async Task InvalidatePriceCacheAsync(int productId)
        {
            var cacheKey = $"price_info_{productId}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("🗑️ Invalidated price cache for product {ProductId}", productId);
        }

        public async Task InvalidateAllPriceCacheAsync()
        {
            // Since we don't have a cache registry, this is a placeholder
            _logger.LogInformation("Invalidated ALL price cache");
        }
    }

    // Helper class for discount calculation results
    public class DiscountResult
    {
        public decimal OriginalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public bool HasDiscount { get; set; }
    }
}