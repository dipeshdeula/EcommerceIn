// ✅ FIXED: ProductPricingService.cs
using System.Diagnostics;
using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
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
        private readonly IHybridCacheService _hybridCacheService;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public ProductPricingService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            INepalTimeZoneService nepalTimeZoneService,
            IHybridCacheService hybridCacheService,
            ILogger<ProductPricingService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _nepalTimeZoneService = nepalTimeZoneService;
            _hybridCacheService = hybridCacheService;
            _logger = logger;
        }

        // ✅ CORE METHOD 1: Single Product Pricing
        public async Task<ProductPriceInfoDTO> GetEffectivePriceAsync(int productId, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting effective price for product ID: {ProductId}", productId);

            // Try hybrid cache first
            var cacheKey = $"pricing:product:{productId}:user:{userId ?? 0}";
            var cachedPricing = await _hybridCacheService.GetAsync<ProductPriceInfoDTO>(cacheKey, cancellationToken);
            
            if (cachedPricing != null)
            {
                _logger.LogDebug("🎯 Cache HIT for product {ProductId}", productId);
                return cachedPricing;
            }

            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", productId);
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            var result = await GetEffectivePriceAsync(product, userId, cancellationToken);
            
            // Cache the result
            await _hybridCacheService.SetAsync(cacheKey, result, "pricing", cancellationToken);
            
            return result;
        }

        // ✅ CORE METHOD 2: Single Product with Entity
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
                EventDiscountAmount = 0,
                ProductDiscountAmount = product.DiscountPrice.HasValue ? product.MarketPrice - basePrice : 0,
                HasProductDiscount = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice,
                CalculatedAt = DateTime.UtcNow
            };

            try
            {
                // Get the best active event for this product
                var activeEvent = await GetBestActiveEventForProductAsync(product.Id, userId, cancellationToken);

                if (activeEvent != null)
                {
                    _logger.LogInformation("🎉 Found active event for product {ProductId}: {EventName} (Type: {PromotionType}, Discount: {DiscountValue})",
                        product.Id, activeEvent.Name, activeEvent.PromotionType, activeEvent.DiscountValue);

                    // Calculate discount
                    var discountResult = CalculateDiscount(product, activeEvent);

                    if (discountResult.HasEventDiscount || discountResult.HasFreeShipping)
                    {
                        ApplyDiscountToPriceInfo(priceInfo, discountResult, activeEvent);

                        _logger.LogInformation("✅ Applied event pricing to product {ProductId}: " +
                                            "Rs.{OriginalPrice} → Rs.{FinalPrice} " +
                                            "(Total saved: Rs.{TotalSavings}) Event: {EventName}",
                            product.Id, priceInfo.OriginalPrice, priceInfo.EffectivePrice,
                            priceInfo.TotalDiscountAmount, activeEvent.Name);
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

        // ✅ CORE METHOD 3: Bulk Pricing (OPTIMIZED)
        public async Task<List<ProductPriceInfoDTO>> GetEffectivePricesAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("🚀 Getting effective prices for {Count} products", productIds.Count);

            if (!productIds.Any()) return new List<ProductPriceInfoDTO>();

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // ✅ STEP 1: Bulk cache lookup - Single call instead of N calls
                var cachedPricing = await _hybridCacheService.GetPricingBulkAsync(productIds, userId, cancellationToken);
                
                var priceInfos = new List<ProductPriceInfoDTO>();
                var missingProductIds = new List<int>();

                foreach (var productId in productIds)
                {
                    if (cachedPricing.TryGetValue(productId, out var cachedPrice) && cachedPrice != null)
                    {
                        priceInfos.Add(cachedPrice);
                        _logger.LogDebug("🎯 Cache HIT for product {ProductId}", productId);
                    }
                    else
                    {
                        missingProductIds.Add(productId);
                    }
                }

                _logger.LogInformation("📊 Cache stats: {CacheHits} hits, {CacheMisses} misses", 
                    priceInfos.Count, missingProductIds.Count);

                // ✅ STEP 2: Calculate missing prices
                if (missingProductIds.Any())
                {
                    var freshPrices = await CalculateBulkPricingAsync(missingProductIds, userId, cancellationToken);
                    priceInfos.AddRange(freshPrices);

                    // ✅ STEP 3: Bulk cache fresh results - Single call instead of N calls
                    if (freshPrices.Any())
                    {
                        var pricingDictionary = freshPrices.ToDictionary(p => p.ProductId, p => p);
                        await _hybridCacheService.SetPricingBulkAsync(pricingDictionary, userId, cancellationToken);
                    }
                }

                // ✅ STEP 4: Sort results to match input order
                var sortedResults = priceInfos.OrderBy(p => productIds.IndexOf(p.ProductId)).ToList();

                stopwatch.Stop();
                var cacheHitRate = (double)(productIds.Count - missingProductIds.Count) / productIds.Count * 100;
                
                _logger.LogInformation("✅ BULK PRICING: {Count} products in {ElapsedMs}ms " +
                                    "(Cache hit rate: {CacheHitRate:F1}%, Avg: {AvgMs}ms per product)",
                                    sortedResults.Count, stopwatch.ElapsedMilliseconds, cacheHitRate,
                                    stopwatch.ElapsedMilliseconds / Math.Max(1, sortedResults.Count));

                return sortedResults;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "🚨 Error in bulk pricing calculation after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                // Fallback to individual calculations
                return await GetEffectivePricesAsyncFallback(productIds, userId, cancellationToken);
            }
        }
        // ✅ HELPER METHOD: Bulk calculation without cache
        private async Task<List<ProductPriceInfoDTO>> CalculateBulkPricingAsync(List<int> productIds, int? userId, CancellationToken cancellationToken)
        {
            // ✅ STEP 1: Get ALL products in ONE query
            var products = await _unitOfWork.Products.GetAllAsync(
                predicate: p => productIds.Contains(p.Id) && !p.IsDeleted,
                cancellationToken: cancellationToken);

            var productLookup = products.ToDictionary(p => p.Id, p => p);

            // ✅ STEP 2: Get ALL active events in ONE query
            var nowUtc = _nepalTimeZoneService.GetUtcCurrentTime();
            var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                predicate: e => e.IsActive &&
                           !e.IsDeleted &&
                           e.Status == EventStatus.Active &&
                           e.StartDate <= nowUtc &&
                           e.EndDate >= nowUtc &&
                           e.CurrentUsageCount < e.MaxUsageCount,
                includeProperties: "EventProducts",
                cancellationToken: cancellationToken);

            // ✅ STEP 3: Build event lookup for products
            var productEventLookup = BuildProductEventLookup(productIds, activeEvents);

            // ✅ STEP 4: Calculate pricing for all products (in-memory)
            var priceInfos = new List<ProductPriceInfoDTO>();

            foreach (var productId in productIds)
            {
                try
                {
                    if (!productLookup.TryGetValue(productId, out var product))
                    {
                        _logger.LogWarning("Product not found: {ProductId}", productId);
                        continue;
                    }

                    // Get best event for this product (from pre-loaded data)
                    var bestEvent = productEventLookup.TryGetValue(productId, out var events)
                        ? events.OrderByDescending(e => e.Priority)
                                .ThenByDescending(e => e.DiscountValue)
                                .ThenBy(e => e.CreatedAt)
                                .FirstOrDefault()
                        : null;

                    // Calculate pricing (pure in-memory operation)
                    var priceInfo = CalculateEffectivePriceInMemory(product, bestEvent);
                    priceInfos.Add(priceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to calculate price for product {ProductId}", productId);
                    
                    // Add fallback price info
                    if (productLookup.TryGetValue(productId, out var fallbackProduct))
                    {
                        priceInfos.Add(CreateFallbackPriceInfo(fallbackProduct));
                    }
                }
            }

            return priceInfos;
        }

        // ✅ HELPER METHOD: Build event lookup (simplified - no complex rules)
        private Dictionary<int, List<BannerEventSpecial>> BuildProductEventLookup(List<int> productIds, IEnumerable<BannerEventSpecial> activeEvents)
        {
            var productEventLookup = new Dictionary<int, List<BannerEventSpecial>>();

            foreach (var eventItem in activeEvents)
            {
                try
                {
                    List<int> applicableProductIds;

                    if (!eventItem.EventProducts?.Any() == true)
                    {
                        // Global event - applies to all products
                        applicableProductIds = productIds.ToList();
                        _logger.LogDebug("Global event {EventId} applies to {Count} products", eventItem.Id, applicableProductIds.Count);
                    }
                    else
                    {
                        // Product-specific events
                        applicableProductIds = eventItem.EventProducts!
                            .Where(ep => productIds.Contains(ep.ProductId))
                            .Select(ep => ep.ProductId)
                            .ToList();
                        
                        _logger.LogDebug("Product-specific event {EventId} applies to {Count} products", eventItem.Id, applicableProductIds.Count);
                    }

                    // Add to lookup for applicable products
                    foreach (var productId in applicableProductIds)
                    {
                        if (!productEventLookup.ContainsKey(productId))
                            productEventLookup[productId] = new List<BannerEventSpecial>();
                        
                        productEventLookup[productId].Add(eventItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing event {EventId}", eventItem.Id);
                }
            }

            return productEventLookup;
        }

        // ✅ CORE METHOD 4: Get Best Event (Simplified)
        public async Task<BannerEventSpecial?> GetBestActiveEventForProductAsync(int productId, int? userId = null, CancellationToken cancellationToken = default)
        {
            var nowUtc = _nepalTimeZoneService.GetUtcCurrentTime();

            _logger.LogDebug("Looking for active events for product {ProductId} at {UtcTime}",
               productId, nowUtc.ToString("yyyy-MM-dd HH:mm:ss"));

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

            if (!activeEvents.Any())
            {
                _logger.LogDebug("No active events found");
                return null;
            }

            var applicableEvents = new List<BannerEventSpecial>();
            foreach (var eventItem in activeEvents)
            {
                // Check if event applies to this product
                bool isApplicable = false;
                if (!eventItem.EventProducts?.Any() == true)
                {
                    // Global event - applies to all products
                    _logger.LogDebug("Global event {EventId} applies to all products", eventItem.Id);
                    isApplicable = true;
                }
                else if (eventItem.EventProducts?.Any(ep => ep.ProductId == productId) == true)
                {
                    _logger.LogDebug("Product-specific event {EventId} includes product {ProductId}", eventItem.Id, productId);
                    isApplicable = true;
                }

                if (isApplicable)
                {
                    applicableEvents.Add(eventItem);
                }
            }

            if (!applicableEvents.Any())
            {
                _logger.LogDebug("No applicable events found for product {ProductId}", productId);
                return null;
            }

            var bestEvent = applicableEvents.OrderByDescending(e => e.Priority)
                                          .ThenByDescending(e => e.DiscountValue)
                                          .ThenBy(e => e.CreatedAt)
                                          .First();

            _logger.LogInformation("✅ Selected best event for product {ProductId}: {EventName} " +
                                 "(Priority: {Priority}, Discount: {DiscountValue}%)",
                productId, bestEvent.Name, bestEvent.Priority, bestEvent.DiscountValue);

            return bestEvent;
        }

        // ✅ CORE METHOD 5: Calculate Discount (Simplified)
        private DiscountResult CalculateDiscount(Product product, BannerEventSpecial activeEvent)
        {
            _logger.LogDebug("Calculating discount for Product: {ProductId}, Event: {EventId}", product.Id, activeEvent.Id);

            var basePrice = product.DiscountPrice ?? product.MarketPrice;
            var hasProductDiscount = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice;
            
            var result = new DiscountResult
            {
                OriginalPrice = product.MarketPrice,
                BasePrice = basePrice,
                FinalPrice = basePrice,
                HasDiscount = hasProductDiscount,
                ProductDiscountAmount = hasProductDiscount ? product.MarketPrice - basePrice : 0,
                EventDiscountAmount = 0,
                PromotionType = activeEvent.PromotionType
            };

            if (activeEvent == null)
            {
                _logger.LogDebug("No active event provided. Using base price: Rs.{BasePrice}", basePrice);
                return result;
            }

            // Get discount value (specific or general)
            var specificDiscount = activeEvent.EventProducts?.FirstOrDefault(ep => ep.ProductId == product.Id)?.SpecificDiscount;
            var discountValue = specificDiscount ?? activeEvent.DiscountValue;

            // Calculate discount based on promotion type
            decimal eventDiscountAmount = 0;
            string promotionDescription = "";

            switch (activeEvent.PromotionType)
            {
                case PromotionType.Percentage:
                    if (discountValue > 0 && discountValue <= 100)
                    {
                        eventDiscountAmount = (basePrice * discountValue) / 100;
                        promotionDescription = $"{discountValue}% OFF";
                    }
                    break;
                    
                case PromotionType.FixedAmount:
                    if (discountValue > 0)
                    {
                        eventDiscountAmount = Math.Min(discountValue, basePrice);
                        promotionDescription = $"Rs.{discountValue} OFF";
                    }
                    break;
                    
                case PromotionType.BuyOneGetOne:
                    eventDiscountAmount = basePrice * 0.5m;
                    promotionDescription = "Buy One Get One";
                    break;
                    
                case PromotionType.FreeShipping:
                    eventDiscountAmount = 0;
                    promotionDescription = "Free Shipping";
                    result.HasFreeShipping = true;
                    break;
                    
                case PromotionType.Bundle:
                    if (discountValue > 0)
                    {
                        eventDiscountAmount = (basePrice * discountValue) / 100;
                        promotionDescription = $"Bundle {discountValue}% OFF";
                    }
                    break;
                    
                default:
                    _logger.LogWarning("Unknown promotion type: {PromotionType}", activeEvent.PromotionType);
                    break;
            }

            // Apply maximum discount cap if set
            if (activeEvent.MaxDiscountAmount.HasValue && eventDiscountAmount > activeEvent.MaxDiscountAmount.Value)
            {
                eventDiscountAmount = activeEvent.MaxDiscountAmount.Value;
            }

            // Calculate final price
            var finalPrice = Math.Max(0, basePrice - eventDiscountAmount);
            var hasEventDiscount = eventDiscountAmount > 0 && finalPrice < basePrice;

            // Update result
            result.FinalPrice = finalPrice;
            result.HasDiscount = hasProductDiscount || hasEventDiscount || result.HasFreeShipping;
            result.EventDiscountAmount = eventDiscountAmount;
            result.TotalDiscountAmount = result.ProductDiscountAmount + eventDiscountAmount;
            result.PromotionDescription = promotionDescription;

            _logger.LogDebug("✅ Discount calculated: Rs.{OriginalPrice} → Rs.{FinalPrice} (Total saved: Rs.{TotalDiscount})",
                product.MarketPrice, finalPrice, result.TotalDiscountAmount);

            return result;
        }

        // ✅ HELPER METHOD: Pure in-memory pricing calculation
        private ProductPriceInfoDTO CalculateEffectivePriceInMemory(Product product, BannerEventSpecial? bestEvent)
        {
            var basePrice = product.DiscountPrice ?? product.MarketPrice;

            var priceInfo = new ProductPriceInfoDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OriginalPrice = product.MarketPrice,
                BasePrice = basePrice,
                EffectivePrice = basePrice,
                EventDiscountAmount = 0,
                ProductDiscountAmount = product.DiscountPrice.HasValue ? product.MarketPrice - basePrice : 0,
                HasProductDiscount = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice,
                CalculatedAt = DateTime.UtcNow
            };

            // Apply event discount if available
            if (bestEvent != null)
            {
                try
                {
                    var discountResult = CalculateDiscount(product, bestEvent);
                    ApplyDiscountToPriceInfo(priceInfo, discountResult, bestEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error applying event {EventId} to product {ProductId}", bestEvent.Id, product.Id);
                }
            }

            return priceInfo;
        }

        // ✅ HELPER METHOD: Apply discount result to price info
        private void ApplyDiscountToPriceInfo(ProductPriceInfoDTO priceInfo, DiscountResult discountResult, BannerEventSpecial activeEvent)
        {
            if (discountResult.HasEventDiscount || discountResult.HasFreeShipping)
            {
                priceInfo.EffectivePrice = discountResult.FinalPrice;
                priceInfo.EventDiscountAmount = discountResult.EventDiscountAmount;
                priceInfo.HasEventDiscount = discountResult.HasEventDiscount;
                priceInfo.IsOnSale = discountResult.HasDiscount;

                // Event details
                priceInfo.AppliedEventId = activeEvent.Id;
                priceInfo.ActiveEventId = activeEvent.Id;
                priceInfo.ActiveEventName = activeEvent.Name;
                priceInfo.AppliedEventName = activeEvent.Name;
                priceInfo.EventTagLine = activeEvent.TagLine;
                priceInfo.PromotionType = activeEvent.PromotionType;
                priceInfo.EventStartDate = activeEvent.StartDate;
                priceInfo.EventEndDate = activeEvent.EndDate;
                priceInfo.HasActiveEvent = true;
                priceInfo.IsEventActive = true;

                // Time calculations
                var timeRemaining = activeEvent.EndDate - DateTime.UtcNow;
                priceInfo.EventTimeRemaining = timeRemaining > TimeSpan.Zero ? timeRemaining : null;
                priceInfo.IsEventExpiringSoon = timeRemaining.TotalHours <= 24 && timeRemaining > TimeSpan.Zero;

                // Display formatting
                priceInfo.FormattedSavings = priceInfo.TotalDiscountAmount > 0
                    ? $"Save Rs.{priceInfo.TotalDiscountAmount:F2}"
                    : "";

                priceInfo.FormattedDiscountBreakdown = GenerateDiscountBreakdown(discountResult);
                priceInfo.EventStatus = GenerateEventStatus(activeEvent, timeRemaining);

                // Free shipping
                if (discountResult.HasFreeShipping)
                {
                    priceInfo.HasFreeShipping = true;
                    priceInfo.FormattedSavings = priceInfo.TotalDiscountAmount > 0
                        ? $"{priceInfo.FormattedSavings} + Free Shipping"
                        : "Free Shipping";
                }
            }
        }

        // ✅ SUPPORTING METHODS
        public async Task<bool> CanUserUseEventAsync(int eventId, int? userId, CancellationToken cancellationToken = default)
        {
            if (!userId.HasValue) return true; // Anonymous users can use events

            try
            {
                var eventItem = await _unitOfWork.BannerEventSpecials.GetAsync(
                    predicate: e => e.Id == eventId && e.IsActive && !e.IsDeleted,
                    cancellationToken: cancellationToken);

                if (eventItem == null) return false;

                // Basic usage limit check (simplified)
                if (eventItem.CurrentUsageCount >= eventItem.MaxUsageCount) return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating event usage for event {EventId}, user {UserId}", eventId, userId);
                return false;
            }
        }

        public async Task<List<int>> GetEventHighlightedProductIdsAsync(CancellationToken cancellationToken = default)
        {
            var nowUtc = _nepalTimeZoneService.GetUtcCurrentTime();

            try
            {
                var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                    predicate: e => e.IsActive &&
                                   !e.IsDeleted &&
                                   e.Status == EventStatus.Active &&
                                   e.StartDate <= nowUtc &&
                                   e.EndDate >= nowUtc,
                    includeProperties: "EventProducts",
                    cancellationToken: cancellationToken);

                var productIds = new List<int>();

                foreach (var eventItem in activeEvents)
                {
                    if (eventItem.EventProducts?.Any() == true)
                    {
                        productIds.AddRange(eventItem.EventProducts.Select(ep => ep.ProductId));
                    }
                    else
                    {
                        // Global event - get limited product IDs for performance
                        var allProducts = await _unitOfWork.Products.GetAllAsync(
                            predicate: p => !p.IsDeleted,
                            take: 50,
                            cancellationToken: cancellationToken);

                        productIds.AddRange(allProducts.Select(p => p.Id));
                    }
                }

                return productIds.Distinct().ToList();
            }
            catch (Exception ex)
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

        // ✅ CACHE METHODS
        public async Task RefreshPricesForEventAsync(int eventId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing prices for event {EventId}", eventId);
            await _hybridCacheService.RemoveByPatternAsync("pricing:*", cancellationToken);
        }

        public async Task InvalidatePriceCacheAsync(int productId)
        {
            try
            {
                await _hybridCacheService.RemoveByPatternAsync($"pricing:product:{productId}:*");
                _logger.LogDebug("Invalidated price cache for product {ProductId}", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating price cache for product {ProductId}", productId);
            }
        }

        public async Task InvalidateAllPriceCacheAsync()
        {
            await _hybridCacheService.RemoveByPatternAsync("pricing:*");
            _logger.LogInformation("Invalidated ALL price cache");
        }

        // ✅ CART-SPECIFIC METHODS
        public async Task<ProductPriceInfoDTO> GetCartPriceAsync(int productId, int quantity, int? userId = null, CancellationToken cancellationToken = default)
        {
            var priceInfo = await GetEffectivePriceAsync(productId, userId, cancellationToken);

            // Stock validation for cart
            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
            if (product != null)
            {
                var availableStock = product.StockQuantity - product.ReservedStock;
                var canReserve = availableStock >= quantity;

                priceInfo.WithStockInfo(availableStock, canReserve);
            }

            return priceInfo;
        }

        public async Task<List<ProductPriceInfoDTO>> GetCartPricesAsync(List<CartPriceRequestDTO> requests, int? userId = null, CancellationToken cancellationToken = default)
        {
            var results = new List<ProductPriceInfoDTO>();

            foreach (var request in requests)
            {
                try
                {
                    var priceInfo = await GetCartPriceAsync(request.ProductId, request.Quantity, userId, cancellationToken);

                    // Price protection check
                    if (request.MaxAcceptablePrice.HasValue && priceInfo.EffectivePrice > request.MaxAcceptablePrice.Value)
                    {
                        priceInfo.IsPriceStable = false;
                        priceInfo.StockMessage = $"Price changed. Current: Rs.{priceInfo.EffectivePrice:F2}, Max: Rs.{request.MaxAcceptablePrice:F2}";
                    }

                    results.Add(priceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting cart price for product {ProductId}", request.ProductId);
                    results.Add(new ProductPriceInfoDTO
                    {
                        ProductId = request.ProductId,
                        IsStockAvailable = false,
                        IsPriceStable = false,
                        StockMessage = "Error calculating price"
                    });
                }
            }

            return results;
        }

        public async Task<bool> ValidateCartPriceAsync(int productId, decimal expectedPrice, int? userId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentPrice = await GetEffectivePriceAsync(productId, userId, cancellationToken);
                var tolerance = 0.01m;

                return Math.Abs(currentPrice.EffectivePrice - expectedPrice) <= tolerance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart price for product {ProductId}", productId);
                return false;
            }
        }

        // ✅ HELPER METHODS
        private string GenerateDiscountBreakdown(DiscountResult result)
        {
            var breakdown = new List<string>();

            if (result.ProductDiscountAmount > 0)
                breakdown.Add($"Product: -Rs.{result.ProductDiscountAmount:F2}");

            if (result.EventDiscountAmount > 0)
                breakdown.Add($"Event: -Rs.{result.EventDiscountAmount:F2}");

            if (result.HasFreeShipping)
                breakdown.Add("Free Shipping");

            return string.Join(", ", breakdown);
        }

        private string GenerateEventStatus(BannerEventSpecial activeEvent, TimeSpan? timeRemaining)
        {
            if (!timeRemaining.HasValue || timeRemaining.Value <= TimeSpan.Zero)
                return "Event Ended";

            if (timeRemaining.Value.TotalDays > 1)
                return $"Ends in {Math.Ceiling(timeRemaining.Value.TotalDays)} days";

            if (timeRemaining.Value.TotalHours > 1)
                return $"Ends in {Math.Ceiling(timeRemaining.Value.TotalHours)} hours";

            return $"Ends in {Math.Ceiling(timeRemaining.Value.TotalMinutes)} minutes";
        }

        private async Task<List<ProductPriceInfoDTO>> GetEffectivePricesAsyncFallback(List<int> productIds, int? userId, CancellationToken cancellationToken)
        {
            _logger.LogWarning("🔄 Using fallback pricing method for {Count} products", productIds.Count);
            
            var priceInfos = new List<ProductPriceInfoDTO>();
            
            foreach (var productId in productIds.Take(10))
            {
                try
                {
                    var priceInfo = await GetEffectivePriceAsync(productId, userId, cancellationToken);
                    priceInfos.Add(priceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed fallback pricing for product {ProductId}", productId);
                    
                    var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                    if (product != null)
                    {
                        priceInfos.Add(CreateFallbackPriceInfo(product));
                    }
                }
            }
            
            return priceInfos;
        }

        private ProductPriceInfoDTO CreateFallbackPriceInfo(Product product)
        {
            return new ProductPriceInfoDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OriginalPrice = product.MarketPrice,
                EffectivePrice = product.DiscountPrice ?? product.MarketPrice,
                BasePrice = product.DiscountPrice ?? product.MarketPrice,
                CalculatedAt = DateTime.UtcNow,
                HasEventDiscount = false,
                IsOnSale = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice
            };
        }
    }

    // ✅ HELPER CLASS: Discount calculation results
    public class DiscountResult
    {
        public decimal OriginalPrice { get; set; }
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public bool HasDiscount { get; set; }

        public decimal ProductDiscountAmount { get; set; }
        public decimal EventDiscountAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }

        public PromotionType PromotionType { get; set; }
        public string PromotionDescription { get; set; } = "";
        public bool HasFreeShipping { get; set; } = false;

        public bool HasProductDiscount => ProductDiscountAmount > 0;
        public bool HasEventDiscount => EventDiscountAmount > 0;
        public decimal DiscountPercentage => OriginalPrice > 0 ? (TotalDiscountAmount / OriginalPrice) * 100 : 0;
    }
}