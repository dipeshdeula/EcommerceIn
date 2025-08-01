// ✅ FIXED: Infrastructure/Persistence/Services/BannerEventRuleEngine.cs
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using Microsoft.Extensions.Logging;

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
            var result = new RuleEvaluationResultDTO { IsValid = true, Messages = new List<string>() };

            try
            {
                if (bannerEvent.Rules?.Any() != true)
                {
                    result.Messages.Add("No restrictions - applies to all products");
                    return result;
                }

                _logger.LogDebug("Evaluating {RuleCount} rules for event {EventId}", 
                    bannerEvent.Rules.Count, bannerEvent.Id);

                foreach (var rule in bannerEvent.Rules)
                {
                    var ruleResult = await EvaluateRuleAsync(rule, context);
                    if (!ruleResult.IsValid)
                    {
                        result.IsValid = false;
                        result.Messages.Add(ruleResult.Message);
                        result.FailedRules.Add(rule);
                    }
                    else
                    {
                        result.Messages.Add(ruleResult.Message);
                    }
                }

                _logger.LogDebug("Rule evaluation completed for event {EventId}. Valid: {IsValid}", 
                    bannerEvent.Id, result.IsValid);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rules for event {EventId}", bannerEvent.Id);
                return new RuleEvaluationResultDTO
                {
                    IsValid = false,
                    Messages = new List<string> { "Error evaluating rules" },
                    FailedRules = new List<EventRule>()
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
                return result.IsValid;
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
                    _ => new SingleRuleResultDTO { IsValid = false, Message = $" Unknown rule type: {rule.Type}" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule {RuleType} with value {RuleValue}", 
                    rule.Type, rule.TargetValue);
                return new SingleRuleResultDTO 
                { 
                    IsValid = false, 
                    Message = $" Error evaluating {rule.Type} rule" 
                };
            }
        }

        private async Task<SingleRuleResultDTO> ValidateCategoryRule(EventRule rule, EvaluationContextDTO context)
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
                    Message = $" Cart contains products from allowed categories: {string.Join(", ", categoryNames)}"
                };
            }

            return new SingleRuleResultDTO
            {
                IsValid = false,
                Message = "Cart doesn't contain products from required categories"
            };
        }

        private async Task<SingleRuleResultDTO> ValidateSubCategoryRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedSubCategoryIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
            var matchingItems = context.CartItems
                .Where(item => allowedSubCategoryIds.Contains(item.Product?.SubSubCategory?.SubCategoryId ?? 0))
                .ToList();

            return await Task.FromResult(matchingItems.Any())
                ? new SingleRuleResultDTO { IsValid = true, Message = " Cart contains products from allowed subcategories" }
                : new SingleRuleResultDTO { IsValid = false, Message = "Cart doesn't contain products from required subcategories" };
        }

        private async Task<SingleRuleResultDTO> ValidateSubSubCategoryRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedSubSubCategoryIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
            var matchingItems = context.CartItems
                .Where(item => allowedSubSubCategoryIds.Contains(item.Product?.SubSubCategoryId ?? 0))
                .ToList();

            return await Task.FromResult(matchingItems.Any())
                ? new SingleRuleResultDTO { IsValid = true, Message = " Cart contains products from allowed sub-subcategories" }
                : new SingleRuleResultDTO { IsValid = false, Message = "Cart doesn't contain products from required sub-subcategories" };
        }

        private SingleRuleResultDTO ValidateProductRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedProductIds = rule.TargetValue.Split(',').Select(int.Parse).ToList();
            var matchingItems = context.CartItems
                .Where(item => allowedProductIds.Contains(item.ProductId))
                .ToList();

            return matchingItems.Any()
                ? new SingleRuleResultDTO { IsValid = true, Message = "Cart contains required products" }
                : new SingleRuleResultDTO { IsValid = false, Message = "Cart must contain specific products for this promotion" };
        }

        

        private SingleRuleResultDTO ValidatePriceRangeRule(EventRule rule, EvaluationContextDTO context)
        {
            var parts = rule.TargetValue.Split('-');
            if (parts.Length != 2)
                return new SingleRuleResultDTO { IsValid = false, Message = "Invalid price range format" };

            if (!decimal.TryParse(parts[0], out var minPrice) || !decimal.TryParse(parts[1], out var maxPrice))
                return new SingleRuleResultDTO { IsValid = false, Message = "Invalid price range values" };

            var cartTotal = context.CartItems.Sum(item => item.ReservedPrice * item.Quantity);

            return cartTotal >= minPrice && cartTotal <= maxPrice
                ? new SingleRuleResultDTO { IsValid = true, Message = $"Cart total Rs.{cartTotal:F2} is within range Rs.{minPrice:F2}-{maxPrice:F2}" }
                : new SingleRuleResultDTO { IsValid = false, Message = $"Cart total Rs.{cartTotal:F2} is outside required range Rs.{minPrice:F2}-{maxPrice:F2}" };
        }

        

        private SingleRuleResultDTO ValidateGeographyRule(EventRule rule, EvaluationContextDTO context)
        {
            var allowedCities = rule.TargetValue.Split(',').Select(c => c.Trim()).ToList();
            var userCity = context.User?.Addresses?.FirstOrDefault()?.City?.Trim() ?? "";

            return allowedCities.Contains(userCity, StringComparer.OrdinalIgnoreCase)
                ? new SingleRuleResultDTO { IsValid = true, Message = $" Available in {userCity}" }
                : new SingleRuleResultDTO { IsValid = false, Message = $" Not available in {userCity}. Available in: {string.Join(", ", allowedCities)}" };
        }

        private SingleRuleResultDTO ValidatePaymentMethodRule(EventRule rule, EvaluationContextDTO context)
        {
            if (string.IsNullOrEmpty(context.PaymentMethod))
            {
                return new SingleRuleResultDTO { IsValid = true, Message = "⏳ Payment method will be validated at checkout" };
            }

            var allowedMethods = rule.TargetValue.Split(',').Select(m => m.Trim()).ToList();

            return allowedMethods.Contains(context.PaymentMethod, StringComparer.OrdinalIgnoreCase)
                ? new SingleRuleResultDTO { IsValid = true, Message = $" Payment method {context.PaymentMethod} is supported" }
                : new SingleRuleResultDTO { IsValid = false, Message = $" Payment method {context.PaymentMethod} not supported. Use: {string.Join(", ", allowedMethods)}" };
        }

        //  Get category names for better user messages
        private async Task<List<string>> GetCategoryNames(List<int> categoryIds)
        {
            try
            {
                var categories = await _unitOfWork.Categories.GetAllAsync(
                    predicate: c => categoryIds.Contains(c.Id) && !c.IsDeleted,cancellationToken:default);
                return categories.Select(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category names");
                return categoryIds.Select(id => $"Category {id}").ToList();
            }
        }

        
    }
}