using Application.Common;
using Application.Common.Models;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Features.BannerSpecialEvent.Queries
{
    public record GetAllBannerEventSpecialQuery(
        int PageNumber = 1,
        int PageSize = 10,
        bool IncludeDeleted = false,
        string? Status = null,
        bool? IsActive = null
    ) : IRequest<Result<PagedResult<BannerEventSpecialDTO>>>;

    public class GetAllBannerEventSpecialQueryHandler : IRequestHandler<GetAllBannerEventSpecialQuery, Result<PagedResult<BannerEventSpecialDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllBannerEventSpecialQueryHandler> _logger;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly IBannerEventAnalyticsService _analyticsService;

        public GetAllBannerEventSpecialQueryHandler(
            IUnitOfWork unitOfWork,
            INepalTimeZoneService nepalTimeZoneService,
            IBannerEventAnalyticsService analyticsService,
            ILogger<GetAllBannerEventSpecialQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _nepalTimeZoneService = nepalTimeZoneService;
            _analyticsService = analyticsService;
        }

        public async Task<Result<PagedResult<BannerEventSpecialDTO>>> Handle(GetAllBannerEventSpecialQuery request, CancellationToken cancellationToken)
        {
            try
            {

                // Build predicate for filtering
                Expression<Func<BannerEventSpecial, bool>> predicate = e => true;

                if (!request.IncludeDeleted)
                {
                    predicate = e => !e.IsDeleted;
                }

                if (request.IsActive.HasValue)
                {
                    var isActiveFilter = request.IsActive.Value;
                    predicate = predicate.And(e => e.IsActive == isActiveFilter);
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    // FluentValidation already ensured this is valid
                    Enum.TryParse<EventStatus>(request.Status, true, out var status);
                    predicate = predicate.And(e => e.Status == status);
                }

                // Get total count for pagination
                var totalCount = await _unitOfWork.BannerEventSpecials.CountAsync(predicate, cancellationToken);

                // Get banner events with all related data
                var bannerEvents = await _unitOfWork.BannerEventSpecials.GetAllAsync(
                    predicate: predicate,
                    orderBy: query => query.OrderByDescending(e => e.CreatedAt)
                                          .ThenByDescending(e => e.Priority),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties: "Images,Rules,EventProducts,EventProducts.Product,EventProducts.Product.Category,EventProducts.Product.Images",
                    includeDeleted: request.IncludeDeleted);

                // Convert to DTOs with enhanced analytics data
                var bannerEventDTOs = new List<BannerEventSpecialDTO>();

                foreach (var bannerEvent in bannerEvents)
                {
                    // Use Nepal timezone service for proper conversion
                    var dto = bannerEvent.ToDTO(_nepalTimeZoneService);

                    //  Get real-time usage statistics from analytics service
                    var usageStats = await _analyticsService.GetEventUsageStatisticsAsync(bannerEvent.Id);

                    if (usageStats.Succeeded && usageStats.Data != null)
                    {
                        //  Update DTO with real usage data
                        dto.CurrentUsageCount = usageStats.Data.TotalUsages;
                        dto.UsagePercentage = dto.MaxUsageCount > 0
                            ? Math.Round((decimal)dto.CurrentUsageCount / dto.MaxUsageCount * 100, 2)
                            : 0;
                        dto.RemainingUsage = Math.Max(0, dto.MaxUsageCount - dto.CurrentUsageCount);
                        dto.IsUsageLimitReached = dto.CurrentUsageCount >= dto.MaxUsageCount;

                        // Add comprehensive usage summary
                        dto.UsageSummary = new EventUsageSummaryDTO
                        {
                            TotalUsages = usageStats.Data.TotalUsages,
                            UniqueUsers = usageStats.Data.UniqueUsers,
                            TotalDiscountGiven = usageStats.Data.TotalDiscount,
                            AverageDiscountPerUser = usageStats.Data.AverageDiscount,
                            ConversionRate = CalculateConversionRate(usageStats.Data.TotalUsages, dto.MaxUsageCount),
                            LastUsedAt = await GetLastUsageDate(bannerEvent.Id, cancellationToken),
                            MostActiveDay = await GetMostActiveDay(bannerEvent.Id, cancellationToken)
                        };
                    }
                    else
                    {
                        //  Fallback to basic data if analytics service fails
                        dto.CurrentUsageCount = 0;
                        dto.UsagePercentage = 0;
                        dto.RemainingUsage = dto.MaxUsageCount;
                        dto.IsUsageLimitReached = false;
                        dto.UsageSummary = new EventUsageSummaryDTO();
                    }

                    //  Enhanced EventRules data with detailed analysis
                    if (bannerEvent.Rules?.Any() == true)
                    {
                        dto.Rules = bannerEvent.Rules.OrderBy(r => r.Priority).Select(rule => new EventRuleDTO
                        {
                            Id = rule.Id,
                            Type = rule.Type,
                            TargetValue = rule.TargetValue,
                            Conditions = rule.Conditions ?? string.Empty,
                            DiscountType = rule.DiscountType,
                            DiscountValue = rule.DiscountValue,
                            MaxDiscount = rule.MaxDiscount ?? 0,
                            MinOrderValue = rule.MinOrderValue ?? 0,
                            Priority = rule.Priority,
                            RuleDescription = GenerateRuleDescription(rule),
                            IsRestrictive = IsRuleRestrictive(rule),
                            TargetAudience = GetTargetAudience(rule)
                        }).ToList();
                    }
                    else
                    {
                        dto.Rules = new List<EventRuleDTO>();
                    }

                    // Enhanced EventProducts data with calculated pricing
                    if (bannerEvent.EventProducts?.Any() == true)
                    {
                        var eventProducts = new List<EventProductDTO>();

                        foreach (var ep in bannerEvent.EventProducts)
                        {
                            var eventProduct = new EventProductDTO
                            {
                                Id = ep.Id,
                                BannerEventId = ep.BannerEventId,
                                ProductId = ep.ProductId,
                                ProductName = ep.Product?.Name ?? "Unknown Product",
                                SpecificDiscount = ep.SpecificDiscount ?? 0,
                                AddedAt = ep.AddedAt != default ? ep.AddedAt : bannerEvent.CreatedAt,
                                ProductMarketPrice = ep.Product?.MarketPrice ?? 0,
                                ProductImageUrl = ep.Product?.Images?.FirstOrDefault()?.ImageUrl ?? string.Empty,
                                CategoryName = ep.Product?.Category?.Name ?? "Uncategorized",
                                //  Calculate actual discounted price
                                CalculatedDiscountPrice = CalculateDiscountedPrice(
                                    ep.Product?.MarketPrice ?? 0,
                                    bannerEvent,
                                    ep.SpecificDiscount),
                                FormattedSpecificDiscount = ep.SpecificDiscount.HasValue && ep.SpecificDiscount > 0
                                    ? $"Rs.{ep.SpecificDiscount:F2} OFF"
                                    : GetEventDiscountDescription(bannerEvent),
                                HasSpecificDiscount = ep.SpecificDiscount.HasValue && ep.SpecificDiscount > 0
                            };

                            eventProducts.Add(eventProduct);
                        }

                        dto.EventProducts = eventProducts;
                        dto.ProductIds = bannerEvent.EventProducts.Select(ep => ep.ProductId).ToList();
                    }
                    else
                    {
                        dto.EventProducts = new List<EventProductDTO>();
                        dto.ProductIds = new List<int>();
                    }

                    //  Set computed properties
                    dto.TotalProductsCount = bannerEvent.EventProducts?.Count ?? 0;
                    dto.TotalRulesCount = bannerEvent.Rules?.Count ?? 0;

                    bannerEventDTOs.Add(dto);
                }

                // Create paged result
                var pagedResult = new PagedResult<BannerEventSpecialDTO>
                {
                    Data = bannerEventDTOs,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };

                _logger.LogInformation("Retrieved {Count} banner events with analytics data (Page {PageNumber}/{TotalPages})",
                    bannerEventDTOs.Count, request.PageNumber, pagedResult.TotalPages);

                return Result<PagedResult<BannerEventSpecialDTO>>.Success(
                    pagedResult,
                    $"Banner events retrieved successfully with analytics. Page {request.PageNumber} of {pagedResult.TotalPages}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve banner events for page {PageNumber}", request.PageNumber);
                return Result<PagedResult<BannerEventSpecialDTO>>.Failure($"Failed to retrieve banner events: {ex.Message}");
            }
        }
        
        //  Helper methods for enhanced data processing
        private decimal CalculateConversionRate(int totalUsages, int maxUsage)
        {
            if (maxUsage <= 0) return 0;
            return Math.Round((decimal)totalUsages / maxUsage * 100, 2);
        }

        private async Task<DateTime?> GetLastUsageDate(int eventId, CancellationToken cancellationToken)
        {
            var lastUsage = await _unitOfWork.EventUsages.GetAllAsync(
                predicate: u => u.BannerEventId == eventId && !u.IsDeleted,
                orderBy: q => q.OrderByDescending(u => u.UsedAt),
                take: 1,
                cancellationToken: cancellationToken);

            return lastUsage.FirstOrDefault()?.UsedAt;
        }

        private async Task<string> GetMostActiveDay(int eventId, CancellationToken cancellationToken)
        {
            var usages = await _unitOfWork.EventUsages.GetAllAsync(
                predicate: u => u.BannerEventId == eventId && !u.IsDeleted,
                cancellationToken: cancellationToken);

            if (!usages.Any()) return "No Usage";

            var mostActiveDay = usages
                .GroupBy(u => u.UsedAt.Date)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return mostActiveDay?.Key.ToString("yyyy-MM-dd") ?? "N/A";
        }

        private decimal CalculateDiscountedPrice(decimal originalPrice, BannerEventSpecial bannerEvent, decimal? specificDiscount)
        {
            if (specificDiscount.HasValue && specificDiscount > 0)
            {
                return Math.Max(0, originalPrice - specificDiscount.Value);
            }

            // Use event discount
            var discount = bannerEvent.PromotionType == Domain.Enums.BannerEventSpecial.PromotionType.Percentage
                ? originalPrice * (bannerEvent.DiscountValue / 100)
                : bannerEvent.DiscountValue;

            // Apply maximum discount cap
            if (bannerEvent.MaxDiscountAmount.HasValue)
            {
                discount = Math.Min(discount, bannerEvent.MaxDiscountAmount.Value);
            }

            return Math.Max(0, originalPrice - discount);
        }

        private string GetEventDiscountDescription(BannerEventSpecial bannerEvent)
        {
            return bannerEvent.PromotionType switch
            {
                Domain.Enums.BannerEventSpecial.PromotionType.Percentage => $"{bannerEvent.DiscountValue}% OFF",
                Domain.Enums.BannerEventSpecial.PromotionType.FixedAmount => $"Rs.{bannerEvent.DiscountValue} OFF",
                Domain.Enums.BannerEventSpecial.PromotionType.BuyOneGetOne => "Buy One Get One Free",
                Domain.Enums.BannerEventSpecial.PromotionType.FreeShipping => "Free Shipping",
                _ => "Special Event Discount"
            };
        }

        private string GenerateRuleDescription(EventRule rule)
        {
            var baseDescription = rule.Type switch
            {
                Domain.Enums.BannerEventSpecial.RuleType.Category => $"Must include products from categories: {rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.SubCategory => $"Must include products from sub-categories: {rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.SubSubCategory => $"Must include products from sub-sub-categories: {rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.Product => $"Must include specific products: {rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.PriceRange => $"Order total must be within: Rs.{rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.PaymentMethod => $"Must use payment methods: {rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.Geography => $"Available only in: {rule.TargetValue}",
                Domain.Enums.BannerEventSpecial.RuleType.All => "No restrictions - applies to all customers",
                _ => $"Custom requirement: {rule.TargetValue}"
            };

            //  Smart detection for price range patterns
            if (rule.TargetValue?.Contains('-') == true && 
                rule.TargetValue.Split('-').Length == 2 &&
                decimal.TryParse(rule.TargetValue.Split('-')[0], out _) &&
                decimal.TryParse(rule.TargetValue.Split('-')[1], out _))
            {
                baseDescription = $"Order total must be within: Rs.{rule.TargetValue}";
            }

            //  Add minimum order value info if present
            if (rule.MinOrderValue.HasValue && rule.MinOrderValue > 0)
            {
                baseDescription += $" (Min order: Rs.{rule.MinOrderValue:F2})";
            }

            return baseDescription;
        }


        private bool IsRuleRestrictive(EventRule rule)
        {
            return rule.MinOrderValue > 0 || 
                   !string.IsNullOrEmpty(rule.Conditions) ||
                   rule.Type != Domain.Enums.BannerEventSpecial.RuleType.Category;
        }

        private string GetTargetAudience(EventRule rule)
        {
            return rule.Type switch
            {
                Domain.Enums.BannerEventSpecial.RuleType.PaymentMethod => "Payment Method Users",
                Domain.Enums.BannerEventSpecial.RuleType.PriceRange => "High Value Customers",
                Domain.Enums.BannerEventSpecial.RuleType.Geography => "Location-Based Customers",
                Domain.Enums.BannerEventSpecial.RuleType.Category => "Category Shoppers",
                Domain.Enums.BannerEventSpecial.RuleType.Product => "Product-Specific Buyers",
                Domain.Enums.BannerEventSpecial.RuleType.All => "All Customers",
                _ => "Targeted Customers"
            };
        }

    }
}