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
        private readonly IEventUsageService _eventUsageService;
        
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public ProductPricingService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            INepalTimeZoneService nepalTimeZoneService,
            IHybridCacheService hybridCacheService,
            IEventUsageService eventUsageService,
            ILogger<ProductPricingService> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _nepalTimeZoneService = nepalTimeZoneService;
            _hybridCacheService = hybridCacheService;
            _eventUsageService = eventUsageService;
            _logger = logger;
        }

        // CORE METHOD 1: Single Product Pricing
        public async Task<ProductPriceInfoDTO> GetEffectivePriceAsync(int productId, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting effective price for product ID: {ProductId}", productId);

            // Try hybrid cache first
            var cacheKey = $"pricing:product:{productId}:user:{userId ?? 0}";
            var cachedPricing = await _hybridCacheService.GetAsync<ProductPriceInfoDTO>(cacheKey, cancellationToken);
            
            if (cachedPricing != null)
            {
                _logger.LogDebug("Cache HIT for product {ProductId}", productId);
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

        // CORE METHOD 2: Single Product with Entity - FIXED EVENT LOGIC
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
                CalculatedAt = DateTime.UtcNow,
                HasActiveEvent = false,
                CanUseEvent = false
            };

            try
            {
                // Get the best active event for this product
                var activeEvent = await GetBestActiveEventForProductAsync(product.Id, userId, cancellationToken);

                if (activeEvent != null && userId.HasValue)
                {
                    _logger.LogInformation("Found active event for product {ProductId}: {EventName} (Type: {PromotionType}, Discount: {DiscountValue})",
                        product.Id, activeEvent.Name, activeEvent.PromotionType, activeEvent.DiscountValue);

                    // Check if user can still use this event for this specific product
                    bool canUseEventForThisProduct = true;
                    int userProductEventUsage = 0;
                    int remainingEventUsage = activeEvent.MaxUsagePerUser;

                    if (userId.HasValue)
                    {
                        // FIXED: Check product-specific event usage with correct parameter order
                        userProductEventUsage = await _eventUsageService.GetUserEventUsageCountAsync(activeEvent.Id, userId.Value, product.Id);

                        remainingEventUsage = Math.Max(0, activeEvent.MaxUsagePerUser - userProductEventUsage);
                        canUseEventForThisProduct = remainingEventUsage > 0;
                        
                        _logger.LogInformation("User {UserId} event usage for product {ProductId}: {Used}/{Max} (Remaining: {Remaining})",
                            userId.Value, product.Id, userProductEventUsage, activeEvent.MaxUsagePerUser, remainingEventUsage);
                    }

                    // Apply event pricing based on usage status
                    if (canUseEventForThisProduct)
                    {
                        // User can still use event discount for this product
                        var discountResult = CalculateDiscount(product, activeEvent);

                        if (discountResult.HasEventDiscount || discountResult.HasFreeShipping)
                        {
                            ApplyDiscountToPriceInfo(priceInfo, discountResult, activeEvent);

                            _logger.LogInformation("Applied event pricing to product {ProductId}: " +
                                                "Rs.{OriginalPrice} -> Rs.{FinalPrice} " +
                                                "(Event saved: Rs.{EventSavings}) Event: {EventName}",
                                product.Id, priceInfo.OriginalPrice, priceInfo.EffectivePrice,
                                priceInfo.EventDiscountAmount, activeEvent.Name);
                        }
                    }
                    else
                    {
                        // User has reached event limit for this product - use regular price
                        _logger.LogInformation("User {UserId} reached event limit for product {ProductId}. Using regular price: Rs.{RegularPrice}",
                            userId, product.Id, basePrice);
                        
                        // Set event info but no discount
                        ApplyEventInfoToPriceInfo(priceInfo, activeEvent);
                        priceInfo.EventTagLine = $"Event limit reached for this product ({userProductEventUsage}/{activeEvent.MaxUsagePerUser})";
                        priceInfo.CanUseEvent = false;
                        priceInfo.EventLimitReached = true;
                    }

                    // Set usage information
                    priceInfo.UserEventUsageCount = userProductEventUsage;
                    priceInfo.MaxEventUsagePerUser = activeEvent.MaxUsagePerUser;
                    priceInfo.RemainingEventUsage = remainingEventUsage;
                    priceInfo.CanUseEvent = canUseEventForThisProduct;
                }
                else
                {
                    _logger.LogDebug("No active events found for product {ProductId}. Using base price: Rs.{BasePrice}",
                        product.Id, basePrice);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating effective price for product {ProductId}", product.Id);
            }

            return priceInfo;
        }

        // CORE METHOD 3: Cart-Specific Pricing with Smart Quantity Logic
        public async Task<ProductPriceInfoDTO> GetCartPriceAsync(int productId, int quantity, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting cart price for product {ProductId}, quantity {Quantity}, user {UserId}", 
                productId, quantity, userId);

            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            var basePrice = product.DiscountPrice ?? product.MarketPrice;
            var totalRegularPrice = basePrice * quantity;

            var priceInfo = new ProductPriceInfoDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OriginalPrice = product.MarketPrice,
                BasePrice = basePrice,
                EffectivePrice = totalRegularPrice,
                EventDiscountAmount = 0,
                ProductDiscountAmount = product.DiscountPrice.HasValue ? (product.MarketPrice - basePrice) * quantity : 0,
                HasProductDiscount = product.DiscountPrice.HasValue && product.DiscountPrice < product.MarketPrice,
                RequestedQuantity = quantity,
                CalculatedAt = DateTime.UtcNow
            };

            try
            {
                // Get active event
                var activeEvent = await GetBestActiveEventForProductAsync(productId, userId, cancellationToken);

                if (activeEvent != null && userId.HasValue)
                {
                    // SMART QUANTITY PRICING: Split between event price and regular price
                    var userProductEventUsage = await _eventUsageService.GetUserProductEventUsageCountAsync(
                        activeEvent.Id, userId.Value, productId);
                    
                    var remainingEventSlots = Math.Max(0, activeEvent.MaxUsagePerUser - userProductEventUsage);
                    var eventEligibleQuantity = Math.Min(quantity, remainingEventSlots);
                    var regularPriceQuantity = quantity - eventEligibleQuantity;

                    _logger.LogInformation("Cart pricing breakdown for product {ProductId}: " +
                                         "Requested={Requested}, EventEligible={EventEligible}, Regular={Regular}, " +
                                         "UserUsage={UserUsage}/{MaxUsage}",
                                         productId, quantity, eventEligibleQuantity, regularPriceQuantity,
                                         userProductEventUsage, activeEvent.MaxUsagePerUser);

                    decimal totalEventPrice = 0;
                    decimal totalRegularPriceAdjusted = regularPriceQuantity * basePrice;
                    decimal totalEventDiscount = 0;

                    // Calculate event pricing for eligible quantity
                    if (eventEligibleQuantity > 0)
                    {
                        var discountResult = CalculateDiscount(product, activeEvent);
                        var eventUnitPrice = discountResult.FinalPrice;
                        totalEventPrice = eventUnitPrice * eventEligibleQuantity;
                        totalEventDiscount = discountResult.EventDiscountAmount * eventEligibleQuantity;

                        _logger.LogDebug("Event pricing: {EventQty}x Rs.{EventUnitPrice} = Rs.{EventTotal} " +
                                       "(Saved Rs.{EventSavings})",
                                       eventEligibleQuantity, eventUnitPrice, totalEventPrice, totalEventDiscount);
                    }

                    // Set final pricing
                    priceInfo.EffectivePrice = totalEventPrice + totalRegularPriceAdjusted;
                    priceInfo.EventDiscountAmount = totalEventDiscount;
                    priceInfo.EventEligibleQuantity = eventEligibleQuantity;
                    priceInfo.RegularPriceQuantity = regularPriceQuantity;

                    // Set event details
                    if (eventEligibleQuantity > 0)
                    {
                        ApplyEventInfoToPriceInfo(priceInfo, activeEvent);
                        priceInfo.CanUseEvent = true;
                    }
                    else
                    {
                        priceInfo.EventTagLine = $"Event limit reached for this product ({userProductEventUsage}/{activeEvent.MaxUsagePerUser})";
                        priceInfo.CanUseEvent = false;
                        priceInfo.EventLimitReached = true;
                    }

                    // Set usage information
                    priceInfo.UserEventUsageCount = userProductEventUsage;
                    priceInfo.MaxEventUsagePerUser = activeEvent.MaxUsagePerUser;
                    priceInfo.RemainingEventUsage = remainingEventSlots;

                    _logger.LogInformation("Final cart price for product {ProductId}: Rs.{FinalPrice} " +
                                         "(Event: {EventQty}x items, Regular: {RegularQty}x items)",
                                         productId, priceInfo.EffectivePrice, eventEligibleQuantity, regularPriceQuantity);
                }

                // Add stock validation
                var availableStock = product.StockQuantity - product.ReservedStock;
                var canReserve = availableStock >= quantity;
                priceInfo.WithStockInfo(availableStock, canReserve);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cart price for product {ProductId}", productId);
                // Continue with regular pricing
            }

            return priceInfo;
        }

        // CORE METHOD 4: Get Best Event (Optimized)
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

            _logger.LogInformation("Selected best event for product {ProductId}: {EventName} " +
                                 "(Priority: {Priority}, Discount: {DiscountValue}%)",
                productId, bestEvent.Name, bestEvent.Priority, bestEvent.DiscountValue);

            return bestEvent;
        }

        // CORE METHOD 5: Calculate Discount
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

            _logger.LogDebug("Discount calculated: Rs.{OriginalPrice} -> Rs.{FinalPrice} (Total saved: Rs.{TotalDiscount})",
                product.MarketPrice, finalPrice, result.TotalDiscountAmount);

            return result;
        }

        // HELPER METHOD: Apply discount result to price info
        private void ApplyDiscountToPriceInfo(ProductPriceInfoDTO priceInfo, DiscountResult discountResult, BannerEventSpecial activeEvent)
        {
            if (discountResult.HasEventDiscount || discountResult.HasFreeShipping)
            {
                priceInfo.EffectivePrice = discountResult.FinalPrice;
                priceInfo.EventDiscountAmount = discountResult.EventDiscountAmount;
                priceInfo.HasEventDiscount = discountResult.HasEventDiscount;
                priceInfo.IsOnSale = discountResult.HasDiscount;

                ApplyEventInfoToPriceInfo(priceInfo, activeEvent);

                // Free shipping
                if (discountResult.HasFreeShipping)
                {
                    priceInfo.HasFreeShipping = true;
                }
            }
        }

        // HELPER METHOD: Apply event information
        private void ApplyEventInfoToPriceInfo(ProductPriceInfoDTO priceInfo, BannerEventSpecial activeEvent)
        {
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

            priceInfo.EventStatus = GenerateEventStatus(activeEvent, timeRemaining);
        }

        // BULK AND UTILITY METHODS
        public async Task<List<ProductPriceInfoDTO>> GetEffectivePricesAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting effective prices for {Count} products", productIds.Count);

            if (!productIds.Any()) return new List<ProductPriceInfoDTO>();

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
                    _logger.LogError(ex, "Error getting price for product {ProductId}", productId);
                    
                    var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
                    if (product != null)
                    {
                        priceInfos.Add(CreateFallbackPriceInfo(product));
                    }
                }
            }
            
            return priceInfos;
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

        public async Task<bool> IsProductOnSaleAsync(int productId, CancellationToken cancellationToken = default)
        {
            var priceInfo = await GetEffectivePriceAsync(productId, null, cancellationToken);
            return priceInfo.HasDiscount;
        }

        // CACHE METHODS
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

        // HELPER METHODS
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

    
        public async Task<List<int>> GetEventHighlightedProductIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var nowUtc = _nepalTimeZoneService.GetUtcCurrentTime();
                
                _logger.LogDebug("Getting event highlighted products at {UtcTime}", nowUtc);
                
                // Get all active events with their associated products
                var activeEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                    predicate: e => e.IsActive &&
                            !e.IsDeleted &&
                            e.Status == EventStatus.Active &&
                            e.StartDate <= nowUtc &&
                            e.EndDate >= nowUtc &&
                            e.CurrentUsageCount < e.MaxUsageCount,
                    includeProperties: "EventProducts,EventProducts.Product", // Include Product navigation
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Found {Count} active events", activeEvents.Count());

                var productIds = new HashSet<int>();

                foreach (var eventItem in activeEvents)
                {
                    if (eventItem.EventProducts?.Any() == true)
                    {
                        // Product-specific event - add only the specific products that are valid
                        foreach (var eventProduct in eventItem.EventProducts)
                        {
                            // Validate that the product exists and is valid
                            if (eventProduct.Product != null && 
                                !eventProduct.Product.IsDeleted && 
                                eventProduct.Product.StockQuantity > 0)
                            {
                                productIds.Add(eventProduct.ProductId);
                                _logger.LogDebug("Added product {ProductId} from specific event {EventId}", 
                                    eventProduct.ProductId, eventItem.Id);
                            }
                        }
                    }
                    else
                    {
                        // Global event - applies to all valid products
                        _logger.LogDebug("Processing global event {EventId}: {EventName}", eventItem.Id, eventItem.Name);
                        
                        // Get all valid products for global events (limited for performance)
                        var allValidProducts = await _unitOfWork.Products.GetAllAsync(
                            predicate: p => !p.IsDeleted && 
                                        p.StockQuantity > 0 && 
                                        p.MarketPrice > 0, // Only products with valid pricing
                            orderBy: q => q.OrderByDescending(p => p.Id), // Show newest first
                            take: 100, // Limit to prevent performance issues
                            cancellationToken: cancellationToken);
                        
                        foreach (var product in allValidProducts)
                        {
                            productIds.Add(product.Id);
                        }
                        
                        _logger.LogDebug("Added {Count} products from global event {EventId}", 
                            allValidProducts.Count(), eventItem.Id);
                        
                        // Only process first global event to avoid duplicates
                        break;
                    }
                }

                var finalProductIds = productIds.ToList();
                _logger.LogInformation("Found {Count} highlighted products for active events", finalProductIds.Count);
                
                return finalProductIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event highlighted product IDs");
                return new List<int>();
            }
        }
                
        public async Task InvalidateAllPriceCacheAsync()
        {
            try
            {
                await _hybridCacheService.RemoveByPatternAsync("pricing:*");
                _logger.LogInformation("Invalidated all price cache entries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating all price cache");
            }
        }
    }

    // HELPER CLASS: Discount calculation results
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