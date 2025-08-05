using Application.Dto.BannerEventSpecialDTOs;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;

namespace Application.Extension
{
    public static class RuleEvaluationExtensions
    {
        public static AppliedRuleDTO ToAppliedRuleDTO(this EventRule rule)
        {
            return new AppliedRuleDTO
            {
                Id = rule.Id,
                Priority = rule.Priority,
                Type = rule.Type.ToString(),
                TargetValue = rule.TargetValue ?? string.Empty,
                DiscountValue = rule.DiscountValue,
                MaxDiscount = rule.MaxDiscount,
                MinOrderValue = rule.MinOrderValue,
                FormattedDiscount = FormatDiscount(rule.DiscountType, rule.DiscountValue),
                AppliedAt = DateTime.UtcNow,
                RuleDescription = GenerateRuleDescription(rule)
            };
        }

        public static FailedRuleDTO ToFailedRuleDTO(this EventRule rule, string failureReason)
        {
            return new FailedRuleDTO
            {
                Id = rule.Id,
                Priority = rule.Priority,
                Type = rule.Type.ToString(),
                TargetValue = rule.TargetValue ?? string.Empty,
                MinOrderValue = rule.MinOrderValue,
                FailureReason = failureReason,
                FailedAt = DateTime.UtcNow,
                SuggestionToFix = GenerateSuggestion(rule, failureReason),
                RequiredAction = GenerateRequiredAction(rule, failureReason)
            };
        }

        private static string FormatDiscount(PromotionType discountType, decimal discountValue)
        {
            return discountType switch
            {
                PromotionType.Percentage => $"{discountValue}% OFF",
                PromotionType.FixedAmount => $"Rs.{discountValue} OFF",
                PromotionType.BuyOneGetOne => "Buy One Get One",
                PromotionType.FreeShipping => "Free Shipping",
                _ => $"Rs.{discountValue} discount"
            };
        }

        private static string GenerateRuleDescription(EventRule rule)
        {
            return rule.Type switch
            {
                RuleType.Category => $"Category-based rule: {rule.TargetValue}",
                RuleType.Product => $"Product-specific rule: {rule.TargetValue}",
                RuleType.PriceRange => $"Price range rule: {rule.TargetValue}",
                RuleType.PaymentMethod => $"Payment method rule: {rule.TargetValue}",
                RuleType.Geography => $"Geography rule: {rule.TargetValue}",
                RuleType.SubCategory => $"Sub-category rule: {rule.TargetValue}",
                RuleType.SubSubCategory => $"Sub-sub-category rule: {rule.TargetValue}",
                RuleType.All => "No restrictions - applies to all",
                _ => $"Custom rule: {rule.TargetValue}"
            };
        }

        private static string GenerateSuggestion(EventRule rule, string failureReason)
        {
            return rule.Type switch
            {
                RuleType.Category => "Add products from the required categories to your cart",
                RuleType.Product => "Include the specific products required by this rule",
                RuleType.PriceRange => "Increase your order value to meet the minimum requirement",
                RuleType.PaymentMethod => "Use the required payment method for this discount",
                RuleType.Geography => "This offer is only available in specific locations",
                _ => "Review the rule requirements and adjust your cart accordingly"
            };
        }

        private static string GenerateRequiredAction(EventRule rule, string failureReason)
        {
            if (failureReason.Contains("minimum order", StringComparison.OrdinalIgnoreCase))
            {
                return $"Increase order value to Rs.{rule.MinOrderValue ?? 0}";
            }

            return rule.Type switch
            {
                RuleType.Category => $"Add products from categories: {rule.TargetValue}",
                RuleType.Product => $"Include products: {rule.TargetValue}",
                RuleType.PaymentMethod => $"Use payment method: {rule.TargetValue}",
                RuleType.Geography => $"Available in: {rule.TargetValue}",
                RuleType.PriceRange => $"Order amount must be: {rule.TargetValue}",
                _ => $"Meet requirement: {rule.TargetValue}"
            };
        }
    }
}