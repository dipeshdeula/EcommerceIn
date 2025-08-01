////filepath: e:\EcomerceDeployPostgres\EcommerceBackendAPI\Infrastructure\Persistence\Services\ProductPricingService.cs
using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Dto.ProductDTOs;
using Application.Extension;
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
            _nepalTimeZoneService = nepalTimeZoneService;
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
                EventDiscountAmount = 0,
                ProductDiscountAmount = product.DiscountPrice.HasValue ? product.MarketPrice - basePrice : 0,
                HasProductDiscount = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice,
                CalculatedAt = DateTime.UtcNow


            };

            try
            {
                // ✅ Get the best active event for this product
                var activeEvent = await GetBestActiveEventForProductAsync(product.Id, userId, cancellationToken);

                if (activeEvent != null)
                {
                    _logger.LogInformation("🎉 Found active event for product {ProductId}: {EventName} (Type: {PromotionType}, Discount: {DiscountValue})",
                        product.Id, activeEvent.Name, activeEvent.PromotionType, activeEvent.DiscountValue);

                    // ✅ Calculate discount
                    var discountResult = CalculateDiscount(product, activeEvent);

                    _logger.LogDebug("📊 Discount result: Original=Rs.{OriginalPrice}, Base=Rs.{BasePrice}, Final=Rs.{FinalPrice}, " +
                                "ProductDiscount=Rs.{ProductDiscount}, EventDiscount=Rs.{EventDiscount}",
                        discountResult.OriginalPrice, discountResult.BasePrice, discountResult.FinalPrice,
                        discountResult.ProductDiscountAmount, discountResult.EventDiscountAmount);

                    if (discountResult.HasEventDiscount || discountResult.HasFreeShipping)
                    {
                        priceInfo.EffectivePrice = discountResult.FinalPrice;
                        priceInfo.EventDiscountAmount = discountResult.EventDiscountAmount;
                        priceInfo.HasEventDiscount = discountResult.HasEventDiscount;
                        priceInfo.IsOnSale = discountResult.HasDiscount;

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

                        // calculate time remaining
                        var timeRemaining = activeEvent.EndDate - DateTime.UtcNow;
                        priceInfo.EventTimeRemaining = timeRemaining > TimeSpan.Zero ? timeRemaining : null;
                        priceInfo.IsEventExpiringSoon = timeRemaining.TotalHours <= 24 && timeRemaining > TimeSpan.Zero;                     

                

                        // Format display strings
                        priceInfo.FormattedSavings = priceInfo.TotalDiscountAmount > 0
                            ? $"Save Rs.{priceInfo.TotalDiscountAmount:F2}"
                            : "";

                        priceInfo.FormattedDiscountBreakdown = GenerateDiscountBreakdown(discountResult);
                        priceInfo.EventStatus = GenerateEventStatus(activeEvent, timeRemaining);

                        // Free shipping flag
                        if (discountResult.HasFreeShipping)
                        {
                            priceInfo.HasFreeShipping = true;
                            priceInfo.FormattedSavings = priceInfo.TotalDiscountAmount > 0
                                ? $"{priceInfo.FormattedSavings} + Free Shipping"
                                : "Free Shipping";
                        }

                        _logger.LogInformation("Applied event pricing to product {ProductId}: " +
                                            "Rs.{OriginalPrice} → Rs.{FinalPrice} " +
                                            "(Total saved: Rs.{TotalSavings}) Event: {EventName}",
                            product.Id, priceInfo.OriginalPrice, priceInfo.EffectivePrice,
                            priceInfo.TotalDiscountAmount, activeEvent.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Event found but no discount applied for product {ProductId}", product.Id);
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
                               (e.Status == EventStatus.Active ||
                               e.Status == EventStatus.Scheduled) &&
                               e.StartDate <= nowUtc &&
                               e.EndDate >= nowUtc &&
                               e.CurrentUsageCount < e.MaxUsageCount,
                includeProperties: "EventProducts,Rules",
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
                _logger.LogDebug("checking event {EventId}:{EventName}", eventItem.Id, eventItem.Name);

                // check if event applies to this product
                bool isApplicable = false;
                if (!eventItem.EventProducts?.Any() == true)
                {
                    // Global event -applies to all products
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
                    _logger.LogDebug("Event {EventId} is applicable to product {ProductId}", eventItem.Id, productId);
                }
                else
                {
                    _logger.LogDebug("Event {EventId} is NOT applicable to product {ProductId}", eventItem.Id, productId);
                }
            }

            if (!applicableEvents.Any())
            {
                _logger.LogDebug(" No applicable events found for product {ProductId}", productId);
                return null;
            }

            var bestEvent = applicableEvents.OrderByDescending(e => e.Priority) // Higher priority first
                                            .ThenByDescending(e => e.DiscountValue) // Higher discount second
                                            .ThenBy(e => e.CreatedAt) // Earlier created if tie
                                            .First();

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
                EventDiscountAmount = 0,
                PromotionType = activeEvent.PromotionType
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

            // calculate discount based n promotion type
            decimal eventDiscountAmount = 0;
            string promotionDescription = "";

            switch (activeEvent.PromotionType)
            {
                case PromotionType.Percentage:
                    if (discountValue > 0 && discountValue <= 100)
                    {
                        eventDiscountAmount = (basePrice * discountValue) / 100;
                        promotionDescription = $"{discountValue}% OFF";
                        _logger.LogDebug("Percentage discount : Rs.{BasePrice}*{DiscountValue}%=Rs.{EventDiscountAmount}",
                        basePrice, discountValue, eventDiscountAmount);
                    }
                    break;
                case PromotionType.FixedAmount:
                    if (discountValue > 0)
                    {
                        eventDiscountAmount = Math.Min(discountValue, basePrice); // Can't discount more than price
                        promotionDescription = $"Rs.{discountValue} OFF";
                        _logger.LogDebug("Fixed amount discount : Rs.{EventDiscountAmount}", eventDiscountAmount);

                    }
                    break;
                case PromotionType.BuyOneGetOne:
                    // 50% off for simplicity
                    eventDiscountAmount = basePrice * 0.5m;
                    promotionDescription = "Buy One Get One";
                    _logger.LogDebug("BOGO discount : Rs.{BasePrice}*50%=Rs.{EventDiscountAmount}", basePrice, eventDiscountAmount);
                    break;
                case PromotionType.FreeShipping:
                    // Free shipping - no price discount but special handling needed
                    eventDiscountAmount = 0; // No price discount
                    promotionDescription = "Free Shipping";
                    result.HasFreeShipping = true;
                    _logger.LogDebug("Free shipping applied (no price discount)");
                    break;

                case PromotionType.Bundle:
                    // Bundle discount - apply percentage discount for bundle deals
                    if (discountValue > 0)
                    {
                        eventDiscountAmount = (basePrice * discountValue) / 100;
                        promotionDescription = $"Bundle {discountValue}% OFF";
                        _logger.LogDebug("Bundle discount: Rs.{BasePrice}*{DiscountValue}%=Rs.{EventDiscountAmount}",
                            basePrice, discountValue, eventDiscountAmount);
                    }
                    break;
                default:
                    _logger.LogWarning("Unknown promotion type : {PromotionType}, activeEvent: {ActiveEvent}",
                        activeEvent.PromotionType, activeEvent);
                    break;
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
            var hasEventDiscount = eventDiscountAmount > 0 && finalPrice < basePrice || result.HasFreeShipping;

            // Update result with final price and discount info
            result.FinalPrice = finalPrice;
            result.HasDiscount = hasProductDiscount || hasEventDiscount;
            result.EventDiscountAmount = eventDiscountAmount;
            result.TotalDiscountAmount = result.ProductDiscountAmount + eventDiscountAmount;
            result.PromotionDescription = promotionDescription;


            _logger.LogInformation("✅ DISCOUNT CALCULATED: Rs.{OriginalPrice} → Rs.{BasePrice} → Rs.{FinalPrice} " +
                    "(Product: -Rs.{ProductDiscount}, Event: -Rs.{EventDiscount}, Total: -Rs.{TotalDiscount}) " +
                    "Promotion: {PromotionDescription}",
            product.MarketPrice, basePrice, finalPrice,
            result.ProductDiscountAmount, eventDiscountAmount, result.TotalDiscountAmount,
            promotionDescription);
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

            try {

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
            } catch (Exception ex)
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
            await Task.CompletedTask;
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

        public PromotionType PromotionType { get; set; }
        public string PromotionDescription { get; set; } = "";
        public bool HasFreeShipping { get; set; } = false;
        public bool HasBuyOneGetOne { get; set; } = false;

        // Convenience properties for easy access
        public bool HasProductDiscount => ProductDiscountAmount > 0;
        public bool HasEventDiscount => EventDiscountAmount > 0;
        public decimal DiscountPercentage => OriginalPrice > 0 ? (TotalDiscountAmount / OriginalPrice) * 100 : 0;
    }
}