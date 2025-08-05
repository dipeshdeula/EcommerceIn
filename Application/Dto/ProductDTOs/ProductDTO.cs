using System.Text.Json.Serialization;
using Application.Dto.CategoryDTOs;

namespace Application.Dto.ProductDTOs
{
    public class ProductDTO
    {
        //  BASIC PRODUCT DATA 
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MarketPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public int ReservedStock { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public int Reviews { get; set; }
        public decimal Rating { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public int CategoryId { get; set; }
        public int SubSubCategoryId { get; set; }

        //  COMPUTED BASIC PROPERTIES 
        public int AvailableStock => Math.Max(0, StockQuantity - ReservedStock);
        public bool IsInStock => AvailableStock > 0;
        public bool HasProductDiscount { get; set; } = false;
        public decimal BasePrice { get; set; } = 0;
        public decimal ProductDiscountAmount { get; set; } = 0;


        //  DISPLAY FORMATTING 
        public string FormattedMarketPrice => $"Rs. {MarketPrice:F2}";
        public string FormattedBasePrice { get; set; } = "";

        public string FormattedDiscountAmount { get; set; } = "";

        public string StockStatus => AvailableStock <= 0 ? "Out of Stock"
                                   : AvailableStock <= 10 ? "Low Stock"
                                   : "In Stock";

        //  NAVIGATION PROPERTIES 
        public ICollection<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();

        //  COMPOSITION PROPERTIES (Set by services) 
        public ProductPricingDTO? Pricing { get; set; }
        public ProductStockDTO? Stock { get; set; }
        // convenience properties:
        public decimal CurrentPrice => Pricing?.EffectivePrice ?? BasePrice;
        public bool IsOnSale => Pricing?.HasAnyDiscount ?? HasProductDiscount;
        public bool CanAddToCart => Stock?.CanAddToCart() ?? (IsInStock && !IsDeleted);
        public string DisplayPrice => $"Rs. {CurrentPrice:F2}";
        public string DisplayStatus => !CanAddToCart ? "Unavailable" : IsOnSale ? "On Sale" : "Available";

        public decimal TotalSavingsAmount => Pricing?.TotalDiscountAmount ?? ProductDiscountAmount;
        public string FormattedSavings => TotalSavingsAmount > 0 ? $"Save Rs. {TotalSavingsAmount:F2}" : string.Empty;
        
        [JsonPropertyName("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [JsonPropertyName("statusBadge")]
        public string StatusBadge => IsDeleted ? "DELETED" : "ACTIVE";

        [JsonPropertyName("adminNotes")]
        public string AdminNotes => IsDeleted 
            ? $"Deleted on {DeletedAt?.ToString("yyyy-MM-dd") ?? "Unknown date"}"
            : "Active product";

        // ✅ Admin-specific display properties
        [JsonPropertyName("managementActions")]
        public List<string> ManagementActions => IsDeleted 
            ? new List<string> { "Restore", "Permanently Delete" }
            : new List<string> { "Edit", "Delete", "View Analytics" };

    }
}
