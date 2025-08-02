using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Utilities;
using System.Linq.Expressions;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllProductsByCategoryId(
         int CategoryId,
        int PageNumber,
        int PageSize,
        int? UserId = null,
        bool OnSaleOnly = false,
        string? SortBy = null // "price","name","rating","discount"

        ) : IRequest<Result<CategoryWithProductsDTO>>;

    public class GetAllProductsByCategoryIdHandler : IRequestHandler<GetAllProductsByCategoryId, Result<CategoryWithProductsDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private ILogger<GetAllProductsByCategoryId> _logger;
        public GetAllProductsByCategoryIdHandler(
            ICategoryRepository categoryRepository,
            IProductPricingService pricingService,
            IHybridCacheService cacheService,
            ILogger<GetAllProductsByCategoryId> logger
            )
        {
            _categoryRepository = categoryRepository;
            _pricingService = pricingService;
            _cacheService = cacheService;
            _logger = logger;

        }
        public async Task<Result<CategoryWithProductsDTO>> Handle(GetAllProductsByCategoryId request, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryRepository.FindByIdAsync(request.CategoryId);
                if (category == null)
                {
                    return Result<CategoryWithProductsDTO>.Failure("CategoryId is not found");
                }

                // Fetch products by CategoryId with pagination
                var products = await _categoryRepository.GetProductsByCategoryIdAsync(
                    request.CategoryId,
                    (request.PageNumber - 1) * request.PageSize,
                    request.PageSize
                    );

                // Convert to DTOs
                var productDTOs = products.Select(p => p.ToDTO()).ToList();

                // Apply dynamic pricing to all products
                await productDTOs.ApplyPricingAsync(_pricingService, _cacheService,request.UserId, cancellationToken);

                // Filter by sale status if requested
                if (request.OnSaleOnly)
                {
                    productDTOs = productDTOs.Where(p => p.IsOnSale).ToList();
                }

                // Apply sorting
                productDTOs = ApplySorting(productDTOs, request.SortBy);
                

                // Calculate category-level statistics
                var categoryWithProductsDto = new CategoryWithProductsDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    Products = productDTOs,

                    //  Pricing statistics
                    TotalProducts = productDTOs.Count,
                    ProductsOnSale = productDTOs.Count(p => p.IsOnSale),
                    AveragePrice = productDTOs.Any() ? productDTOs.Average(p => p.Pricing?.EffectivePrice ?? 0) : 0,
                    MinPrice = productDTOs.Any() ? productDTOs.Min(p => p.Pricing?.EffectivePrice ?? 0) : 0,
                    MaxPrice = productDTOs.Any() ? productDTOs.Max(p => p.Pricing? .EffectivePrice ?? 0) : 0,
                    TotalSavings = productDTOs.Sum(p => p.Pricing?.ProductDiscountAmount ?? 0 + p.Pricing?.EventDiscountAmount ?? 0)
                };
                
                _logger.LogInformation("Retrieved category {CategoryId} with {ProductCount} products, {OnSaleCount} on sale, total savings: Rs.{TotalSavings}",
                    request.CategoryId, productDTOs.Count, categoryWithProductsDto.ProductsOnSale, categoryWithProductsDto.TotalSavings);

                return Result<CategoryWithProductsDTO>.Success(categoryWithProductsDto,
                    $"Category retrieved with {categoryWithProductsDto.ProductsOnSale} items on sale. Total potential savings: Rs.{categoryWithProductsDto.TotalSavings:F2}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {CategoryId} with dynamic pricing", request.CategoryId);
                return Result<CategoryWithProductsDTO>.Failure($"Failed to retrieve category products: {ex.Message}");

            }
        }

        private List<ProductDTO> ApplySorting(List<ProductDTO> products, string? sortBy)
        {
           return sortBy?.ToLower() switch
           {
               "price" => products.OrderBy(p => p.Pricing!.EffectivePrice).ToList(),
               "name" => products.OrderBy(p => p.Name).ToList(),
               "rating" => products.OrderByDescending(p => p.Rating).ToList(),
               "discount" => products.OrderByDescending(p => p.Pricing!.TotalDiscountPercentage).ToList(),
               _ => products.OrderByDescending(p => p.Id).ToList()
           };
        }

        
    }

}
