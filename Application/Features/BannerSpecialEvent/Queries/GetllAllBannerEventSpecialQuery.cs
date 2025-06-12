////filepath: e:\EcomerceDeployPostgres\EcommerceBackendAPI\Application\Features\BannerSpecialEvent\Queries\GetAllBannerEventSpecialQuery.cs
using Application.Common;
using Application.Common.Models;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Domain.Entities;
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

        public GetAllBannerEventSpecialQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetAllBannerEventSpecialQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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
                    includeProperties: "Images,Rules,EventProducts,EventProducts.Product",
                    includeDeleted: request.IncludeDeleted);

                //  Convert to DTOs with related data
                var bannerEventDTOs = bannerEvents.Select(bannerEvent =>
                {
                    var dto = bannerEvent.ToDTO();

                    // Include EventRules data
                    if (bannerEvent.Rules?.Any() == true)
                    {
                        dto.Rules = bannerEvent.Rules.Select(rule => new EventRuleDTO
                        {
                            Id = rule.Id,
                            Type = rule.Type,
                            TargetValue = rule.TargetValue,
                            Conditions = rule.Conditions,
                            DiscountType = rule.DiscountType,
                            DiscountValue = rule.DiscountValue,
                            MaxDiscount = rule.MaxDiscount,
                            MinOrderValue = rule.MinOrderValue,
                            Priority = rule.Priority
                        }).ToList();
                    }

                    // Include EventProducts data with product details
                    if (bannerEvent.EventProducts?.Any() == true)
                    {
                        dto.EventProducts = bannerEvent.EventProducts.Select(ep => new EventProductDTO
                        {
                            Id = ep.Id,
                            BannerEventId = ep.BannerEventId,
                            ProductId = ep.ProductId,
                            ProductName = ep.Product?.Name ?? "Unknown Product",
                            SpecificDiscount = ep.SpecificDiscount,
                            AddedAt = ep.AddedAt,
                            //  Additional product info if available
                            ProductMarketPrice = ep.Product?.MarketPrice ?? 0,
                            ProductImageUrl = ep.Product?.Images?.FirstOrDefault()?.ImageUrl,
                            CategoryName = ep.Product?.Category?.Name
                        }).ToList();

                        // Also set ProductIds for backward compatibility
                        dto.ProductIds = bannerEvent.EventProducts.Select(ep => ep.ProductId).ToList();
                    }

                    // Now these are writable properties
                    dto.TotalProductsCount = bannerEvent.EventProducts?.Count ?? 0;
                    dto.TotalRulesCount = bannerEvent.Rules?.Count ?? 0;

                    return dto;
                }).ToList();

                // Create paged result
                var pagedResult = new PagedResult<BannerEventSpecialDTO>
                {
                    Data = bannerEventDTOs,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                };

                _logger.LogInformation($"Retrieved {pagedResult.TotalCount} banner events (Page {pagedResult.PageNumber}/{pagedResult.TotalPages})",
                    bannerEventDTOs.Count, request.PageNumber, pagedResult.TotalPages);

                return Result<PagedResult<BannerEventSpecialDTO>>.Success(
                    pagedResult,
                    $"Banner events retrieved successfully. Page {request.PageNumber} of {pagedResult.TotalPages}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve banner events for page {PageNumber}", request.PageNumber);
                return Result<PagedResult<BannerEventSpecialDTO>>.Failure($"Failed to retrieve banner events: {ex.Message}");
            }
        }
    }
}