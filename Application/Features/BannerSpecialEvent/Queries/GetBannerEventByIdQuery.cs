using Application.Common;
using Application.Dto;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Queries
{
    public record GetBannerEventByIdQuery(int Id) : IRequest<Result<BannerEventSpecialDTO>>;

    public class GetBannerEventByIdQueryHandler : IRequestHandler<GetBannerEventByIdQuery, Result<BannerEventSpecialDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetBannerEventByIdQueryHandler> _logger;

        public GetBannerEventByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetBannerEventByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<BannerEventSpecialDTO>> Handle(GetBannerEventByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetAsync(
                     predicate: e => e.Id == request.Id,
                     includeProperties: "Images,Rules,EventProducts,EventProducts.Product,EventProducts.Product.Category,EventProducts.Product.Images",
                     includeDeleted: true,
                     cancellationToken: cancellationToken);

                if (bannerEvent == null)
                {
                    _logger.LogWarning("Banner event with ID {EventId} not found", request.Id);
                    return Result<BannerEventSpecialDTO>.Failure("Banner event not found");
                }

                // Initialize DTO with base properties
                var dto = bannerEvent.ToDTO();

                //  Initialize all collections
                dto.Images = new List<BannerImageDTO>();
                dto.Rules = new List<EventRuleDTO>();
                dto.EventProducts = new List<EventProductDTO>();
                dto.ProductIds = new List<int>();

                // Map Banner Images
                if (bannerEvent.Images?.Any() == true)
                {
                    dto.Images = bannerEvent.Images.Select(image => new BannerImageDTO
                    {
                        Id = image.Id,
                        ImageUrl = image.ImageUrl,

                    }).ToList();

                    _logger.LogDebug("Mapped {Count} images for banner event {EventId}", dto.Images.Count, request.Id);
                }

                // Map Event Rules with detailed logging
                if (bannerEvent.Rules?.Any() == true)
                {
                    dto.Rules = bannerEvent.Rules.Select(rule => new EventRuleDTO
                    {
                        Id = rule.Id,
                        Type = rule.Type,
                        TargetValue = rule.TargetValue ?? string.Empty,
                        Conditions = rule.Conditions,
                        DiscountType = rule.DiscountType,
                        DiscountValue = rule.DiscountValue,
                        MaxDiscount = rule.MaxDiscount,
                        MinOrderValue = rule.MinOrderValue,
                        Priority = rule.Priority
                    }).OrderBy(r => r.Priority).ToList();

                    _logger.LogDebug("Mapped {Count} rules for banner event {EventId}", dto.Rules.Count, request.Id);
                }
                else
                {
                    _logger.LogDebug("No rules found for banner event {EventId}", request.Id);
                }

                // Map Event Products with enhanced product details
                if (bannerEvent.EventProducts?.Any() == true)
                {
                    dto.EventProducts = bannerEvent.EventProducts.Select(ep => new EventProductDTO
                    {
                        Id = ep.Id,
                        BannerEventId = ep.BannerEventId,
                        ProductId = ep.ProductId,
                        ProductName = ep.Product?.Name ?? "Unknown Product",
                        SpecificDiscount = ep.SpecificDiscount ?? 0,
                        AddedAt = ep.AddedAt,
                        // Enhanced product information
                        ProductMarketPrice = ep.Product?.MarketPrice ?? 0,
                        ProductImageUrl = ep.Product?.Images?.FirstOrDefault(img => img.IsMain)?.ImageUrl ??
                                         ep.Product?.Images?.FirstOrDefault()?.ImageUrl,
                        CategoryName = ep.Product?.Category?.Name
                    }).OrderBy(ep => ep.ProductName).ToList();

                    // Set ProductIds for backward compatibility
                    dto.ProductIds = bannerEvent.EventProducts
                        .Select(ep => ep.ProductId)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToList();

                    _logger.LogDebug("Mapped {Count} event products for banner event {EventId}", dto.EventProducts.Count, request.Id);
                }
                else
                {
                    _logger.LogDebug("No event products found for banner event {EventId}", request.Id);
                }

                // Set statistics with null safety
                dto.TotalProductsCount = bannerEvent.EventProducts?.Count ?? 0;
                dto.TotalRulesCount = bannerEvent.Rules?.Count ?? 0;

                // Log summary
                _logger.LogInformation(
                    "Successfully retrieved banner event {EventId} - {EventName} with {RulesCount} rules, {ProductsCount} products, and {ImagesCount} images",
                    request.Id,
                    bannerEvent.Name,
                    dto.TotalRulesCount,
                    dto.TotalProductsCount,
                    dto.Images.Count);

                return Result<BannerEventSpecialDTO>.Success(dto,
                    $"Banner event retrieved successfully with {dto.TotalRulesCount} rules and {dto.TotalProductsCount} products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve banner event {EventId}", request.Id);
                return Result<BannerEventSpecialDTO>.Failure($"Failed to retrieve banner event: {ex.Message}");
            }
        }
    }
}
