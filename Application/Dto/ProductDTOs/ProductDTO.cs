using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;

namespace Application.Dto.ProductDTOs
{
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // e.g. "home-appliances"
        public string Description { get; set; } = string.Empty;
        public decimal MarketPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPrice { get; set; } // Nullable if no discount
        public int StockQuantity { get; set; }
        public int ReservedStock { get; set; } // Reserved stock for orders
        public string Sku { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public int Reviews { get; set; }
        public decimal Rating { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        public int CategoryId { get; set; }
        public int SubSubCategoryId { get; set; }

        // ✅ ADDED: Dynamic pricing fields
        public decimal OriginalPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal EffectivePrice { get; set; } // Current price after events
        public bool HasActiveEvent { get; set; }
        public bool HasActiveDiscount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int? ActiveEventId { get; set; }
        public string? ActiveEventName { get; set; }
        public string? EventTagLine { get; set; }
        public PromotionType? PromotionType { get; set; } 

        public DateTime? EventEndDate { get; set; }        

        // ✅ Computed properties
        public int AvailableStock => StockQuantity - ReservedStock;
        public bool IsInStock => AvailableStock > 0;
        public bool IsOnSale => HasActiveEvent || DiscountPrice.HasValue;

        // Formatted properties
        public string FormattedPrice => $"Rs. {EffectivePrice:F2}";
        public string FormattedOriginalPrice => $"Rs. {OriginalPrice:F2}";
        public string FormattedMarketPrice => $"Rs.{MarketPrice:F2}";
        public string FormattedEffectivePrice => $"Rs.{EffectivePrice:F2}";
        public string FormattedSavings => HasActiveEvent ? $"Save Rs.{DiscountAmount:F2}" : string.Empty;
        public string FormattedCurrentPrice => $"Rs.{CurrentPrice:F2}";

        // Navigation property for product images
        public ICollection<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();
    }
}
