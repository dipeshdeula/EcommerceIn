using Application.Dto;
using Application.Dto.BannerEventSpecialDTOs;
using Domain.Entities;
using Domain.Entities.Common;

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

        public static CategoryDTO ToDTO(this Category category)
        {
            return new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
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
                StockQuantity = product.StockQuantity,
                ReservedStock = product.ReservedStock,  
                Sku = product.Sku,
                Weight = product.Weight,
                Reviews = product.Reviews,
                Rating = product.Rating,
                IsDeleted = product.IsDeleted,
                Images = product.Images.Select(pi => pi.ToDTO()).ToList() // Map product images
            };
        }

        // Map ProductImage to ProductImageDTO
        public static ProductImageDTO ToDTO(this ProductImage productImage)
        {
            return new ProductImageDTO
            {
                Id = productImage.Id,
                ImageUrl = productImage.ImageUrl
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

        public static ProductStoreDTO ToDTO (this ProductStore productStore)
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
            return new CartItemDTO
            {
                Id = cartItem.Id,
                UserId = cartItem.UserId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                IsDeleted = cartItem.IsDeleted,
                User = cartItem.User?.ToDTO(),
                Product = cartItem.Product?.ToDTO()
            };
        }

        public static IEnumerable<CartItemDTO> ToDTO(this IEnumerable<CartItem> cartItems)
        {
            return cartItems.Select(cartItem => cartItem.ToDTO());
        }

        public static OrderDTO ToDTO(this Order order)
        {
            return new OrderDTO
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                Items = order.Items.Select(oi => oi.ToDTO()).ToList()
            };
        }

        public static OrderItemDTO ToDTO(this OrderItem orderItem)
        {
            return new OrderItemDTO
            {
                Id = orderItem.Id,
                OrderId = orderItem.OrderId,
                ProductId = orderItem.ProductId,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
               /* Product = orderItem.Product?.ToDTO(), // Map Product to ProductDTO
                Order = null // Avoid circular reference*/
            };
        }

        public static NearbyProductDto ToNearbyProductDto(this ProductStore productStore, double distance)
        {
            return new NearbyProductDto
            {
                ProductId = productStore.ProductId,
                Name = productStore.Product.Name,
                ImageUrl = productStore.Product.Images.FirstOrDefault()?.ImageUrl,
                StoreCity = productStore.Store.Address?.City,
                StoreName = productStore.Store.Name,
                MarketPrice = productStore.Product.MarketPrice,
                CostPrice = productStore.Product.CostPrice,
                DiscountPrice = productStore.Product.DiscountPrice,
                StockQuantity = productStore.Product.StockQuantity,
                StoreId = productStore.StoreId,
                StoreAddress = $"{productStore.Store.Address?.Street},{productStore.Store.Address?.City}",
                Distance = distance,
                HasDiscount = productStore.Product.DiscountPrice.HasValue
            };
        }

        public static BannerEventSpecialDTO ToDTO(this BannerEventSpecial bannerEventSpecial)
        {
            return new BannerEventSpecialDTO
            {
                Id = bannerEventSpecial.Id,
                Name = bannerEventSpecial.Name,
                Description = bannerEventSpecial.Description,
                TagLine = bannerEventSpecial.TagLine,
                EventType = bannerEventSpecial.EventType,
                PromotionType = bannerEventSpecial.PromotionType,
                DiscountValue = bannerEventSpecial.DiscountValue,
                MaxDiscountAmount = bannerEventSpecial.MaxDiscountAmount,
                MinOrderValue = bannerEventSpecial.MinOrderValue,
                StartDate = bannerEventSpecial.StartDate,
                EndDate = bannerEventSpecial.EndDate,
                ActiveTimeSlot = bannerEventSpecial.ActiveTimeSlot,
                MaxUsageCount = bannerEventSpecial.MaxUsageCount,
                CurrentUsageCount = bannerEventSpecial.CurrentUsageCount,
                MaxUsagePerUser = bannerEventSpecial.MaxUsagePerUser,
                Priority = bannerEventSpecial.Priority,
                IsActive = bannerEventSpecial.IsActive,
                IsDeleted = bannerEventSpecial.IsDeleted,
                Status = bannerEventSpecial.Status,
                Images = bannerEventSpecial.Images?.Select(i => i.ToDTO()).ToList() ?? new List<BannerImageDTO>(),
                Rules = bannerEventSpecial.Rules?.Select(r => r.ToDTO()).ToList() ?? new List<EventRuleDTO>(),
                EventProducts = bannerEventSpecial.EventProducts?.Select(ep => ep.ToDTO()).ToList() ?? new List<EventProductDTO>()
            };
        }

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
                IsActive = eventRule.IsActive,
                Priority = eventRule.Priority
            };
        }

        public static EventProductDTO ToDTO(this EventProduct eventProduct)
        {
            return new EventProductDTO
            {
                Id = eventProduct.Id,
                BannerEventId = eventProduct.BannerEventId,
                ProductId = eventProduct.ProductId,
                ProductName = eventProduct.Product?.Name ?? string.Empty,
                SpecificDiscount = eventProduct.SpecificDiscount,
                IsActive = eventProduct.IsActive
            };
        }

        public static BannerImageDTO ToDTO(this BannerImage bannerImage)
        {
            return new BannerImageDTO
            {
                Id = bannerImage.Id,
                ImageUrl = bannerImage.ImageUrl
            };
        }


        public static PaymentMethodDTO ToDTO(this PaymentMethod paymentMethod)
        {
            return new PaymentMethodDTO {
                Id = paymentMethod.Id,
                Name = paymentMethod.Name,
                Type = paymentMethod.Type,
                Logo = paymentMethod.Logo,
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
                Currency = paymentRequest.Currency,
                Description = paymentRequest.Description,
                KhaltiPidx = paymentRequest.KhaltiPidx,
                EsewaTransactionId = paymentRequest.EsewaTransactionId,
                CreatedAt = paymentRequest.CreatedAt,
                UpdatedAt = paymentRequest.UpdatedAt,
            };
        }

    }
}
