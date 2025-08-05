using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventRuleDTO
    {
        public int Id { get; set; }
        public RuleType Type { get; set; }
        public string TargetValue { get; set; } = string.Empty;
        public string? Conditions { get; set; } = string.Empty;
        public PromotionType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int Priority { get; set; }

         // Enhanced properties for admin dashboard
        [JsonPropertyName("ruleDescription")]
        public string RuleDescription { get; set; } = string.Empty;

        [JsonPropertyName("isRestrictive")]
        public bool IsRestrictive { get; set; }

        [JsonPropertyName("targetAudience")]
        public string TargetAudience { get; set; } = string.Empty;

        [JsonPropertyName("formattedDiscount")]
        public string FormattedDiscount => DiscountType switch
        {
            PromotionType.Percentage => $"{DiscountValue}% OFF",
            PromotionType.FixedAmount => $"Rs.{DiscountValue} OFF",
            PromotionType.BuyOneGetOne => "Buy One Get One",
            PromotionType.FreeShipping => "Free Shipping",
            _ => "Special Discount"
        };

        [JsonPropertyName("formattedMaxDiscount")]
        public string FormattedMaxDiscount => MaxDiscount > 0 ? $"Max Rs.{MaxDiscount}" : "No Limit";

        [JsonPropertyName("formattedMinOrder")]
        public string FormattedMinOrder => MinOrderValue > 0 ? $"Min Rs.{MinOrderValue}" : "No Minimum";

        [JsonPropertyName("ruleComplexity")]
            public string RuleComplexity => Type switch
        {
            RuleType.Category or RuleType.SubCategory or RuleType.SubSubCategory => "Simple", // ✅ Fix: Was "Moderate"
            RuleType.Product => "Moderate",
            RuleType.PriceRange => "Moderate", // ✅ Fix: Was "Complex"  
            RuleType.PaymentMethod => "Simple", // ✅ Fix: Was "Complex"
            RuleType.Geography => "Simple",
            RuleType.All => "None",
            _ => "Advanced"
        };


        [JsonPropertyName("applicabilityScope")]
        public string ApplicabilityScope => Type switch
        {
            RuleType.All => "All Customers",
            RuleType.Category => "Category Specific",
            RuleType.SubCategory => "Sub-Category Specific",
            RuleType.SubSubCategory => "Sub-Sub-Category Specific",
            RuleType.Product => "Product Specific",
            RuleType.PriceRange => "Order Value Based",
            RuleType.PaymentMethod => "Payment Method Based",
            RuleType.Geography => "Geography Based",            
            _ => "Custom Criteria"
        };

        // public string RuleDescription => GenerateRuleDescription();

        // private string GenerateRuleDescription()
        // {
        //     var discount = DiscountType == PromotionType.Percentage
        //         ? $"{DiscountValue}%"
        //         : $"${DiscountValue}";

        //     return $"{Type}: {TargetValue} → {discount} discount";
        // }
    }
}
