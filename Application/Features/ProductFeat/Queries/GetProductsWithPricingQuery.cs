using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.ProductFeat.Queries
{
    public record GetProductsWithPricingQuery(
        int? CategoryId = null,
        int? UserId = null,
        int PageNumber = 1,
        int PageSize = 20
        ) : IRequest<Result<IEnumerable<ProductWithPricingDTO>>>;

    public class GetProductsWithPricingQueryHandler : IRequestHandler<GetProductsWithPricingQuery, Result<IEnumerable<ProductWithPricingDTO>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _pricingService;

        public GetProductsWithPricingQueryHandler(
            IProductRepository productRepository,
            IProductPricingService pricingService
            )
        {
            _productRepository = productRepository;
            _pricingService = pricingService;
        }
        public async Task<Result<IEnumerable<ProductWithPricingDTO>>> Handle(GetProductsWithPricingQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var products = await _productRepository.GetAllAsync(
                    predicate: p => !p.IsDeleted &&
                    (request.CategoryId == null || p.CategoryId == request.CategoryId),
                    orderBy: q => q.OrderBy(p => p.Name),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties: "Images,Category",
                    cancellationToken: cancellationToken
                    );

                var productDTOs = new List<ProductWithPricingDTO>();

                foreach (var product in products)
                {
                    // Get effective pricing for each product
                    var priceInfo = await _pricingService.GetEffectivePriceAsync(product, request.UserId);

                    var productDTO = new ProductWithPricingDTO
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Sku = product.Sku,
                        MarketPrice = product.MarketPrice,
                        DiscountPrice = product.DiscountPrice,
                        EffectivePrice = priceInfo.EffectivePrice,
                        DiscountAmount = priceInfo.DiscountAmount,
                        DiscountPercentage = priceInfo.DiscountPercentage,
                        HasActivePromotion = priceInfo.HasDiscount,
                        ActiveEventId = priceInfo.AppliedEventId,
                        ActiveEventName = priceInfo.AppliedEventName,
                        EventTagLine = priceInfo.EventTagLine,
                        StockQuantity = product.StockQuantity,
                        Rating = product.Rating,
                        Reviews = product.Reviews,
                        Images = product.Images?.Select(i => i.ToDTO()).ToList() ?? new()
                    };

                    productDTOs.Add(productDTO);
                }

                return Result<IEnumerable<ProductWithPricingDTO>>.Success(
                    productDTOs,
                    $"Retrieved {productDTOs.Count} products with pricing information");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<ProductWithPricingDTO>>.Failure($"Failed to get products: {ex.Message}");
            }
        }
    }

}
