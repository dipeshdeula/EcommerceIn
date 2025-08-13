using System.Diagnostics;
using Application.Common.Helper;
using Application.Dto;
using Application.Dto.AddressDTOs;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Dto.BilItemDTOs;
using Application.Dto.CartItemDTOs;
using Application.Dto.CategoryDTOs;
using Application.Dto.CompanyDTOs;
using Application.Dto.LocationDTOs;
using Application.Dto.OrderDTOs;
using Application.Dto.PaymentDTOs;
using Application.Dto.PaymentMethodDTOs;
using Application.Dto.ProductDTOs;
using Application.Dto.PromoCodeDTOs;
using Application.Dto.Shared;
using Application.Dto.ShippingDTOs;
using Application.Dto.StoreDTOs;
using Application.Dto.UserDTOs;
using Application.Dto.WhishListDTOs;
using Application.Features.BannerSpecialEvent.Module;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;

namespace Application.Extension
{
    public static class MappingExtensions
    {
        public static UserDTO ToDTO(this User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Contact = user.Contact,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl,
                IsDeleted = user.IsDeleted,
                Role = user.Role,
                Addresses = user.Addresses.Select(a => a.ToDTO()).ToList()
            };
        }

        public static AddressDTO ToDTO(this Address address)
        {
            return new AddressDTO
            {
                Id = address.Id,
                Label = address.Label,
                Street = address.Street,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault
            };
        }

        public static CompanyInfoDTO ToDTO(this CompanyInfo companyInfo)
        {
            return new CompanyInfoDTO
            {
                Id = companyInfo.Id,
                Name = companyInfo.Name,
                Email = companyInfo.Email,
                Contact = companyInfo.Contact,
                RegistrationNumber = companyInfo.RegistrationNumber,
                RegisteredPanNumber = companyInfo.RegisteredPanNumber,
                RegisteredVatNumber = companyInfo.RegisteredVatNumber,
                Street = companyInfo.Street,
                City = companyInfo.City,
                Province = companyInfo.Province,
                PostalCode = companyInfo.PostalCode,
                CreatedAt = companyInfo.CreatedAt,
                UpdateAt = companyInfo.UpdateAt,
                WebsiteUrl = companyInfo.WebsiteUrl,
                LogoUrl = companyInfo.LogoUrl,
                IsDeleted = companyInfo.IsDeleted,

            };
        }

