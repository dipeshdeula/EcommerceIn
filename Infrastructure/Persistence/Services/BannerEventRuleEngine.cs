using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Persistence.Services
{
    public class BannerEventRuleEngine : IBannerEventRuleEngine
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BannerEventRuleEngine> _logger;

        public BannerEventRuleEngine(IUnitOfWork unitOfWork, ILogger<BannerEventRuleEngine> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RuleEvaluationResultDTO> EvaluateAllRulesAsync(
            BannerEventSpecial bannerEvent,
            EvaluationContextDTO context)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new RuleEvaluationResultDTO 
            { 
                IsValid = true, 
                IsEligible = true,
                Messages = new List<string>(),
                AppliedRules = new List<AppliedRuleDTO>(),
                FailedRules = new List<FailedRuleDTO>()
            };

            try
            {
                // ✅ Handle case when no rules exist
                if (bannerEvent.Rules?.Any() != true)
                {
                    result.IsValid = true;
                    result.IsEligible = true;
                    result.Messages.Add("No restrictions - applies to all products");
                    result.RulesEvaluated = 0;
                    
                    // ✅ Calculate event-based discount
                    result.CalculatedDiscount = CalculateEventDiscount(bannerEvent, context.OrderTotal ?? 0);
                    result.FinalDiscount = Math.Min(result.CalculatedDiscount, bannerEvent.MaxDiscountAmount ?? result.CalculatedDiscount);
                    result.FormattedDiscount = FormatDiscount(bannerEvent.PromotionType, result.FinalDiscount);
                    
                    stopwatch.Stop();
                    result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                _logger.LogDebug("Evaluating {RuleCount} rules for event {EventId}", 
                    bannerEvent.Rules.Count, bannerEvent.Id);

                result.RulesEvaluated = bannerEvent.Rules.Count;
                bool allRulesPassed = true;

                // ✅ Evaluate all rules - sort by priority (lowest number = highest priority)
                var sortedRules = bannerEvent.Rules.OrderBy(r => r.Priority).ToList();

                foreach (var rule in sortedRules)
                {
                    var ruleResult = await EvaluateRuleAsync(rule, context);
                    
                    if (!ruleResult.IsValid)
                    {
                        allRulesPassed = false;
                        result.Messages.Add(ruleResult.Message);
                        
                        // ✅ Add to failed rules with proper DTO
                        result.FailedRules.Add(rule.ToFailedRuleDTO(ruleResult.Message));
                    }
                    else
                    {
                        result.Messages.Add(ruleResult.Message);
                        
                        // ✅ Add to applied rules with proper DTO
                        result.AppliedRules.Add(rule.ToAppliedRuleDTO());
                    }
                }

                // ✅ Set final eligibility
                result.IsValid = allRulesPassed;
                result.IsEligible = allRulesPassed;

                // ✅ Calculate discount based on rules
                if (allRulesPassed)
                {
                    // Find the highest priority rule (lowest priority number) that applies
                    var bestRule = result.AppliedRules.OrderBy(r => r.Priority).FirstOrDefault();
                    
                    if (bestRule != null)
                    {
                        // Use rule-based discount
                        var ruleEntity = sortedRules.First(r => r.Id == bestRule.Id);
                        result.CalculatedDiscount = CalculateRuleDiscount(ruleEntity, context.OrderTotal ?? 0);
                        
                        // Apply rule max discount cap if exists
                        if (ruleEntity.MaxDiscount.HasValue)
                        {
                            result.CalculatedDiscount = Math.Min(result.CalculatedDiscount, ruleEntity.MaxDiscount.Value);
                        }
                    }
                    else
                    {
                        // Fallback to event discount
                        result.CalculatedDiscount = CalculateEventDiscount(bannerEvent, context.OrderTotal ?? 0);
                    }
                    
                    // Apply event-wide maximum discount cap
                    result.FinalDiscount = Math.Min(result.CalculatedDiscount, bannerEvent.MaxDiscountAmount ?? result.CalculatedDiscount);
                    result.FormattedDiscount = FormatDiscount(bannerEvent.PromotionType, result.FinalDiscount);
                }
                else
                {
                    result.CalculatedDiscount = 0;
                    result.FinalDiscount = 0;
                    result.FormattedDiscount = "No discount - Rules not met";
                }

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                _logger.LogDebug("Rule evaluation completed for event {EventId}. Eligible: {IsEligible}, Discount: Rs.{Discount:F2}", 
                    bannerEvent.Id, result.IsEligible, result.FinalDiscount);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error evaluating rules for event {EventId}", bannerEvent.Id);
                
                return new RuleEvaluationResultDTO
                {
                    IsValid = false,
                    IsEligible = false,
                    Messages = new List<string> { "Error evaluating rules" },
                    AppliedRules = new List<AppliedRuleDTO>(),
                    FailedRules = new List<FailedRuleDTO>(),
                    CalculatedDiscount = 0,
                    FinalDiscount = 0,
                    FormattedDiscount = "Error",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    RulesEvaluated = bannerEvent.Rules?.Count ?? 0
                };
            }
        }

        public async Task<bool> ValidateCartRulesAsync(int eventId, List<CartItem> cartItems, User user, string? paymentMethod = null)
        {
            try
            {
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(eventId);
                if (bannerEvent == null) return false;

                var context = new EvaluationContextDTO
                {
                    CartItems = cartItems,
                    User = user,
                    PaymentMethod = paymentMethod,
                    OrderTotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity),
                    EvaluationTime = DateTime.UtcNow
                };

                var result = await EvaluateAllRulesAsync(bannerEvent, context);
                return result.IsEligible; // ✅ Use IsEligible instead of IsValid
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart rules for event {EventId}", eventId);
                return false;
            }
        }

        private async Task<SingleRuleResultDTO> EvaluateRuleAsync(EventRule rule, EvaluationContextDTO context)
        {
            try
            {
                // ✅ Check minimum order value first (applies to all rule types)
                if (rule.MinOrderValue.HasValue && (context.OrderTotal ?? 0) < rule.MinOrderValue.Value)
                {
                    return new SingleRuleResultDTO 
                    { 
                        IsValid = false, 
                        Message = $"Minimum order value Rs.{rule.MinOrderValue:F2} not met (current: Rs.{context.OrderTotal:F2})" 
                    };
                }

                return rule.Type switch 
                {
                    RuleType.Category => await ValidateCategoryRule(rule, context),
                    RuleType.SubCategory => await ValidateSubCategoryRule(rule, context),
                    RuleType.SubSubCategory => await ValidateSubSubCategoryRule(rule, context),
                    RuleType.Product => ValidateProductRule(rule, context),
                    RuleType.PriceRange => ValidatePriceRangeRule(rule, context),
                    RuleType.Geography => ValidateGeographyRule(rule, context),
                    RuleType.PaymentMethod => ValidatePaymentMethodRule(rule, context),
                    RuleType.All => new SingleRuleResultDTO { IsValid = true, Message = "No restrictions" },
                    _ => new SingleRuleResultDTO { IsValid = false, Message = $"Unknown rule type: {rule.Type}" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule {RuleType} with value {RuleValue}", 
                    rule.Type, rule.TargetValue);
                return new SingleRuleResultDTO 
                { 
                    IsValid = false, 
                    Message = $"Error evaluating {rule.Type} rule" 
                };
            }
        }

        // ✅ Rule validation methods remain the same but with improved error messages
        private async Task<SingleRuleResultDTO> ValidateCategoryRule(EventRule rule, EvaluationContextDTO context)
        {
            try
            {
                var allowedCategoryIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
                var matchingItems = context.CartItems
                    .Where(item => allowedCategoryIds.Contains(item.Product?.CategoryId ?? 0))
                    .ToList();

                if (matchingItems.Any())
                {
                    var categoryNames = await GetCategoryNames(allowedCategoryIds);
                    return new SingleRuleResultDTO
                    {
                        IsValid = true,
                        Message = $"✅ Cart contains products from allowed categories: {string.Join(", ", categoryNames)}"
                    };
                }

                var allCategoryNames = await GetCategoryNames(allowedCategoryIds);
                return new SingleRuleResultDTO
                {
                    IsValid = false,
                    Message = $"❌ Cart must contain products from categories: {string.Join(", ", allCategoryNames)}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating category rule");
                return new SingleRuleResultDTO
                {
                    IsValid = false,
                    Message = "Error validating category requirements"
                };
            }
        }

        private async Task<SingleRuleResultDTO> ValidateSubCategoryRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedSubCategoryIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
            var matchingItems = context.CartItems
                .Where(item => allowedSubCategoryIds.Contains(item.Product?.SubSubCategory?.SubCategoryId ?? 0))
                .ToList();

            return await Task.FromResult(matchingItems.Any())
                ? new SingleRuleResultDTO { IsValid = true, Message = "✅ Cart contains products from allowed subcategories" }
                : new SingleRuleResultDTO { IsValid = false, Message = "❌ Cart must contain products from required subcategories" };
        }

        private async Task<SingleRuleResultDTO> ValidateSubSubCategoryRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedSubSubCategoryIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
            var matchingItems = context.CartItems
                .Where(item => allowedSubSubCategoryIds.Contains(item.Product?.SubSubCategoryId ?? 0))
                .ToList();

            return await Task.FromResult(matchingItems.Any())
                ? new SingleRuleResultDTO { IsValid = true, Message = "✅ Cart contains products from allowed sub-subcategories" }
                : new SingleRuleResultDTO { IsValid = false, Message = "❌ Cart must contain products from required sub-subcategories" };
        }

        private SingleRuleResultDTO ValidateProductRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedProductIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
            var matchingItems = context.CartItems
                .Where(item => allowedProductIds.Contains(item.ProductId))
                .ToList();

            return matchingItems.Any()
                ? new SingleRuleResultDTO { IsValid = true, Message = "✅ Cart contains required products" }
                : new SingleRuleResultDTO { IsValid = false, Message = $"❌ Cart must contain specific products (IDs: {rule.TargetValue})" };
        }

        private SingleRuleResultDTO ValidatePriceRangeRule(EventRule rule, EvaluationContextDTO context)
        {
            var parts = rule.TargetValue.Split('-');
            if (parts.Length != 2)
                return new SingleRuleResultDTO { IsValid = false, Message = "❌ Invalid price range format" };

            if (!decimal.TryParse(parts[0], out var minPrice) || !decimal.TryParse(parts[1], out var maxPrice))
                return new SingleRuleResultDTO { IsValid = false, Message = "❌ Invalid price range values" };

            var cartTotal = context.OrderTotal ?? context.CartItems.Sum(item => item.ReservedPrice * item.Quantity);

            return cartTotal >= minPrice && cartTotal <= maxPrice
                ? new SingleRuleResultDTO { IsValid = true, Message = $"✅ Cart total Rs.{cartTotal:F2} is within range Rs.{minPrice:F2}-{maxPrice:F2}" }
                : new SingleRuleResultDTO { IsValid = false, Message = $"❌ Cart total Rs.{cartTotal:F2} must be between Rs.{minPrice:F2}-{maxPrice:F2}" };
        }

        private SingleRuleResultDTO ValidateGeographyRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedCities = rule.TargetValue.Split(',').Select(c => c.Trim()).ToList();
            var userCity = context.User?.Addresses?.FirstOrDefault()?.City?.Trim() ?? "";

            if (string.IsNullOrEmpty(userCity))
            {
                return new SingleRuleResultDTO { IsValid = false, Message = "❌ User address required for geography validation" };
            }

            return allowedCities.Contains(userCity, StringComparer.OrdinalIgnoreCase)
                ? new SingleRuleResultDTO { IsValid = true, Message = $"✅ Available in {userCity}" }
                : new SingleRuleResultDTO { IsValid = false, Message = $"❌ Not available in {userCity}. Available in: {string.Join(", ", allowedCities)}" };
        }

        private SingleRuleResultDTO ValidatePaymentMethodRule(EventRule rule, EvaluationContextDTO context)
        {
            if (string.IsNullOrEmpty(context.PaymentMethod))
            {
                return new SingleRuleResultDTO { IsValid = true, Message = "⏳ Payment method will be validated at checkout" };
            }

            var allowedMethods = rule.TargetValue.Split(',').Select(m => m.Trim()).ToList();

            return allowedMethods.Contains(context.PaymentMethod, StringComparer.OrdinalIgnoreCase)
                ? new SingleRuleResultDTO { IsValid = true, Message = $"✅ Payment method {context.PaymentMethod} is supported" }
                : new SingleRuleResultDTO { IsValid = false, Message = $"❌ Payment method {context.PaymentMethod} not supported. Use: {string.Join(", ", allowedMethods)}" };
        }

        // ✅ Helper methods for discount calculation
        private decimal CalculateEventDiscount(BannerEventSpecial bannerEvent, decimal orderAmount)
        {
            return bannerEvent.PromotionType switch
            {
                PromotionType.Percentage => orderAmount * (bannerEvent.DiscountValue / 100),
                PromotionType.FixedAmount => bannerEvent.DiscountValue,
                _ => 0
            };
        }

        private decimal CalculateRuleDiscount(EventRule rule, decimal orderAmount)
        {
            return rule.DiscountType switch
            {
                PromotionType.Percentage => orderAmount * (rule.DiscountValue / 100),
                PromotionType.FixedAmount => rule.DiscountValue,
                _ => 0
            };
        }

        private string FormatDiscount(PromotionType promotionType, decimal discountValue)
        {
            return promotionType switch
            {
                PromotionType.Percentage => $"{discountValue}% OFF",
                PromotionType.FixedAmount => $"Rs.{discountValue:F2} OFF",
                PromotionType.BuyOneGetOne => "Buy One Get One",
                PromotionType.FreeShipping => "Free Shipping",
                _ => $"Rs.{discountValue:F2} discount"
            };
        }

        // ✅ Get category names for better user messages
        private async Task<List<string>> GetCategoryNames(List<int> categoryIds)
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllAsync(
                    predicate: c => categoryIds.Contains(c.Id) && !c.IsDeleted,
                    cancellationToken: default);
                return categories.Select(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category names");
                return categoryIds.Select(id => $"Category {id}").ToList();
            }
        }
    }

    // ✅ Supporting DTO for single rule evaluation
    public class SingleRuleResultDTO
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}