using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class NearbyProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty; // Product name
        public string ImageUrl { get; set; } = string.Empty; // URL to the product main image only
        public string StoreCity { get; set; } = string.Empty; // City where the store is located

        public string StoreName { get; set; } = string.Empty; // Store name
        public double Price { get; set; }  // Price of the product
        public double Distance { get; set; } // Distance in kilometers
        public int StockQuantity { get; set; } // Quantity of the product available in this store

        public int StoreId { get; set; }                // For redirection or detail page
        public string StoreAddress { get; set; } = "";  // Formatted full address if needed
        public bool HasDiscount { get; set; }           // To highlight discounts
        public double? DiscountPrice { get; set; }      // Discounted price if available

    }
}
