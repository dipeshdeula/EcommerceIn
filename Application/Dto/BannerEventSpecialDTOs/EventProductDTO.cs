using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventProductDTO
    {
        public int Id { get; set; }
        public int BannerEventId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("specificDiscount")]
        public decimal SpecificDiscount { get; set; } = 0; // ✅ Default to 0 instead of nullable

        public DateTime AddedAt { get; set; }
        public decimal ProductMarketPrice { get; set; }
        public string ProductImageUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal CalculatedDiscountPrice { get; set; }

        [JsonPropertyName("formattedSpecificDiscount")]
        public string FormattedSpecificDiscount { get; set; } = "Event Discount Applied";

        [JsonPropertyName("hasSpecificDiscount")]
        public bool HasSpecificDiscount { get; set; }

        // ✅ Additional calculated properties
        [JsonPropertyName("discountAmount")]
        public decimal DiscountAmount => Math.Max(0, ProductMarketPrice - CalculatedDiscountPrice);

        [JsonPropertyName("discountPercentage")]
        public decimal DiscountPercentage => ProductMarketPrice > 0
            ? Math.Round((DiscountAmount / ProductMarketPrice) * 100, 2)
            : 0;

        [JsonPropertyName("formattedOriginalPrice")]
        public string FormattedOriginalPrice => $"Rs.{ProductMarketPrice:F2}";

        [JsonPropertyName("formattedDiscountPrice")]
        public string FormattedDiscountPrice => $"Rs.{CalculatedDiscountPrice:F2}";

        [JsonPropertyName("formattedSavings")]
        public string FormattedSavings => $"Save Rs.{DiscountAmount:F2} ({DiscountPercentage}%)";

        [JsonPropertyName("discountEffectiveness")]
        public string DiscountEffectiveness => DiscountPercentage switch
        {
            >= 50 => "Excellent Deal",
            >= 30 => "Great Savings",
            >= 15 => "Good Value",
            >= 5 => "Small Discount",
            _ => "Minimal Savings"
        };

        [JsonPropertyName("productStatus")]
        public string ProductStatus => CalculatedDiscountPrice < ProductMarketPrice ? "Discounted" : "Regular Price";
    }
}