        public static CategoryDTO ToDTO(this Category category)
        {
            return new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsDeleted = category.IsDeleted,
                SubCategories = category.SubCategories.Select(sc => sc.ToDTO()).ToList() // Map subcategories

            };
        }

        // Map SubCategory to SubCategoryDTO
        public static SubCategoryDTO ToDTO(this SubCategory subCategory)
        {
            return new SubCategoryDTO
            {
                Id = subCategory.Id,
                Name = subCategory.Name,
                Slug = subCategory.Slug,
                Description = subCategory.Description,
                ImageUrl = subCategory.ImageUrl,
                IsDeleted = subCategory.IsDeleted,
                SubSubCategories = subCategory.SubSubCategories.Select(ssc => ssc.ToDTO()).ToList() // Map sub-subcategories
            };
        }

        // Map SubSubCategory to SubSubCategoryDTO
        public static SubSubCategoryDTO ToDTO(this SubSubCategory subSubCategory)
        {
            return new SubSubCategoryDTO
            {
                Id = subSubCategory.Id,
                Name = subSubCategory.Name,
                Slug = subSubCategory.Slug,
                Description = subSubCategory.Description,
                ImageUrl = subSubCategory.ImageUrl,
                IsDeleted = subSubCategory.IsDeleted,
                SubCategoryId = subSubCategory.SubCategoryId,
                Products = subSubCategory.Products.Select(p => p.ToDTO()).ToList() // Map products
            };
        }

        // Map Product to ProductDTO
        public static ProductDTO ToDTO(this Product product)
        {
            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                MarketPrice = product.MarketPrice,
                CostPrice = product.CostPrice,
                DiscountPrice = product.DiscountPrice,
                DiscountPercentage = product.DiscountPercentage,
                StockQuantity = product.StockQuantity,
                ReservedStock = product.ReservedStock,
                Sku = product.Sku,
                Weight = product.Weight,
                Reviews = product.Reviews,
                Rating = product.Rating,
                Dimensions = product.Dimensions ?? string.Empty,
                IsDeleted = product.IsDeleted,
                CategoryId = product.CategoryId,
                SubSubCategoryId = product.SubSubCategoryId,
                Images = product.Images?.Select(pi => pi.ToDTO()).ToList() ?? new List<ProductImageDTO>(),

                // pricing and stock will be set by services (null initially)
                Pricing = null,
                Stock = null
            };
        }

        // ProductDetailsDTO with all components
        public static ProductDetailsDTO ToDetailsDTO(this Product product, ProductPriceInfoDTO? priceInfo = null)
        {
            return new ProductDetailsDTO
            {
                Product = product.ToDTO(),
                //Pricing = priceInfo?.ToPricingDTO() ?? CreateDefaultPricing(product),
                Stock = product.ToStockDTO()
            };
        }
        public static ProductPricingDTO ToPricingDTO(this ProductPriceInfoDTO priceInfo)
        {
            return new ProductPricingDTO
            {
                ProductId = priceInfo.ProductId,
                ProductName = priceInfo.ProductName,
                OriginalPrice = priceInfo.OriginalPrice,
                BasePrice = priceInfo.BasePrice,
                EffectivePrice = priceInfo.EffectivePrice,

                // MAP DISCOUNT AMOUNTS
                ProductDiscountAmount = priceInfo.RegularDiscountAmount,
                EventDiscountAmount = priceInfo.EventDiscountAmount,
                TotalDiscountAmount = priceInfo.TotalDiscountAmount,
                TotalDiscountPercentage = priceInfo.TotalDiscountPercentage,

                // MAP DISCOUNT FLAGS
                HasProductDiscount = priceInfo.HasProductDiscount,
                HasEventDiscount = priceInfo.HasEventDiscount,
                HasAnyDiscount = priceInfo.HasAnyDiscount,
                IsOnSale = priceInfo.IsOnSale,

                // MAP EVENT INFO
                ActiveEventId = priceInfo.AppliedEventId,
                ActiveEventName = priceInfo.AppliedEventName,
                EventTagLine = priceInfo.EventTagLine,
                PromotionType = priceInfo.PromotionType,
                EventStartDate = priceInfo.EventStartDate,
                EventEndDate = priceInfo.EventEndDate,
                HasActiveEvent = priceInfo.HasActiveEvent,
                IsEventActive = priceInfo.IsEventActive,
                EventTimeRemaining = priceInfo.EventTimeRemaining,
                IsEventExpiringSoon = priceInfo.IsEventExpiringSoon,



                //  MAP FORMATTED STRINGS
                FormattedOriginalPrice = priceInfo.FormattedOriginalPrice,
                FormattedEffectivePrice = priceInfo.FormattedEffectivePrice,
                FormattedSavings = priceInfo.FormattedSavings,
                FormattedDiscountBreakdown = priceInfo.FormattedDiscountBreakdown,
                EventStatus = priceInfo.EventStatus,

                //  MAP SPECIAL FLAGS
                HasFreeShipping = priceInfo.HasFreeShipping,

                //  MAP METADATA
                IsPriceStable = priceInfo.IsPriceStable,
                CalculatedAt = priceInfo.CalculatedAt
            };
        }
        public static ProductStockDTO ToStockDTO(this Product product)
        {
            return new ProductStockDTO
            {
                ProductId = product.Id,
                TotalStock = product.StockQuantity,
                ReservedStock = product.ReservedStock,
                CanReserve = product.CanReserve(1),
                IsAvailableForSale = !product.IsDeleted && product.IsInStock,
                MaxOrderQuantity = Math.Min(product.AvailableStock, 10)
            };
        }

        // Map ProductImage to ProductImageDTO
        public static ProductImageDTO ToDTO(this ProductImage productImage)
        {
            return new ProductImageDTO
            {
                Id = productImage.Id,
                ImageUrl = productImage.ImageUrl,
                ProductId = productImage.ProductId,
                AltText = $"Product image {productImage.Id}",
                IsMain = productImage.Id == productImage.Product?.Images?.FirstOrDefault()?.Id,
                DisplayOrder = productImage.Id
            };
        }
        public static ProductDTO ApplyPricing(this ProductDTO productDTO, ProductPriceInfoDTO? priceInfo)
        {
            if (priceInfo == null)
            {
                //  NO PRICING DATA - Create default pricing
                productDTO.Pricing = CreateDefaultPricing(productDTO);
                return productDTO;
            }

            // APPLY PRICING DATA via composition
            productDTO.Pricing = priceInfo.ToPricingDTO();
            productDTO.DiscountPrice = priceInfo.EffectivePrice; // Final price after all discounts
            productDTO.DiscountPercentage = priceInfo.TotalDiscountPercentage; // Total discount percentage

            productDTO.HasProductDiscount = priceInfo.HasProductDiscount;
            productDTO.ProductDiscountAmount = priceInfo.ProductDiscountAmount;

            productDTO.BasePrice = priceInfo.BasePrice;

            productDTO.FormattedBasePrice = $"Rs. {priceInfo.BasePrice:F2}";
            productDTO.FormattedDiscountAmount = $"Rs. {priceInfo.TotalDiscountAmount:F2}";


            return productDTO;
        }

        // Version 1 : with hybird Cache (primary method)
        public static async Task<List<ProductDTO>> ApplyPricingAsync(
            this List<ProductDTO> productDTOs,
            IProductPricingService pricingService,
            IHybridCacheService? cacheService = null,
            int? userId = null,
            CancellationToken cancellationToken = default)
        {
            if (productDTOs == null || !productDTOs.Any())
                return productDTOs ?? new List<ProductDTO>();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var productIds = productDTOs.Select(p => p.Id).ToList();

                //  STEP 1: Check if we have cache service available
                Dictionary<int, ProductPriceInfoDTO> cachedPricingLookup = new();

                if (cacheService != null)
                {
                    try
                    {
                        // Try to get pricing from cache first
                        var cachedPricing = await cacheService.GetPricingAsync(productIds, userId, cancellationToken);
                        cachedPricingLookup = cachedPricing?.ToDictionary(p => p.ProductId, p => p) ?? new Dictionary<int, ProductPriceInfoDTO>();
                    }
                    catch (Exception cacheEx)
                    {
                        // Cache failure shouldn't break the whole process
                        Console.WriteLine($" CACHE ERROR: {cacheEx.Message}");
                        cachedPricingLookup = new Dictionary<int, ProductPriceInfoDTO>();
                    }
                }

                // STEP 2: Identify missing pricing data
                var missingProductIds = productIds.Where(id => !cachedPricingLookup.ContainsKey(id)).ToList();

                // STEP 3: Fetch missing pricing from service
                if (missingProductIds.Any())
                {
                    try
                    {
                        var freshPricing = await pricingService.GetEffectivePricesAsync(missingProductIds, userId, cancellationToken);

                        if (freshPricing?.Any() == true)
                        {
                            // STEP 4: Cache the fresh pricing (if cache service available)
                            if (cacheService != null)
                            {
                                try
                                {
                                    await cacheService.SetPricingAsync(freshPricing, userId, cancellationToken);
                                }
                                catch (Exception cacheEx)
                                {
                                    // Cache set failure shouldn't break the process
                                    Console.WriteLine($" CACHE SET ERROR: {cacheEx.Message}");
                                }
                            }

                            // Add to lookup
                            foreach (var pricing in freshPricing)
                            {
                                if (pricing != null) // Null safety
                                {
                                    cachedPricingLookup[pricing.ProductId] = pricing;
                                }
                            }
                        }
                    }
                    catch (Exception pricingEx)
                    {
                        Console.WriteLine($" PRICING SERVICE ERROR: {pricingEx.Message}");
                        // Continue with available pricing data
                    }
                }

                //  STEP 5: Apply pricing to all products with null safety
                foreach (var productDTO in productDTOs)
                {
                    if (productDTO != null) //  Null safety
                    {
                        if (cachedPricingLookup.TryGetValue(productDTO.Id, out var priceInfo))
                        {
                            productDTO.ApplyPricing(priceInfo);
                        }
                        else
                        {
                            productDTO.ApplyPricing(null); //  This creates default pricing
                        }
                    }
                }

                stopwatch.Stop();
                var cacheHitRate = productIds.Count > 0
                    ? (double)(productIds.Count - missingProductIds.Count) / productIds.Count * 100
                    : 0;

                Console.WriteLine($" PRICING APPLIED: {productDTOs.Count} products in {stopwatch.ElapsedMilliseconds}ms " +
                                $"(Cache hit rate: {cacheHitRate:F1}%, Cache service: {(cacheService != null ? "Available" : "Unavailable")})");

                return productDTOs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL PRICING ERROR: {ex.Message}");

                // FINAL FALLBACK: Apply default pricing to all products
                foreach (var productDTO in productDTOs)
                {
                    if (productDTO != null)
                    {
                        try
                        {
                            productDTO.ApplyPricing(null);
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"FALLBACK ERROR for product {productDTO.Id}: {fallbackEx.Message}");
                            // Set minimal default values
                            productDTO.Pricing = CreateMinimalDefaultPricing(productDTO);
                        }
                    }
                }
                return productDTOs;
            }
        }

        //  VERSION 2: Without Cache (Fallback method)
        public static async Task<List<ProductDTO>> ApplyPricingDirectAsync(
     this List<ProductDTO> productDTOs,
     IProductPricingService pricingService,
     int? userId = null,
     CancellationToken cancellationToken = default)
        {
            if (productDTOs == null || !productDTOs.Any())
                return productDTOs ?? new List<ProductDTO>();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var productIds = productDTOs.Select(p => p.Id).ToList();

                // DIRECT SERVICE CALL (No caching) with null safety
                var priceInfos = await pricingService.GetEffectivePricesAsync(productIds, userId, cancellationToken);
                var pricingLookup = priceInfos?.ToDictionary(p => p.ProductId, p => p) ?? new Dictionary<int, ProductPriceInfoDTO>();

                //  Apply pricing to all products with null safety
                foreach (var productDTO in productDTOs)
                {
                    if (productDTO != null)
                    {
                        if (pricingLookup.TryGetValue(productDTO.Id, out var priceInfo))
                        {
                            productDTO.ApplyPricing(priceInfo);
                        }
                        else
                        {
                            productDTO.ApplyPricing(null);
                        }
                    }
                }

                stopwatch.Stop();
                Console.WriteLine($" DIRECT PRICING: {productDTOs.Count} products in {stopwatch.ElapsedMilliseconds}ms (No Cache)");

                return productDTOs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($" PRICING ERROR: {ex.Message}");

                // FINAL FALLBACK: Apply default pricing with null safety
                foreach (var productDTO in productDTOs)
                {
                    if (productDTO != null)
                    {
                        try
                        {
                            productDTO.ApplyPricing(null);
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($" FALLBACK ERROR for product {productDTO.Id}: {fallbackEx.Message}");
                            // Set minimal default values
                            productDTO.Pricing = CreateMinimalDefaultPricing(productDTO);
                        }
                    }
                }
                return productDTOs;
            }
        }

        // Minimal default pricing creation method
        private static ProductPricingDTO CreateMinimalDefaultPricing(ProductDTO productDTO)
        {
            return new ProductPricingDTO
            {
                ProductId = productDTO.Id,
                ProductName = productDTO.Name ?? "Unknown Product",
                OriginalPrice = productDTO.MarketPrice,
                BasePrice = productDTO.BasePrice > 0 ? productDTO.BasePrice : productDTO.MarketPrice,
                EffectivePrice = productDTO.BasePrice > 0 ? productDTO.BasePrice : productDTO.MarketPrice,
                ProductDiscountAmount = 0,
                EventDiscountAmount = 0,
                TotalDiscountAmount = 0,
                TotalDiscountPercentage = 0,
                HasProductDiscount = false,
                HasEventDiscount = false,
                HasAnyDiscount = false,
                IsOnSale = false,
                FormattedOriginalPrice = $"Rs.{productDTO.MarketPrice:F2}",
                FormattedEffectivePrice = $"Rs.{(productDTO.BasePrice > 0 ? productDTO.BasePrice : productDTO.MarketPrice):F2}",
                FormattedSavings = "",
                IsPriceStable = true,
                CalculatedAt = DateTime.UtcNow
            };
        }

        // Create default pricing when no pricing service data

        private static ProductPricingDTO CreateDefaultPricing(ProductDTO productDTO)
        {
            var productDiscountAmount = productDTO.HasProductDiscount
                ? productDTO.MarketPrice - productDTO.BasePrice : 0;

            return new ProductPricingDTO
            {
                ProductId = productDTO.Id,
                ProductName = productDTO.Name,
                OriginalPrice = productDTO.MarketPrice,
                BasePrice = productDTO.BasePrice,
                EffectivePrice = productDTO.BasePrice,
                ProductDiscountAmount = productDiscountAmount,
                EventDiscountAmount = 0,
                TotalDiscountAmount = productDiscountAmount,
                TotalDiscountPercentage = productDTO.MarketPrice > 0
            ? Math.Round((productDiscountAmount / productDTO.MarketPrice) * 100, 2)
            : 0,
                HasProductDiscount = productDTO.HasProductDiscount,
                HasEventDiscount = false,
                HasAnyDiscount = productDTO.HasProductDiscount,
                IsOnSale = productDTO.HasProductDiscount,
                ActiveEventId = null,
                ActiveEventName = null,
                EventTagLine = null,
                PromotionType = null,
                EventStartDate = null,
                EventEndDate = null,
                HasActiveEvent = false,
                IsEventActive = false,
                EventTimeRemaining = null,
                IsEventExpiringSoon = false,
                FormattedOriginalPrice = $"Rs. {productDTO.MarketPrice:F2}",
                FormattedEffectivePrice = $"Rs. {productDTO.BasePrice:F2}",
                FormattedSavings = productDiscountAmount > 0 ? $"Save Rs. {productDiscountAmount:F2}" : "",
                FormattedDiscountBreakdown = productDiscountAmount > 0 ? $"Product discount: Rs. {productDiscountAmount:F2}" : "",
                EventStatus = "",
                IsPriceStable = true,
                CalculatedAt = DateTime.UtcNow
            };
        }


        public static StoreDTO ToDTO(this Store store)
        {
            return new StoreDTO
            {
                Id = store.Id,
                Name = store.Name,
                OwnerName = store.OwnerName,
                ImageUrl = store.ImageUrl,
                IsDeleted = store.IsDeleted,
                Address = store.Address?.ToDTO() // Map the Address to StoreAddressDTO
            };
        }

        public static StoreAddressDTO ToDTO(this StoreAddress storeAddress)
        {
            if (storeAddress == null) return null;
            return new StoreAddressDTO
            {
                Id = storeAddress.Id,
                StoreId = storeAddress.StoreId,
                Street = storeAddress.Street,
                City = storeAddress.City,
                Province = storeAddress.Province,
                PostalCode = storeAddress.PostalCode,
                Latitude = storeAddress.Latitude,
                Longitude = storeAddress.Longitude
            };
        }

        public static ProductStoreDTO ToDTO(this ProductStore productStore)
        {
            return new ProductStoreDTO
            {
                Id = productStore.Id,
                StoreId = productStore.StoreId,
                ProductId = productStore.ProductId,
                //Product = productStore.Product?.ToDTO(),
                //Store = productStore.Store?.ToDTO()

            };
        }

        public static CartItemDTO ToDTO(this CartItem cartItem)
        {
            if (cartItem == null) throw new ArgumentNullException(nameof(cartItem));

            return new CartItemDTO
            {
                Id = cartItem.Id,
                UserId = cartItem.UserId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                OriginalPrice = CalculateOriginalPrice(cartItem),
                ReservedPrice = cartItem.ReservedPrice,
                //ShippingCost = cartItem.ShippingCost,
                //ShippingId = cartItem.ShippingId ?? 0,

                // PROMO CODE
                AppliedPromoCodeId = cartItem.AppliedPromoCodeId ?? 0,
                PromoCodeDiscountAmount = cartItem.PromoCodeDiscountAmount ?? 0,
                AppliedPromoCode = cartItem.AppliedPromoCode,
                EventDiscountAmount = cartItem.EventDiscountAmount,
                AppliedEventId = cartItem.AppliedEventId,
                IsStockReserved = cartItem.IsStockReserved,
                ReservationToken = cartItem.ReservationToken,
                ExpiresAt = cartItem.ExpiresAt,
                CreatedAt = cartItem.CreatedAt,
                UpdatedAt = cartItem.UpdatedAt,
                IsDeleted = cartItem.IsDeleted,
                //  Category = cartItem.Product?.Category != null ? cartItem.Product.Category.ToDTO() : null,
                Product = cartItem.Product?.ToDTO(),
                //User = cartItem.User?.ToDTO(),
                //Shipping = cartItem.Shipping?.ToShippingDTO(),


            };
        }

        public static CartItemResponseDTO ToCartItemResponseDTO(this IEnumerable<CartItem> cartItems, UserDTO user, decimal shippingCost, ShippingDTO? shipping = null, string? shippingMessage = null)
        {
            var items = cartItems.Select(c => c.ToDTO()).ToList();
            var activeItems = items.Where(i => i.IsActive).ToList();

            var totalItemPrice = activeItems.Sum(i => i.TotalItemPrice);
            var totalEventDiscounts = activeItems.Sum(i => i.EventDiscountAmount ?? 0) * activeItems.Sum(i => i.Quantity);
            var totalPromoDiscounts = activeItems.Sum(i => i.PromoCodeDiscountAmount) * activeItems.Sum(i => i.Quantity);
            var totalDiscount = totalEventDiscounts + totalPromoDiscounts;
            var grandTotal = totalItemPrice + shippingCost - totalDiscount;

            return new CartItemResponseDTO
            {
                UserId = user.Id,
                User = user,
                Items = items,
                TotalItemPrice = totalItemPrice,
                TotalDiscount = totalDiscount,
                TotalEventDiscounts = totalEventDiscounts,
                TotalPromoDiscounts = totalPromoDiscounts,
                ShippingCost = shippingCost,
                GrandTotal = grandTotal,
                Shipping = shipping,
                ShippingMessage = shippingMessage ?? (shippingCost == 0 ? " Free shipping applied!" : $"📦 Shipping: Rs. {shippingCost:F2}"),
                HasFreeShipping = shippingCost == 0
            };
        }

        public static IEnumerable<CartItemDTO> ToDTO(this IEnumerable<CartItem> cartItems)
        {
            if (cartItems == null) return new List<CartItemDTO>();
            return cartItems.Select(cartItem => cartItem.ToDTO());
        }

        // Add new method for cart summary mapping
        public static CartSummaryDTO ToSummaryDTO(this IEnumerable<CartItem> cartItems, int userId)
        {
            if (cartItems == null) return new CartSummaryDTO { UserId = userId };

            var itemList = cartItems.ToList();
            var activeItems = itemList.Where(c => !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow).ToList();
            var expiredItems = itemList.Where(c => c.ExpiresAt <= DateTime.UtcNow).ToList();

            var validationErrors = new List<string>();
            if (expiredItems.Any())
            {
                validationErrors.Add($"{expiredItems.Count} item(s) have expired");
            }

            var outOfStockItems = activeItems.Where(c => c.Product?.StockQuantity <= 0).ToList();
            if (outOfStockItems.Any())
            {
                validationErrors.Add($"{outOfStockItems.Count} item(s) are out of stock");
            }

            // Calculate subtotal and shipping
            var subtotal = activeItems.Sum(c => c.ReservedPrice * c.Quantity);
            var totalShipping = activeItems.Sum(c => c.ShippingCost);
            var totalEventDiscounts = activeItems.Sum(c => c.EventDiscountAmount ?? 0);
            var totalPromoDiscounts = activeItems.Sum(c => c.PromoCodeDiscountAmount ?? 0);


            return new CartSummaryDTO
            {
                UserId = userId,
                TotalItems = activeItems.Count,
                TotalQuantity = activeItems.Sum(c => c.Quantity),
                SubTotal = subtotal,

                //  SHIPPING BREAKDOWN
                TotalShipping = totalShipping,
                TotalDiscount = totalEventDiscounts + totalPromoDiscounts,
                EventDiscounts = totalEventDiscounts,
                PromoCodeDiscounts = totalPromoDiscounts,

                EstimatedTotal = subtotal + totalShipping - (totalEventDiscounts + totalPromoDiscounts),

                CanCheckout = activeItems.Any() && !expiredItems.Any() && !outOfStockItems.Any(),
                HasExpiredItems = expiredItems.Any(),
                HasOutOfStockItems = outOfStockItems.Any(),
                ExpiredItemsCount = expiredItems.Count,
                EarliestExpiration = activeItems.Any() ? activeItems.Min(c => c.ExpiresAt) : null,
                ValidationErrors = validationErrors,
                Items = activeItems.Select(c => c.ToDTO()).ToList(),

                //  SHIPPING INFO
                HasFreeShipping = totalShipping == 0 && activeItems.Any(),
                ShippingMessage = totalShipping == 0 ? " Free Shipping Applied!" : $" Shipping: Rs.{totalShipping:F2}"
            };
        }

        // Calculate original price from available CartItem data
        private static decimal? CalculateOriginalPrice(CartItem cartItem)
        {
            var eventDiscount = cartItem.EventDiscountAmount ?? 0;
            var promoDiscount = cartItem.PromoCodeDiscountAmount ?? 0;
            var regularDiscount = cartItem.RegularDiscountAmount;

            var estimatedOriginal = cartItem.ReservedPrice + regularDiscount + eventDiscount + promoDiscount;
            return estimatedOriginal > 0 ? estimatedOriginal : cartItem.ReservedPrice;


        }
        public static CartItemDTO ToEnhancedDTO(this CartItem cartItem, ProductPriceInfoDTO? currentPricing = null)
        {
            if (cartItem == null) throw new ArgumentNullException(nameof(cartItem));

            // Convert to basic DTO first
            var dto = cartItem.ToDTO();

            // If current pricing is available, we can show price comparison
            if (currentPricing != null)
            {
                // Calculate if price has changed since cart creation
                var currentEffectivePrice = currentPricing.EffectivePrice;
                var priceDifference = currentEffectivePrice - cartItem.ReservedPrice;

                // You could add properties to show price changes, but since you don't want to modify DTO,
                // this information would be handled at the service level
            }

            return dto;
        }






        public static OrderDTO ToDTO(this Order order)
        {
            return new OrderDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                IsDeleted = order.IsDeleted,
                UserDTO = order.User?.ToDTO(),
                Items = order.Items != null
                ? order.Items.Select(i => i.ToDTO()).ToList()
                : new List<OrderItemDTO>()
            };
        }

        public static OrderItemDTO ToDTO(this OrderItem orderItem)
        {
            return new OrderItemDTO
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.ProductName,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                TotalPrice = orderItem.Quantity * orderItem.UnitPrice,
                /* Product = orderItem.Product?.ToDTO(), // Map Product to ProductDTO
                 Order = null // Avoid circular reference*/
            };
        }

        public static NearbyProductDto ToNearbyProductDto(this ProductStore productStore, double distance)
        {
            return new NearbyProductDto
            {
                ProductId = productStore.ProductId,
                Name = productStore.Product?.Name ?? "Unknown Product",
                ImageUrl = productStore.Product?.Images.FirstOrDefault()?.ImageUrl ?? string.Empty,
                StoreCity = productStore.Store?.Address?.City ?? string.Empty,
                StoreName = productStore.Store?.Name ?? "Unknown Store",
                MarketPrice = productStore.Product?.MarketPrice ?? 0,
                CostPrice = productStore.Product?.CostPrice ?? 0,
                DiscountPrice = productStore.Product?.DiscountPrice,
                StockQuantity = productStore.Product?.StockQuantity ?? 0,
                StoreId = productStore.StoreId,
                StoreAddress = $"{productStore.Store?.Address?.Street},{productStore.Store?.Address?.City}",
                Distance = distance,
                HasDiscount = productStore.Product?.DiscountPrice.HasValue == true,

                // initialize dynaimc pricing
                CurrentPrice = productStore.Product?.MarketPrice ?? 0,
                EffectivePrice = productStore.Product?.MarketPrice ?? 0,
                HasActiveEvent = false,
                DiscountAmount = 0,
                DiscountPercentage = 0,
                ActiveEventName = null,
                ProductDTO = productStore.Product?.ToDTO()
            };
        }

        public static EventRule ToEntity(this EventRuleDTO dto, int bannerEventId)
        {
            return new EventRule
            {
                Id = dto.Id,
                BannerEventId = bannerEventId,
                Type = dto.Type,
                TargetValue = dto.TargetValue,
                Conditions = dto.Conditions,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinOrderValue = dto.MinOrderValue,
                Priority = dto.Priority,
            };
        }
        public static EventRule ToEntity(this AddEventRuleDTO dto, int bannerEventId)
        {
            return new EventRule
            {
                BannerEventId = bannerEventId,
                Type = dto.Type,
                TargetValue = dto.TargetValue,
                Conditions = dto.Conditions,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinOrderValue = dto.MinOrderValue,
                Priority = dto.Priority,
            };
        }
        // ENHANCED: Updated BannerEventSpecial to DTO mapping WITH Nepal timezone support
        // Replace the ToDTO method (around line 187) with this safer version:

        public static BannerEventSpecialDTO ToDTO(this BannerEventSpecial bannerEventSpecial, INepalTimeZoneService? nepalTimeService = null)
        {
            if (bannerEventSpecial == null) throw new ArgumentNullException(nameof(bannerEventSpecial));

            var currentUtcTime = DateTime.UtcNow;

            // SAFE: Handle timezone service gracefully
            DateTime startDateNepal;
            DateTime endDateNepal;
            DateTime currentNepalTime;
            string timeStatus;
            bool isCurrentlyActive;
            TimeZoneDisplayInfo timeZoneInfo;

            if (nepalTimeService != null)
            {
                try
                {
                    // Convert UTC dates to Nepal time for display
                    startDateNepal = nepalTimeService.ConvertFromUtcToNepal(bannerEventSpecial.StartDate);
                    endDateNepal = nepalTimeService.ConvertFromUtcToNepal(bannerEventSpecial.EndDate);
                    currentNepalTime = nepalTimeService.GetNepalCurrentTime();
                    timeStatus = nepalTimeService.GetEventTimeStatus(bannerEventSpecial.StartDate, bannerEventSpecial.EndDate);
                    isCurrentlyActive = nepalTimeService.IsEventActiveNow(bannerEventSpecial.StartDate, bannerEventSpecial.EndDate);

                    timeZoneInfo = new TimeZoneDisplayInfo
                    {
                        DisplayTimeZone = "Nepal Standard Time",
                        OffsetString = "UTC+05:45",
                        CurrentNepalTime = currentNepalTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        CurrentUtcTime = currentUtcTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        TimeZoneAbbreviation = "NPT",
                        IsDaylightSavingTime = false
                    };
                }
                catch (Exception)
                {
                    // FALLBACK: Use UTC if timezone service fails
                    startDateNepal = bannerEventSpecial.StartDate;
                    endDateNepal = bannerEventSpecial.EndDate;
                    currentNepalTime = currentUtcTime;
                    timeStatus = "Timezone conversion failed - using UTC";
                    isCurrentlyActive = bannerEventSpecial.IsActive &&
                                      bannerEventSpecial.StartDate <= currentUtcTime &&
                                      bannerEventSpecial.EndDate >= currentUtcTime;

                    timeZoneInfo = new TimeZoneDisplayInfo
                    {
                        DisplayTimeZone = "UTC (Fallback)",
                        OffsetString = "UTC+00:00",
                        CurrentNepalTime = "Service Error",
                        CurrentUtcTime = currentUtcTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        TimeZoneAbbreviation = "UTC",
                        IsDaylightSavingTime = false
                    };
                }
            }
            else
            {
                // FALLBACK: Use UTC when no timezone service
                startDateNepal = bannerEventSpecial.StartDate;
                endDateNepal = bannerEventSpecial.EndDate;
                currentNepalTime = currentUtcTime;
                timeStatus = "Nepal timezone service not available - using UTC";
                isCurrentlyActive = bannerEventSpecial.IsActive &&
                                  bannerEventSpecial.StartDate <= currentUtcTime &&
                                  bannerEventSpecial.EndDate >= currentUtcTime;

                timeZoneInfo = new TimeZoneDisplayInfo
                {
                    DisplayTimeZone = "UTC (No Service)",
                    OffsetString = "UTC+00:00",
                    CurrentNepalTime = "Service Not Available",
                    CurrentUtcTime = currentUtcTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    TimeZoneAbbreviation = "UTC",
                    IsDaylightSavingTime = false
                };
            }

            var dto = new BannerEventSpecialDTO
            {
                // SYSTEM FIELDS
                Id = bannerEventSpecial.Id,
                CreatedAt = bannerEventSpecial.CreatedAt,
                UpdatedAt = bannerEventSpecial.UpdatedAt ?? bannerEventSpecial.CreatedAt,
                CurrentUsageCount = bannerEventSpecial.CurrentUsageCount,
                IsDeleted = bannerEventSpecial.IsDeleted,

                // BASIC EVENT INFO
                Name = bannerEventSpecial.Name ?? string.Empty,
                Description = bannerEventSpecial.Description ?? string.Empty,
                TagLine = bannerEventSpecial.TagLine,
                EventType = bannerEventSpecial.EventType,
                PromotionType = bannerEventSpecial.PromotionType,
                DiscountValue = bannerEventSpecial.DiscountValue,
                MaxDiscountAmount = bannerEventSpecial.MaxDiscountAmount,
                MinOrderValue = bannerEventSpecial.MinOrderValue,

                //  UTC DATES (Database storage - always UTC)
                StartDate = DateTime.SpecifyKind(bannerEventSpecial.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(bannerEventSpecial.EndDate, DateTimeKind.Utc),

                //  NEPAL DATES (Display - converted for UI)
                StartDateNepal = startDateNepal,
                EndDateNepal = endDateNepal,

                // CONFIGURATION
                ActiveTimeSlot = bannerEventSpecial.ActiveTimeSlot,
                MaxUsageCount = bannerEventSpecial.MaxUsageCount,
                MaxUsagePerUser = bannerEventSpecial.MaxUsagePerUser,
                Priority = bannerEventSpecial.Priority,
                IsActive = bannerEventSpecial.IsActive,
                Status = bannerEventSpecial.Status,

                // RELATED DATA
                ProductIds = bannerEventSpecial.EventProducts?.Select(ep => ep.ProductId).ToList() ?? new List<int>(),
                TotalProductsCount = bannerEventSpecial.EventProducts?.Count ?? 0,
                TotalRulesCount = bannerEventSpecial.Rules?.Count ?? 0,

                // COMPUTED PROPERTIES (Using safe logic)
                IsCurrentlyActive = isCurrentlyActive,
                IsExpired = endDateNepal < currentNepalTime,
                TimeStatus = timeStatus,

                //  SAFE TIMEZONE INFO
                TimeZoneInfo = timeZoneInfo,

                // NESTED ENTITIES
                Images = bannerEventSpecial.Images?.Select(i => i.ToDTO()).ToList() ?? new List<BannerImageDTO>(),
                Rules = bannerEventSpecial.Rules?.Select(r => r.ToDTO()).ToList() ?? new List<EventRuleDTO>(),
                EventProducts = bannerEventSpecial.EventProducts?.Select(ep => ep.ToDTO()).ToList() ?? new List<EventProductDTO>()
            };

            //  CALCULATE DAYS REMAINING SAFELY
            dto.DaysRemaining = dto.IsExpired ? 0 : (int)Math.Ceiling((endDateNepal - currentNepalTime).TotalDays);

            return dto;
        }

        /// <summary>
        /// FALLBACK METHOD (without timezone service)
        /// </summary>
        public static BannerEventSpecialDTO ToDTO(this BannerEventSpecial entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var dto = new BannerEventSpecialDTO
            {
                Id = entity.Id,
                Name = entity.Name ?? string.Empty,
                Description = entity.Description ?? string.Empty,
                TagLine = entity.TagLine,
                EventType = entity.EventType,
                PromotionType = entity.PromotionType,
                DiscountValue = entity.DiscountValue,
                MaxDiscountAmount = entity.MaxDiscountAmount,
                MinOrderValue = entity.MinOrderValue,

                // UTC DATES
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,

                //  FALLBACK: Assume UTC = Nepal (incorrect but safe)
                StartDateNepal = entity.StartDate,
                EndDateNepal = entity.EndDate,

                ActiveTimeSlot = entity.ActiveTimeSlot,
                MaxUsageCount = entity.MaxUsageCount,
                CurrentUsageCount = entity.CurrentUsageCount,
                MaxUsagePerUser = entity.MaxUsagePerUser,
                Priority = entity.Priority,
                IsActive = entity.IsActive,
                IsDeleted = entity.IsDeleted,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt ?? entity.CreatedAt,

                // FALLBACK TIME LOGIC (UTC-based)
                TimeStatus = "Timezone service not available",
                IsCurrentlyActive = entity.IsActive && entity.StartDate <= DateTime.UtcNow && entity.EndDate >= DateTime.UtcNow,
                IsExpired = entity.EndDate < DateTime.UtcNow,
                DaysRemaining = entity.EndDate < DateTime.UtcNow ? 0 : (int)Math.Ceiling((entity.EndDate - DateTime.UtcNow).TotalDays),

                // RELATED DATA
                ProductIds = entity.EventProducts?.Select(ep => ep.ProductId).ToList() ?? new List<int>(),
                TotalProductsCount = entity.EventProducts?.Count ?? 0,
                TotalRulesCount = entity.Rules?.Count ?? 0,

                // FALLBACK TIMEZONE INFO
                TimeZoneInfo = new TimeZoneDisplayInfo
                {
                    DisplayTimeZone = "Service Unavailable",
                    OffsetString = "N/A",
                    CurrentNepalTime = "Service Unavailable",
                    CurrentUtcTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    TimeZoneAbbreviation = "UTC",
                    IsDaylightSavingTime = false
                },

                // NESTED ENTITIES
                Images = entity.Images?.Select(i => i.ToDTO()).ToList() ?? new List<BannerImageDTO>(),
                Rules = entity.Rules?.Select(r => r.ToDTO()).ToList() ?? new List<EventRuleDTO>(),
                EventProducts = entity.EventProducts?.Select(ep => ep.ToDTO()).ToList() ?? new List<EventProductDTO>()
            };

            return dto;
        }

        //   ToEntity mapping with proper parameters
        public static BannerEventSpecial ToEntity(this AddBannerEventSpecialDTO dto, DateTime startDateUtc, DateTime endDateUtc)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new BannerEventSpecial
            {
                Name = dto.Name?.Trim() ?? throw new ArgumentException("Name is required"),
                Description = dto.Description?.Trim() ?? throw new ArgumentException("Description is required"),
                TagLine = dto.TagLine?.Trim(),
                EventType = dto.EventType,
                PromotionType = dto.PromotionType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderValue = dto.MinOrderValue,

                // USE PRE-CONVERTED UTC DATES FROM SERVICE
                StartDate = startDateUtc,
                EndDate = endDateUtc,

                // PARSE TIMESPAN FROM STRING
                ActiveTimeSlot = dto.ActiveTimeSlotParsed,

                MaxUsageCount = dto.MaxUsageCount ?? int.MaxValue,
                CurrentUsageCount = 0,
                MaxUsagePerUser = dto.MaxUsagePerUser ?? int.MaxValue,
                Priority = dto.Priority ?? 1,
                IsActive = false,
                IsDeleted = false,
                Status = EventStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // ENHANCED: EventRule to DTO mapping
        public static EventRuleDTO ToDTO(this EventRule eventRule)
        {
            return new EventRuleDTO
            {
                Id = eventRule.Id,
                Type = eventRule.Type,
                TargetValue = eventRule.TargetValue,
                Conditions = eventRule.Conditions,
                DiscountType = eventRule.DiscountType,
                DiscountValue = eventRule.DiscountValue,
                MaxDiscount = eventRule.MaxDiscount,
                MinOrderValue = eventRule.MinOrderValue,
                Priority = eventRule.Priority
            };
        }

        // ENHANCED: EventProduct to DTO mapping
        public static EventProductDTO ToDTO(this EventProduct eventProduct)
        {
            return new EventProductDTO
            {
                Id = eventProduct.Id,
                BannerEventId = eventProduct.BannerEventId,
                ProductId = eventProduct.ProductId,
                ProductName = eventProduct.Product?.Name ?? "Unknown Product",
                SpecificDiscount = eventProduct.SpecificDiscount ?? 0,
                ProductImageUrl = eventProduct.Product?.Images?.FirstOrDefault()?.ImageUrl,
                ProductMarketPrice = eventProduct.Product?.MarketPrice ?? 0,
                CalculatedDiscountPrice = eventProduct.SpecificDiscount.HasValue
                    ? (eventProduct.Product?.MarketPrice ?? 0) - eventProduct.SpecificDiscount.Value
                    : eventProduct.Product?.MarketPrice ?? 0
            };
        }

        /// <summary>
        /// Convert BannerEventSpecialDTO back to Entity for rule engine processing
        /// </summary>
        public static BannerEventSpecial ToEntity(this BannerEventSpecialDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new BannerEventSpecial
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                TagLine = dto.TagLine,
                EventType = dto.EventType,
                PromotionType = dto.PromotionType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderValue = dto.MinOrderValue,

                // USE UTC DATES (stored dates)
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,

                ActiveTimeSlot = dto.ActiveTimeSlot,
                MaxUsageCount = dto.MaxUsageCount,
                CurrentUsageCount = dto.CurrentUsageCount,
                MaxUsagePerUser = dto.MaxUsagePerUser,
                Priority = dto.Priority,
                IsActive = dto.IsActive,
                IsDeleted = dto.IsDeleted,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,

                //  MAP RELATED ENTITIES
                Rules = dto.Rules?.Select(r => r.ToEntity()).ToList() ?? new List<EventRule>(),
                EventProducts = dto.EventProducts?.Select(ep => ep.ToEntity()).ToList() ?? new List<EventProduct>(),
                Images = dto.Images?.Select(i => i.ToEntity()).ToList() ?? new List<BannerImage>()
            };
        }

        /// <summary>
        /// Convert EventRuleDTO back to Entity
        /// </summary>
        public static EventRule ToEntity(this EventRuleDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new EventRule
            {
                Id = dto.Id,
                BannerEventId = 0, // Will be set by parent
                Type = dto.Type,
                TargetValue = dto.TargetValue,
                Conditions = dto.Conditions,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinOrderValue = dto.MinOrderValue,
                Priority = dto.Priority
            };
        }

        /// <summary>
        /// Convert EventProductDTO back to Entity
        /// </summary>
        public static EventProduct ToEntity(this EventProductDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new EventProduct
            {
                Id = dto.Id,
                BannerEventId = dto.BannerEventId,
                ProductId = dto.ProductId,
                SpecificDiscount = dto.SpecificDiscount
                // Note: Product navigation property will be loaded separately if needed
            };
        }

        /// <summary>
        /// Convert BannerImageDTO back to Entity
        /// </summary>
        public static BannerImage ToEntity(this BannerImageDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new BannerImage
            {
                Id = dto.Id,
                BannerId = dto.BannerEventId,
                ImageUrl = dto.ImageUrl
            };
        }

        /// <summary>
        ///  Convert CartValidationRequestDTO to EvaluationContextDTO
        /// </summary>
        public static EvaluationContextDTO ToEvaluationContext(this CartValidationRequestDTO request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new EvaluationContextDTO
            {
                CartItems = request.CartItems ?? new List<CartItem>(),
                User = request.User,
                PaymentMethod = request.PaymentMethod,
                OrderTotal = request.CartItems?.Sum(c => c.ReservedPrice * c.Quantity),
                EvaluationTime = DateTime.UtcNow
            };
        }

        /// <summary>
        ///  ENHANCED: Convert RuleTestRequestDTO to EvaluationContextDTO
        /// </summary>
        public static EvaluationContextDTO ToEvaluationContext(this RuleTestRequestDTO request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new EvaluationContextDTO
            {
                CartItems = request.TestCartItems ?? new List<CartItem>(),
                User = request.TestUser,
                PaymentMethod = request.TestPaymentMethod,
                OrderTotal = request.TestOrderTotal ?? request.TestCartItems?.Sum(c => c.ReservedPrice * c.Quantity),
                EvaluationTime = DateTime.UtcNow
            };
        }

        public static BannerImageDTO ToDTO(this BannerImage bannerImage)
        {
            return new BannerImageDTO
            {
                Id = bannerImage.Id,
                ImageUrl = bannerImage.ImageUrl,
                BannerEventId = bannerImage.BannerId
            };
        }
        public static PaymentMethodDTO ToDTO(this PaymentMethod paymentMethod)
        {
            return new PaymentMethodDTO
            {
                Id = paymentMethod.Id,
                ProviderName = paymentMethod.ProviderName,
                Type = paymentMethod.Type,
                Logo = paymentMethod.Logo ?? "",
                RequiresRedirect = paymentMethod.RequiresRedirect,
                SupportedCurrencies = paymentMethod.SupportedCurrencies,
                IsAvailable = paymentMethod.IsActive,
                PaymentRequests = paymentMethod.PaymentRequests != null
            ? paymentMethod.PaymentRequests.Select(pr => pr.ToDTO()).ToList()
            : new List<PaymentRequestDTO>()
            };
        }

        public static PaymentMethodResponseDTO ToDTO(this PaymentMethod paymentMethod, CancellationToken cancellation = default)
        {
            return new PaymentMethodResponseDTO
            {
                Id = paymentMethod.Id,
                ProviderName = paymentMethod.ProviderName,
                Type = paymentMethod.Type.ToString(),
                Logo = paymentMethod.Logo,
                RequiresRedirect = paymentMethod.RequiresRedirect,
                SupportedCurrencies = paymentMethod.SupportedCurrencies!.Select(sc => sc.ToString()).ToArray(),
                IsAvailable = paymentMethod.IsActive,
                PaymentRequests = paymentMethod.PaymentRequests != null
            ? paymentMethod.PaymentRequests.Select(pr => pr.ToDTO()).ToList()
            : new List<PaymentRequestDTO>()
            };
        }

        public static PaymentRequestDTO ToDTO(this PaymentRequest paymentRequest)
        {
            return new PaymentRequestDTO
            {
                Id = paymentRequest.Id,
                UserId = paymentRequest.UserId,
                OrderId = paymentRequest.OrderId,
                PaymentMethodId = paymentRequest.PaymentMethodId,
                PaymentAmount = paymentRequest.PaymentAmount,
                Currency = paymentRequest.Currency ?? "NPR",
                Description = paymentRequest.Description!,
                PaymentStatus = paymentRequest.PaymentStatus,
                PaymentUrl = paymentRequest.PaymentUrl,
                KhaltiPidx = paymentRequest.KhaltiPidx,
                EsewaTransactionId = paymentRequest.EsewaTransactionId,
                CreatedAt = paymentRequest.CreatedAt,
                UpdatedAt = paymentRequest.UpdatedAt,
                IsDeleted = paymentRequest.IsDeleted,
                UserName = paymentRequest.User?.Name,
                PaymentMethodName = paymentRequest.PaymentMethod?.ProviderName,
                OrderTotal = paymentRequest.Order?.TotalAmount,

                // Computed properties
                ExpiresAt = paymentRequest.CreatedAt.AddMinutes(15), // Default 15 min expiry
                RequiresRedirect = paymentRequest.PaymentMethodId == 1 || paymentRequest.PaymentMethodId == 2, // eSewa or Khalti
                Instructions = GetPaymentInstructions(paymentRequest.PaymentMethodId, paymentRequest.PaymentStatus),

                Metadata = new Dictionary<string, string>
                {
                    ["provider"] = GetProviderName(paymentRequest.PaymentMethodId),
                    ["transactionId"] = paymentRequest.EsewaTransactionId ?? paymentRequest.KhaltiPidx ?? "",
                    ["hasPaymentUrl"] = (!string.IsNullOrEmpty(paymentRequest.PaymentUrl)).ToString()
                }

            };
        }
        // Helper method for payment insructions
        private static string GetPaymentInstructions(int paymentMethodId, string status)
        {
            return paymentMethodId switch
            {
                1 => status == "Pending" ? "Click the payment link to proceed with eSewa payment" :
                     status == "Initiated" ? "Complete your payment on eSewa" :
                     $"Payment {status.ToLower()}",
                2 => status == "Pending" ? "Click the payment link to proceed with Khalti payment" :
                     status == "Initiated" ? "Complete your payment on Khalti" :
                     $"Payment {status.ToLower()}",
                _ => $"Payment {status.ToLower()}"
            };
        }

        private static string GetProviderName(int paymentMethodId)
        {
            return paymentMethodId switch
            {
                1 => "eSewa",
                2 => "Khalti",
                3 => "Cash on Delivery",
                _ => "Unknown"
            };
        }
        public static BillingDTO ToDTO(this Billing billing)
        {
            return new BillingDTO
            {
                Id = billing.Id,
                UserId = billing.UserId,
                PaymentId = billing.PaymentId,
                OrderId = billing.OrderId,
                CompanyInfoId = billing.CompanyInfoId,
                BillingDate = billing.BillingDate,
                IsDeleted = billing.IsDeleted,
                User = billing.User?.ToDTO(),
                PaymentRequest = billing.PaymentRequest?.ToDTO(),
                Order = billing.Order?.ToDTO(),
                CompanyInfo = billing.CompanyInfo?.ToDTO(),
                Items = billing.Items != null
                ? billing.Items.Select(i => i.ToDTO()).ToList()
                : new List<BillingItemDTO>()
            };
        }

        public static BillingItemDTO ToDTO(this BillingItem item)
        {
            return new BillingItemDTO
            {
                Id = item.Id,
                BillingId = item.BillingId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                DiscountAmount = item.DiscountAmount,
                TaxAmount = item.TaxAmount,
                Notes = item.Notes,
                IsDeleted = item.IsDeleted,

            };

        }

        public static WishlistDTO ToDTO(this Wishlist whishlist)
        {
            return new WishlistDTO
            {
                Id = whishlist.Id,
                UserId = whishlist.UserId,
                ProductId = whishlist.ProductId,
                CreatedAt = whishlist.CreatedAt,
                UpdatedAt = whishlist.UpdatedAt,
                IsDeleted = whishlist.IsDeleted,
                UserDto = whishlist.User.ToDTO(),
                ProductDto = whishlist.Product.ToDTO(),
            };
        }

        public static ShippingDTO ToShippingDTO(this Shipping config)
        {
            return new ShippingDTO
            {
                Id = config.Id,
                ConfigurationName = config.Name,
                IsActive = config.IsActive,
                IsDefault = config.IsDefault,
                LowOrderThreshold = config.LowOrderThreshold,
                LowOrderShippingCost = config.LowOrderShippingCost,
                HighOrderShippingCost = config.HighOrderShippingCost,
                FreeShippingThreshold = config.FreeShippingThreshold,
                EstimatedDeliveryDays = config.EstimatedDeliveryDays,
                MaxDeliveryDistanceKm = config.MaxDeliveryDistanceKm,
                EnableFreeShippingEvents = config.EnableFreeShippingEvents,
                IsFreeShippingActive = config.IsFreeShippingActive,
                FreeShippingStartDate = config.FreeShippingStartDate,
                FreeShippingEndDate = config.FreeShippingEndDate,
                FreeShippingDescription = config.FreeShippingDescription,
                WeekendSurcharge = config.WeekendSurcharge,
                HolidaySurcharge = config.HolidaySurcharge,
                RushDeliverySurcharge = config.RushDeliverySurcharge,
                CustomerMessage = config.CustomerMessage,
                AdminNotes = config.AdminNotes,
                CreatedByUserName = config.CreatedByUser?.Name ?? "System",
                CreatedAt = config.CreatedAt,
                LastModifiedByUserName = config.LastModifiedByUser?.Name,
                LastModifiedAt = config.UpdatedAt
            };
        }

        public static PromoCodeDTO ToPromoCodeDTO(this PromoCode promoCode, INepalTimeZoneService? nepalTimeService = null)
        {
            if (promoCode == null) throw new ArgumentNullException(nameof(promoCode));

            var currentUtcTime = DateTime.UtcNow;

             var startUtc = DateTime.SpecifyKind(promoCode.StartDate, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(promoCode.EndDate, DateTimeKind.Utc);


            // SAFE: Handle timezone service gracefully
            DateTime startDateNepal, endDateNepal, currentNepalTime;            
            string timeStatus, formattedStartDate, formattedEndDate;
            bool isCurrentlyActive, isExpired;           
            int daysRemaining;
            


            if (nepalTimeService != null)
            {
                try
                {
                    // Use extension methods instead of direct interface calls
                    startDateNepal = nepalTimeService.ConvertFromUtcToNepal(startUtc);
                    endDateNepal = nepalTimeService.ConvertFromUtcToNepal(endUtc);
                    currentNepalTime = nepalTimeService.GetNepalCurrentTime();

                    timeStatus = promoCode.GetStatusInNepalTime(nepalTimeService); // Extension method
                    isCurrentlyActive = promoCode.IsCurrentlyActive(nepalTimeService); // Extension method
                    isExpired = promoCode.IsExpiredNow(nepalTimeService); // Extension method
                    daysRemaining = promoCode.GetDaysRemaining(nepalTimeService); // Extension method

                    formattedStartDate = startDateNepal.ToString("yyyy-MM-dd HH:mm");
                    formattedEndDate = endDateNepal.ToString("yyyy-MM-dd HH:mm");

                }
                catch (Exception ex)
                {
                    // FALLBACK: Use pure domain methods
                    startDateNepal = startUtc;
                    endDateNepal = endUtc;
                    currentNepalTime = currentUtcTime;
                    isCurrentlyActive = promoCode.IsValidAtTime(currentUtcTime); // Pure domain method
                    isExpired = promoCode.IsExpiredAtTime(currentUtcTime); // Pure domain method

                    var timeUntilEnd = promoCode.GetTimeUntilEnd(currentUtcTime);
                    daysRemaining = timeUntilEnd > TimeSpan.Zero ? (int)Math.Ceiling(timeUntilEnd.TotalDays) : 0;
                    timeStatus = $"Timezone error: {ex.Message}";

                    formattedStartDate = TimeParsingHelper.FormatForNepalDisplay(startDateNepal);
                    formattedEndDate = TimeParsingHelper.FormatForNepalDisplay(endDateNepal);
                }
            }
            else
            {
                // FALLBACK: Use pure domain methods when no timezone service
                startDateNepal = promoCode.StartDate;
                endDateNepal = promoCode.EndDate;
                currentNepalTime = currentUtcTime;
                isCurrentlyActive = promoCode.IsValidAtTime(currentUtcTime); // Pure domain method
                isExpired = promoCode.IsExpiredAtTime(currentUtcTime); // Pure domain method

                var timeUntilEnd = promoCode.GetTimeUntilEnd(currentUtcTime);
                daysRemaining = timeUntilEnd > TimeSpan.Zero ? (int)Math.Ceiling(timeUntilEnd.TotalDays) : 0;
                timeStatus = "Timezone service not available";

                formattedStartDate = TimeParsingHelper.FormatForNepalDisplay(startDateNepal);
                formattedEndDate = TimeParsingHelper.FormatForNepalDisplay(endDateNepal);
            }

            var dto = new PromoCodeDTO
            {
                // BASIC FIELDS
                Id = promoCode.Id,
                Code = promoCode.Code,
                Name = promoCode.Name,
                Description = promoCode.Description!,
                Type = promoCode.Type,
                DiscountValue = promoCode.DiscountValue,
                MaxDiscountAmount = promoCode.MaxDiscountAmount,
                MinOrderAmount = promoCode.MinOrderAmount,
                MaxTotalUsage = promoCode.MaxTotalUsage,
                MaxUsagePerUser = promoCode.MaxUsagePerUser,
                CurrentUsageCount = promoCode.CurrentUsageCount,

                // UTC DATES (Database storage - always UTC)
                StartDate = DateTime.SpecifyKind(promoCode.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(promoCode.EndDate, DateTimeKind.Utc),

                // NEPAL DATES (Display - converted for UI)
                StartDateNepal = startDateNepal,
                EndDateNepal = endDateNepal,




                // STATUS FIELDS
                IsActive = promoCode.IsActive,
                IsDeleted = promoCode.IsDeleted,
                ApplyToShipping = promoCode.ApplyToShipping,
                StackableWithEvents = promoCode.StackableWithEvents,
                CustomerTier = promoCode.CustomerTier,
                AdminNotes = promoCode.AdminNotes,

                // AUDIT FIELDS
                CreatedByUserName = promoCode.CreatedByUser?.Name ?? "System",
                CreatedAt = promoCode.CreatedAt,
                LastModifiedByUserName = promoCode.LastModifiedByUser?.Name,
                LastModifiedAt = promoCode.UpdatedAt,

                // COMPUTED PROPERTIES (Using safe logic)
                IsCurrentlyActive = isCurrentlyActive,
                IsExpired = isExpired,
                TimeStatus = timeStatus,

                // SAFE TIMEZONE INFO

            };

            // CALCULATE DAYS REMAINING SAFELY
            if (nepalTimeService != null)
            {
                dto.DaysRemaining = promoCode.GetDaysRemaining(nepalTimeService); // Extension method
            }
            else
            {
                var timeUntilEnd = promoCode.GetTimeUntilEnd(currentUtcTime); // Pure domain method
                dto.DaysRemaining = timeUntilEnd > TimeSpan.Zero ? (int)Math.Ceiling(timeUntilEnd.TotalDays) : 0;
            }

            return dto;
        }

        /// <summary>
        /// FALLBACK METHOD (without timezone service)
        /// </summary>
        public static PromoCodeDTO ToPromoCodeDTO(this PromoCode promoCode)
        {
            return promoCode.ToPromoCodeDTO(null);
        }

        /// <summary>
        /// Convert AddPromoCodeDTO to Entity with proper timezone handling
        /// </summary>
        public static PromoCode ToEntity(this AddPromoCodeDTO dto, DateTime startDateUtc, DateTime endDateUtc, int createdByUserId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            return new PromoCode
            {
                Code = dto.Code?.Trim() ?? throw new ArgumentException("Code is required"),
                Name = dto.Name?.Trim() ?? throw new ArgumentException("Name is required"),
                Description = dto.Description?.Trim(),
                Type = dto.Type,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderAmount = dto.MinOrderAmount,
                MaxTotalUsage = dto.MaxTotalUsage,
                MaxUsagePerUser = dto.MaxUsagePerUser,

                // USE PRE-CONVERTED UTC DATES FROM SERVICE
                StartDate = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc),

                ApplyToShipping = dto.ApplyToShipping,
                StackableWithEvents = dto.StackableWithEvents,
                CustomerTier = dto.CustomerTier,


                // AUDIT FIELDS
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = dto.IsActive,
                IsDeleted = false,
                CurrentUsageCount = 0
            };
        }

        public static PromoCodeUsageDTO ToPromoCodeUsageDTO(this PromoCodeUsage usage, INepalTimeZoneService? nepalTimeService = null)
        {
            var usedAtNepal = nepalTimeService != null ?
                usage.UsedAt.ToNepalTime(nepalTimeService) :
                usage.UsedAt;

            return new PromoCodeUsageDTO
            {
                Id = usage.Id,
                Code = usage.PromoCode?.Code ?? "",
                UserEmail = usage.UserEmail ?? "",
                OrderId = usage.OrderId,
                OrderTotal = usage.OrderTotal,
                DiscountApplied = usage.DiscountAmount,
                UsedAt = usage.UsedAt, // UTC time
                //UsedAtNepal = usedAtNepal, // Nepal time for display
                //FormattedUsedAt = nepalTimeService != null ? 
                /// usedAtNepal.ToUserFriendlyNepalTime(nepalTimeService) :
                // TimeParsingHelper.FormatForNepalDisplay(usedAtNepal),
                PaymentMethod = usage.PaymentMethod
            };
        }

        public static ServiceAreaDTO ToDTO(this ServiceArea serviceArea)
        {
            return new ServiceAreaDTO {
                Id = serviceArea.Id,
                CityName = serviceArea.CityName,
                Province = serviceArea.Province,
                Country = serviceArea.Country,
                CenterLatitude = serviceArea.CenterLatitude,
                CenterLongitude = serviceArea.CenterLongitude,
                RadiusKm = serviceArea.RadiusKm,
                IsActive = serviceArea.IsActive,
                IsComingSoon = serviceArea.IsComingSoon,
                MaxDeliveryDistancekm = serviceArea.MaxDeliveryDistanceKm,
                MinOrderAmount = serviceArea.MinOrderAmount,
                DeliveryStartTime = serviceArea.DeliveryStartTime,
                DeliveryEndTime = serviceArea.DeliveryEndTime,
                EstimatedDeliveryDays = serviceArea.EstimatedDeliveryDays,
                DisplayName = serviceArea.DisplayName,
                Description = serviceArea.Description,
                NotAvailableMessage = serviceArea.NotAvailableMessage,
                Stores = serviceArea.Stores.Select(s => s.ToDTO()).ToList(),
                Orders = serviceArea.Orders.Select(o=>o.ToDTO()).ToList()
            };
        }





    }
    
}
