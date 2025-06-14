using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class NearbyProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty; // Product name
        public string ImageUrl { get; set; } = string.Empty; // URL to the product main image only
        public string StoreCity { get; set; } = string.Empty; // City where the store is located

        public string StoreName { get; set; } = string.Empty; // Store name
        public decimal MarketPrice { get; set; }  // Price of the product
        public decimal CostPrice { get; set; }
        public double Distance { get; set; } // Distance in kilometers
        public int StockQuantity { get; set; } // Quantity of the product available in this store

        public int StoreId { get; set; }                // For redirection or detail page
        public string StoreAddress { get; set; } = "";  // Formatted full address if needed
        public bool HasDiscount { get; set; }           // To highlight discounts
        public decimal? DiscountPrice { get; set; }      // Discounted price if available

        // Dynamic pricing support
        public decimal CurrentPrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public bool HasActiveEvent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string? ActiveEventName { get; set; }

        //  Formatted properties
        public string FormattedPrice => $"Rs.{EffectivePrice:F2}";
        public string FormattedDistance => $"{Distance:F1} km";
        public bool IsInStock => StockQuantity > 0;

        public ProductDTO? ProductDTO;



    }
}
