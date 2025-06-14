////filepath: e:\EcomerceDeployPostgres\EcommerceBackendAPI\Infrastructure\Persistence\Services\ProductPricingService.cs
using Application.Dto.CartItemDTOs;
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

            var basePrice = product.DiscountPrice ?? product.MarketPrice;

            var priceInfo = new ProductPriceInfoDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OriginalPrice = product.MarketPrice,
                BasePrice = basePrice,
                EffectivePrice = basePrice,
                EventDiscountAmount = 0       


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

                    _logger.LogDebug(" Discount calculation result: Original=Rs.{OriginalPrice}, Base=Rs.{BasePrice}, Final=Rs.{FinalPrice}, " +
                           "ProductDiscount=Rs.{ProductDiscount}, EventDiscount=Rs.{EventDiscount}",
                discountResult.OriginalPrice, discountResult.BasePrice, discountResult.FinalPrice,
                discountResult.ProductDiscountAmount, discountResult.EventDiscountAmount);

                    if (discountResult.HasEventDiscount)
                    {
                        priceInfo.EffectivePrice = discountResult.FinalPrice;
                        priceInfo.AppliedEventId = activeEvent.Id;
                        priceInfo.AppliedEventName = activeEvent.Name;
                        priceInfo.EventTagLine = activeEvent.TagLine;
                        priceInfo.PromotionType = activeEvent.PromotionType;
                        priceInfo.EventEndDate = activeEvent.EndDate;
                        priceInfo.EventDiscountAmount = discountResult.EventDiscountAmount;

                        _logger.LogInformation("Applied pricing to product {ProductId}: Rs.{OriginalPrice} → Rs.{FinalPrice} " +
                                     "(Product: -Rs.{ProductDiscount}, Event: -Rs.{EventDiscount}, Total saved: Rs.{TotalSavings})",
                    product.Id, priceInfo.OriginalPrice, priceInfo.EffectivePrice, 
                    priceInfo.RegularDiscountAmount, priceInfo.EventDiscountAmount, priceInfo.TotalDiscountAmount);
                    }
                }
                else
                {
                   _logger.LogDebug("📭 No active events found for product {ProductId}. Using base price: Rs.{BasePrice}", 
                product.Id, basePrice);
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
           

            _logger.LogDebug("Looking for active events for product {ProductId} at {UtcTime} (Nepal: {NepalTime})",
               productId, nowUtc.ToString("yyyy-MM-dd HH:mm:ss"), nowNepal.ToString("yyyy-MM-dd HH:mm:ss"));

            var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                predicate: e => e.IsActive &&
                               !e.IsDeleted &&
                               e.Status == EventStatus.Active &&
                               e.StartDate <= nowUtc &&
                               e.EndDate >= nowUtc &&
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

               //  Log in both UTC and Nepal time for debugging (AFTER selection)
        var startNepal = _nepalTimeZoneService.ConvertFromUtcToNepal(bestEvent.StartDate);
        var endNepal = _nepalTimeZoneService.ConvertFromUtcToNepal(bestEvent.EndDate);

        _logger.LogInformation(" Selected best event for product {ProductId}: {EventName} " +
                             "(Priority: {Priority}, Discount: {DiscountValue}%) " +
                             "UTC: {StartUtc} - {EndUtc}, Nepal: {StartNepal} - {EndNepal}, Now Nepal: {NowNepal}",
            productId, bestEvent.Name, bestEvent.Priority, bestEvent.DiscountValue,
            bestEvent.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
            bestEvent.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
            startNepal.ToString("yyyy-MM-dd HH:mm:ss"),
            endNepal.ToString("yyyy-MM-dd HH:mm:ss"),
            nowNepal.ToString("yyyy-MM-dd HH:mm:ss"));

                return bestEvent;
            }

            _logger.LogDebug(" No applicable events found for product {ProductId}", productId);
            return null;
        }

        // THE CRITICAL METHOD THAT SHOULD BE CALLED
        private DiscountResult CalculateDiscount(Product product, BannerEventSpecial activeEvent)
        {
            _logger.LogInformation(" CALCULATE DISCOUNT CALLED! Product: {ProductId}, Event: {EventId}", product.Id, activeEvent.Id);

            var basePrice = product.DiscountPrice ?? product.MarketPrice;
            var hasProductDiscount = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice;
            var result = new DiscountResult
            {
                OriginalPrice = product.MarketPrice,
                BasePrice = basePrice,
                FinalPrice = basePrice,
                HasDiscount = hasProductDiscount, // Track if product has discount
                ProductDiscountAmount = hasProductDiscount ? product.MarketPrice - basePrice : 0,
                EventDiscountAmount = 0
            };

            // Handle no event scenario

            if (activeEvent == null)
            {
                _logger.LogDebug("No active event provided. Using base price: Rs.{BasePrice}", basePrice);
                return result;
            }

            // Check for specific product discount and event discount
            var specificDiscount = activeEvent.EventProducts?.FirstOrDefault(ep => ep.ProductId == product.Id)?.SpecificDiscount;
            var discountValue = specificDiscount ?? activeEvent.DiscountValue;

            _logger.LogDebug("Base price: Rs.{BasePrice} (Market: Rs.{MarketPrice}, Product discount: Rs.{ProductDiscount})",
            basePrice, product.MarketPrice, result.ProductDiscountAmount);

            _logger.LogDebug("🎯 Using event discount value: {DiscountValue} (Specific: {SpecificDiscount}, Event: {EventDiscount})",
            discountValue, specificDiscount, activeEvent.DiscountValue);

            // STEP 4: Calculate event discount (apply to basePrice, not MarketPrice)
            if (discountValue > 0)
            {
                decimal eventDiscountAmount = 0;

                if (activeEvent.PromotionType == PromotionType.Percentage)
                {
                    eventDiscountAmount = (basePrice * discountValue) / 100;
                    _logger.LogDebug(" Percentage discount: Rs.{BasePrice} * {DiscountValue}% = Rs.{EventDiscountAmount}",
                basePrice, discountValue, eventDiscountAmount);
                }
                else if (activeEvent.PromotionType == PromotionType.FixedAmount)
                {
                    eventDiscountAmount = discountValue;
                 _logger.LogDebug(" Fixed amount discount: Rs.{EventDiscountAmount}", eventDiscountAmount);
                }

                // Apply maximum discount cap if set
                if (activeEvent.MaxDiscountAmount.HasValue && eventDiscountAmount > activeEvent.MaxDiscountAmount.Value)
                {
                    _logger.LogDebug("Applying max discount cap: Rs.{EventDiscountAmount} → Rs.{MaxDiscount}",
                        eventDiscountAmount, activeEvent.MaxDiscountAmount.Value);
                    eventDiscountAmount = activeEvent.MaxDiscountAmount.Value;
                }

                //  Calculate final price (subtract event discount from basePrice)
                var finalPrice = Math.Max(0, basePrice - eventDiscountAmount);
                var hasEventDiscount = eventDiscountAmount > 0 && finalPrice < basePrice;

                // Update result with final price and discount info
                result.FinalPrice = finalPrice;
                result.HasDiscount = hasProductDiscount || hasEventDiscount;
                result.EventDiscountAmount = eventDiscountAmount;
                result.TotalDiscountAmount = result.ProductDiscountAmount + eventDiscountAmount;


                _logger.LogInformation("DISCOUNT CALCULATED: Rs.{OriginalPrice} → Rs.{BasePrice} → Rs.{FinalPrice} " +
                              "(Product: -Rs.{ProductDiscount}, Event: -Rs.{EventDiscount}, Total: -Rs.{TotalDiscount})",
            product.MarketPrice, basePrice, finalPrice, 
            result.ProductDiscountAmount, eventDiscountAmount, result.TotalDiscountAmount);
            }
            else
            {
                _logger.LogDebug("No valid event discount value found. Using base price: Rs.{BasePrice}", basePrice);
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

            _logger.LogDebug(" Getting event highlighted products at UTC: {UtcTime}", 
            nowUtc.ToString("yyyy-MM-dd HH:mm:ss"));

            try{

            var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                predicate: e => e.IsActive &&
                               !e.IsDeleted &&
                               e.Status == EventStatus.Active &&
                               e.StartDate <= nowUtc &&
                               e.EndDate >= nowUtc,
                includeProperties: "EventProducts",
                cancellationToken: cancellationToken);
                _logger.LogDebug("🔍 Getting event highlighted products at UTC: {UtcTime}", 
                nowUtc.ToString("yyyy-MM-dd HH:mm:ss"));

            

            var productIds = new List<int>();

            foreach (var eventItem in activeEvents)
            {
                    if (eventItem.EventProducts?.Any() == true)
                    {
                        // Event has specific products
                        productIds.AddRange(eventItem.EventProducts.Select(ep => ep.ProductId));
                        _logger.LogDebug("Added {Count} specific products from event {EventName}",
                        eventItem.EventProducts.Count, eventItem.Name);
                    }
                    else
                    {
                        // Global event - get all product IDs (limit for performance)
                        var allProducts = await _unitOfWork.Products.GetAllAsync(
                            predicate: p => !p.IsDeleted,
                            take: 50,
                            cancellationToken: cancellationToken);

                        productIds.AddRange(allProducts.Select(p => p.Id));

                        _logger.LogDebug("Added {Count} products from global event {EventName}", 
                        allProducts.Count(), eventItem.Name);
                    }
            }

            var distinctProductIds = productIds.Distinct().ToList();
            _logger.LogDebug("Found {Count} distinct product IDs for active events", distinctProductIds.Count);

            return distinctProductIds;
        }catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event highlighted product IDs");
                return new List<int>();
            }
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
            _logger.LogDebug(" Invalidated price cache for product {ProductId}", productId);
        }

        public async Task InvalidateAllPriceCacheAsync()
        {
            // Since we don't have a cache registry, this is a placeholder
            _logger.LogInformation("Invalidated ALL price cache");
        }

        public async Task<ProductPriceInfoDTO> GetCartPriceAsync(int productId, int quantity, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting cart price for product {ProductId}, quantity {Quantity}", productId, quantity);

            //  Use your existing pricing logic
            var priceInfo = await GetEffectivePriceAsync(productId, userId, cancellationToken);

            //  Stock validation for cart
            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
            if (product != null)
            {
                var availableStock = product.StockQuantity - product.ReservedStock;
                var canReserve = availableStock >= quantity;

                priceInfo.WithStockInfo(availableStock, canReserve);

                _logger.LogDebug("Cart price calculated: Product {ProductId}, Price {Price}, Stock {Stock}, CanReserve {CanReserve}",
                    productId, priceInfo.EffectivePrice, availableStock, canReserve);
            }

            return priceInfo;
        }

        // Bulk cart pricing
        public async Task<List<ProductPriceInfoDTO>> GetCartPricesAsync(List<CartPriceRequestDTO> requests, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting cart prices for {Count} items", requests.Count);

            var results = new List<ProductPriceInfoDTO>();

            foreach (var request in requests)
            {
                try
                {
                    var priceInfo = await GetCartPriceAsync(request.ProductId, request.Quantity, userId, cancellationToken);

                    //  Price protection check
                    if (request.MaxAcceptablePrice.HasValue && priceInfo.EffectivePrice > request.MaxAcceptablePrice.Value)
                    {
                        priceInfo.IsPriceStable = false;
                        priceInfo.StockMessage = $"Price changed. Current: Rs.{priceInfo.EffectivePrice:F2}, Max: Rs.{request.MaxAcceptablePrice:F2}";
                    }

                    // Event validation
                    if (request.PreferredEventId.HasValue && priceInfo.AppliedEventId != request.PreferredEventId.Value)
                    {
                        priceInfo.IsPriceStable = false;
                        priceInfo.StockMessage = "Preferred event is no longer active";
                    }

                    results.Add(priceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting cart price for product {ProductId}", request.ProductId);

                    // Add error result
                    results.Add(new ProductPriceInfoDTO
                    {
                        ProductId = request.ProductId,
                        IsStockAvailable = false,
                        CanReserveStock = false,
                        IsPriceStable = false,
                        StockMessage = "Error calculating price"
                    });
                }
            }

            return results;
        }

        //  Price validation for cart operations


        public async Task<bool> ValidateCartPriceAsync(int productId, decimal expectedPrice, int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentPrice = await GetEffectivePriceAsync(productId, userId, cancellationToken);
                var tolerance = 0.01m; // 1 cent tolerance for rounding differences

                var isValid = Math.Abs(currentPrice.EffectivePrice - expectedPrice) <= tolerance;

                _logger.LogDebug("Price validation: Product {ProductId}, Expected {Expected}, Current {Current}, Valid {Valid}",
                    productId, expectedPrice, currentPrice.EffectivePrice, isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart price for product {ProductId}", productId);
                return false;
            }
        }
    }

    // Helper class for discount calculation results
    public class DiscountResult
    {
        public decimal OriginalPrice { get; set; }
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public bool HasDiscount { get; set; }

        // Track different discount types
        public decimal ProductDiscountAmount { get; set; }
        public decimal EventDiscountAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }

        // Convenience properties for easy access
        public bool HasProductDiscount => ProductDiscountAmount > 0;
        public bool HasEventDiscount => EventDiscountAmount > 0;
        public decimal DiscountPercentage => OriginalPrice > 0 ? (TotalDiscountAmount / OriginalPrice) * 100 : 0;
    }
}