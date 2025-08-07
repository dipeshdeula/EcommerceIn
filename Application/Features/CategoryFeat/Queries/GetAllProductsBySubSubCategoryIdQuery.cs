using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllProductsBySubSubCategoryIdQuery(
        int SubSubCategoryId,
        int PageNumber,
        int PageSize,
        int? UserId = null,
        bool IncludeDeleted = false,
        bool IsAdminRequest = false
        ) : IRequest<Result<CategoryWithProductsDTO>>;

    public class GetAllProductsBySubSubCategoryIdQueryHandler : IRequestHandler<GetAllProductsBySubSubCategoryIdQuery, Result<CategoryWithProductsDTO>>
    {

        private readonly ISubSubCategoryRepository _subSubCategoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<GetAllProductsBySubSubCategoryIdQueryHandler> _logger;

        public GetAllProductsBySubSubCategoryIdQueryHandler(

            ISubSubCategoryRepository subSubCategoryRepository,
            IProductRepository productRepository,
            IHybridCacheService cacheService,
            IProductPricingService pricingService,
            ILogger<GetAllProductsBySubSubCategoryIdQueryHandler> logger
            )
        {
            _subSubCategoryRepository = subSubCategoryRepository;
            _productRepository = productRepository;
            _pricingService = pricingService;
            _cacheService = cacheService;
            _logger = logger;

        }
        public async Task<Result<CategoryWithProductsDTO>> Handle(GetAllProductsBySubSubCategoryIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching products for SubSubCategory {SubSubCategoryId} - Page {PageNumber}, Size {PageSize}",
                    request.SubSubCategoryId, request.PageNumber, request.PageSize);

                //  Check if SubSubCategory exists with deletion filter
                var subSubCategories = await _subSubCategoryRepository.GetAllAsync(
                    predicate: s => s.Id == request.SubSubCategoryId && (request.IncludeDeleted || !s.IsDeleted),
                    take: 1,
                    cancellationToken: cancellationToken);

                var subSubCategory = subSubCategories.FirstOrDefault();
                if (subSubCategory == null)
                {
                    return Result<CategoryWithProductsDTO>.Failure($"SubSubCategory Id: {request.SubSubCategoryId} not found");
                }

                // Get products with deletion filter and pagination
                var getProducts = await _subSubCategoryRepository.GetProductsBySubSubCategoryIdAsync(
                    subSubCategory.Id,
                    (request.PageNumber - 1) * request.PageSize, 
                    request.PageSize,
                    !request.IncludeDeleted, // onlyActive parameter
                    cancellationToken);

                if (!getProducts.Any())
                {
                    _logger.LogInformation("No products found for SubSubCategory {SubSubCategoryId}", request.SubSubCategoryId);

                    return Result<CategoryWithProductsDTO>.Success(new CategoryWithProductsDTO
                    {
                        Id = subSubCategory.Id,
                        Name = subSubCategory.Name,
                        Slug = subSubCategory.Slug,
                        Description = subSubCategory.Description,
                        Products = new List<ProductDTO>(),
                        TotalProducts = 0,
                        ProductsOnSale = 0,
                        AveragePrice = 0,
                        MinPrice = 0,
                        MaxPrice = 0,
                        TotalSavings = 0
                    }, "SubSubCategory found but no products available");
                }

                // Convert to DTOs
                var productDTOs = getProducts.Select(p => p.ToDTO()).ToList();

                // Apply dynamic pricing to active products only with null safety
                var activeProducts = productDTOs.Where(p => !p.IsDeleted).ToList();
                if (activeProducts.Any())
                {
                    await activeProducts.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                }

                // deleted products for admin
                if (request.IsAdminRequest)
                {
                    var deletedProducts = productDTOs.Where(p => p.IsDeleted).ToList();
                    foreach (var deletedProduct in deletedProducts)
                    {
                        deletedProduct.Pricing = CreateBasicPricingForDeletedProduct(deletedProduct);
                    }
                }

                //  Safe pricing calculations with null checks
                var categoryWithProductsDto = new CategoryWithProductsDTO
                {
                    Id = subSubCategory.Id,
                    Name = subSubCategory.Name,
                    Slug = subSubCategory.Slug,
                    Description = subSubCategory.Description,
                    Products = productDTOs,

                    // Pricing statistics (only for active products)
                    TotalProducts = request.IsAdminRequest ? productDTOs.Count : activeProducts.Count,
                    ProductsOnSale = activeProducts.Count(p => p.IsOnSale),
                    AveragePrice = activeProducts.Any() ? Math.Round(activeProducts.Average(p => p.Pricing?.EffectivePrice ?? p.MarketPrice), 2) : 0,
                    MinPrice = activeProducts.Any() ? activeProducts.Min(p => p.Pricing?.EffectivePrice ?? p.MarketPrice) : 0,
                    MaxPrice = activeProducts.Any() ? activeProducts.Max(p => p.Pricing?.EffectivePrice ?? p.MarketPrice) : 0,
                    TotalSavings = activeProducts.Sum(p => (p.Pricing?.ProductDiscountAmount ?? 0) + (p.Pricing?.EventDiscountAmount ?? 0))
                };

                _logger.LogInformation("Retrieved SubSubCategory {SubSubCategoryId} with {ProductCount} products ({ActiveCount} active), {OnSaleCount} on sale, total savings: Rs.{TotalSavings}",
                    request.SubSubCategoryId, productDTOs.Count, activeProducts.Count, categoryWithProductsDto.ProductsOnSale, categoryWithProductsDto.TotalSavings);

                var message = request.IsAdminRequest
                    ? $"SubSubCategory retrieved with {categoryWithProductsDto.ProductsOnSale} items on sale (Admin view). Total potential savings: Rs.{categoryWithProductsDto.TotalSavings:F2}"
                    : $"SubSubCategory retrieved with {categoryWithProductsDto.ProductsOnSale} items on sale. Total potential savings: Rs.{categoryWithProductsDto.TotalSavings:F2}";

                return Result<CategoryWithProductsDTO>.Success(categoryWithProductsDto, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for SubSubCategory {SubSubCategoryId}", request.SubSubCategoryId);
                return Result<CategoryWithProductsDTO>.Failure($"Failed to retrieve SubSubCategory products: {ex.Message}");
            }

        }
        
        private static ProductPricingDTO CreateBasicPricingForDeletedProduct(ProductDTO product)
        {
            return new ProductPricingDTO
            {
                ProductId = product.Id,
                ProductName = product.Name,
                OriginalPrice = product.MarketPrice,
                BasePrice = 0,
                EffectivePrice = 0,
                ProductDiscountAmount = 0,
                EventDiscountAmount = 0,
                TotalDiscountAmount = 0,
                TotalDiscountPercentage = 0,
                HasProductDiscount = false,
                HasEventDiscount = false,
                HasAnyDiscount = false,
                IsOnSale = false,
                FormattedOriginalPrice = $"Rs.{product.MarketPrice:F2}",
                FormattedEffectivePrice = "Rs.0.00 (DELETED)",
                FormattedSavings = "",
                IsPriceStable = false,
                CalculatedAt = DateTime.UtcNow
            };
        }
    }

}
