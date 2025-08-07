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
        string? SortBy = null, // "price","name","rating","discount"
        bool IncludeDeleted = false,
        bool IsAdminRequest = false  
    ) : IRequest<Result<CategoryWithProductsDTO>>;

    public class GetAllProductsByCategoryIdHandler : IRequestHandler<GetAllProductsByCategoryId, Result<CategoryWithProductsDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<GetAllProductsByCategoryIdHandler> _logger; 

        public GetAllProductsByCategoryIdHandler(
            ICategoryRepository categoryRepository,
            IProductPricingService pricingService,
            IHybridCacheService cacheService,
            ILogger<GetAllProductsByCategoryIdHandler> logger)
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
                _logger.LogInformation("Fetching products for Category {CategoryId} - Page {PageNumber}, Size {PageSize}, OnSaleOnly {OnSaleOnly}", 
                    request.CategoryId, request.PageNumber, request.PageSize, request.OnSaleOnly);

                // Check if Category exists with deletion filter
                var categories = await _categoryRepository.GetAllAsync(
                    predicate: c => c.Id == request.CategoryId && (request.IncludeDeleted || !c.IsDeleted),
                    take: 1,
                    cancellationToken: cancellationToken);

                var category = categories.FirstOrDefault();
                if (category == null)
                {
                    return Result<CategoryWithProductsDTO>.Failure("CategoryId is not found");
                }

                //  Fetch products by CategoryId with pagination and deletion filter
                var products = await _categoryRepository.GetProductsByCategoryIdAsync(
                    request.CategoryId,
                    (request.PageNumber - 1) * request.PageSize,
                    request.PageSize,
                    !request.IncludeDeleted, // onlyActive parameter
                    cancellationToken);

                if (!products.Any())
                {
                    _logger.LogInformation("No products found for Category {CategoryId}", request.CategoryId);
                    
                    return Result<CategoryWithProductsDTO>.Success(new CategoryWithProductsDTO
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Slug = category.Slug,
                        Description = category.Description,
                        Products = new List<ProductDTO>(),
                        TotalProducts = 0,
                        ProductsOnSale = 0,
                        AveragePrice = 0,
                        MinPrice = 0,
                        MaxPrice = 0,
                        TotalSavings = 0
                    }, "Category found but no products available");
                }

                // Convert to DTOs
                var productDTOs = products.Select(p => p.ToDTO()).ToList();

                //  Apply dynamic pricing to active products only
                var activeProducts = productDTOs.Where(p => !p.IsDeleted).ToList();
                if (activeProducts.Any())
                {
                    await activeProducts.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                }

                // Handle deleted products for admin
                if (request.IsAdminRequest)
                {
                    var deletedProducts = productDTOs.Where(p => p.IsDeleted).ToList();
                    foreach (var deletedProduct in deletedProducts)
                    {
                        deletedProduct.Pricing = CreateBasicPricingForDeletedProduct(deletedProduct);
                    }
                }

                //  Filter by sale status if requested (only for active products)
                if (request.OnSaleOnly)
                {
                    productDTOs = productDTOs.Where(p => !p.IsDeleted && p.IsOnSale).ToList();
                }

                // Apply sorting with null safety
                productDTOs = ApplySorting(productDTOs, request.SortBy);

                //  Calculate category-level statistics with null safety
                var categoryWithProductsDto = new CategoryWithProductsDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                    Description = category.Description,
                    Products = productDTOs,

                    // Pricing statistics (only for active products)
                    TotalProducts = request.IsAdminRequest ? productDTOs.Count : activeProducts.Count,
                    ProductsOnSale = activeProducts.Count(p => p.IsOnSale),
                    AveragePrice = activeProducts.Any() ? Math.Round(activeProducts.Average(p => p.Pricing?.EffectivePrice ?? p.MarketPrice), 2) : 0,
                    MinPrice = activeProducts.Any() ? activeProducts.Min(p => p.Pricing?.EffectivePrice ?? p.MarketPrice) : 0,
                    MaxPrice = activeProducts.Any() ? activeProducts.Max(p => p.Pricing?.EffectivePrice ?? p.MarketPrice) : 0,
                    
                    // Proper calculation with parentheses and null safety
                    TotalSavings = activeProducts.Sum(p => (p.Pricing?.ProductDiscountAmount ?? 0) + (p.Pricing?.EventDiscountAmount ?? 0))
                };
                
                _logger.LogInformation("Retrieved category {CategoryId} with {ProductCount} products ({ActiveCount} active, {DeletedCount} deleted), {OnSaleCount} on sale, total savings: Rs.{TotalSavings}",
                    request.CategoryId, productDTOs.Count, activeProducts.Count, productDTOs.Count - activeProducts.Count, categoryWithProductsDto.ProductsOnSale, categoryWithProductsDto.TotalSavings);

                var message = request.IsAdminRequest
                    ? $"Category retrieved with {categoryWithProductsDto.ProductsOnSale} items on sale (Admin view - includes deleted). Total potential savings: Rs.{categoryWithProductsDto.TotalSavings:F2}"
                    : $"Category retrieved with {categoryWithProductsDto.ProductsOnSale} items on sale. Total potential savings: Rs.{categoryWithProductsDto.TotalSavings:F2}";

                return Result<CategoryWithProductsDTO>.Success(categoryWithProductsDto, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {CategoryId} with dynamic pricing", request.CategoryId);
                return Result<CategoryWithProductsDTO>.Failure($"Failed to retrieve category products: {ex.Message}");
            }
        }

        private static List<ProductDTO> ApplySorting(List<ProductDTO> products, string? sortBy)
        {
            return sortBy?.ToLower() switch
            {
                "price" => products.OrderBy(p => p.Pricing?.EffectivePrice ?? p.MarketPrice).ToList(),
                "name" => products.OrderBy(p => p.Name).ToList(),
                "rating" => products.OrderByDescending(p => p.Rating).ToList(),
                "discount" => products.OrderByDescending(p => p.Pricing?.TotalDiscountPercentage ?? 0).ToList(),
                _ => products.OrderByDescending(p => p.Id).ToList()
            };
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