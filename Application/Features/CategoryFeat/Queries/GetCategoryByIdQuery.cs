using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetCategoryByIdQuery(
        int CategoryId,
        int PageNumber = 1,
        int PageSize = 10,
        int? UserId = null,
        bool IncludeProducts = true,
        bool IncludeDeleted = false
    ) : IRequest<Result<CategoryDTO>>;

    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

        public GetCategoryByIdQueryHandler(
            ICategoryRepository categoryRepository,
            IProductPricingService pricingService,
            IHybridCacheService cacheService,
            ILogger<GetCategoryByIdQueryHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _pricingService = pricingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<CategoryDTO>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching category with ID: {CategoryId}", request.CategoryId);

                // Build include properties based on requirements
                var includeProperties = request.IncludeProducts 
                    ? "SubCategories,SubCategories.SubSubCategories,SubCategories.SubSubCategories.Products,SubCategories.SubSubCategories.Products.Images"
                    : "SubCategories,SubCategories.SubSubCategories";

                // Get category with all navigation properties
                var category = await _categoryRepository.GetAsync(
                    predicate: c => c.Id == request.CategoryId && 
                                   (request.IncludeDeleted || !c.IsDeleted),
                    includeProperties: includeProperties,
                    cancellationToken: cancellationToken);

                if (category == null)
                {
                    _logger.LogWarning("Category with ID {CategoryId} not found", request.CategoryId);
                    return Result<CategoryDTO>.Failure($"Category with ID {request.CategoryId} not found");
                }

                // Convert to DTO
                var categoryDTO = category.ToDTO();

                // Apply pagination to products if needed
                if (request.IncludeProducts)
                {
                    await ApplyPaginationAndPricing(categoryDTO, request, cancellationToken);
                }

                // Calculate category statistics
                await CalculateCategoryStatistics(categoryDTO, request.UserId, cancellationToken);

                _logger.LogInformation("Category {CategoryId} fetched successfully with {ProductCount} products", 
                    categoryDTO.Id, categoryDTO.ProductCount);

                return Result<CategoryDTO>.Success(
                    categoryDTO, 
                    $"Category '{categoryDTO.Name}' fetched successfully with {categoryDTO.ProductCount} products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category with ID: {CategoryId}", request.CategoryId);
                return Result<CategoryDTO>.Failure($"Failed to fetch category: {ex.Message}");
            }
        }

        private async Task ApplyPaginationAndPricing(CategoryDTO categoryDTO, GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Get all products from all subcategories
                var allProducts = GetAllProductsInCategory(categoryDTO);

                // Filter out deleted products for customers
                if (!request.IncludeDeleted)
                {
                    allProducts = allProducts.Where(p => !p.IsDeleted).ToList();
                }

                if (!allProducts.Any())
                {
                    return;
                }

                // Apply pricing to active products
                var activeProducts = allProducts.Where(p => !p.IsDeleted).ToList();
                if (activeProducts.Any())
                {
                    await activeProducts.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                }

                // Apply pagination to products in each subsubcategory
                foreach (var subCategory in categoryDTO.SubCategories)
                {
                    foreach (var subSubCategory in subCategory.SubSubCategories)
                    {
                        if (subSubCategory.Products.Any())
                        {
                            // Apply pagination
                            var paginatedProducts = subSubCategory.Products
                                .Skip((request.PageNumber - 1) * request.PageSize)
                                .Take(request.PageSize)
                                .ToList();

                            subSubCategory.Products = paginatedProducts;
                        }
                    }
                }

                _logger.LogDebug("Applied pagination and pricing to {ProductCount} products in category {CategoryId}",
                    activeProducts.Count, categoryDTO.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying pagination and pricing for category {CategoryId}", categoryDTO.Id);
            }
        }

        private async Task CalculateCategoryStatistics(CategoryDTO categoryDTO, int? userId, CancellationToken cancellationToken)
        {
            try
            {
                // Get all products in this category
                var allProducts = GetAllProductsInCategory(categoryDTO);

                if (!allProducts.Any())
                {
                    SetEmptyStatistics(categoryDTO);
                    return;
                }

                // Filter out deleted products for statistics
                var activeProducts = allProducts.Where(p => !p.IsDeleted).ToList();

                if (!activeProducts.Any())
                {
                    SetEmptyStatistics(categoryDTO);
                    return;
                }

                // Ensure pricing is applied (might already be done in ApplyPaginationAndPricing)
                if (activeProducts.Any(p => p.Pricing == null))
                {
                    await activeProducts.ApplyPricingAsync(_pricingService, _cacheService, userId, cancellationToken);
                }

                // Calculate statistics
                var prices = activeProducts.Select(p => p.CurrentPrice).Where(price => price > 0).ToList();
                var productsOnSale = activeProducts.Count(p => p.IsOnSale);

                categoryDTO.ProductCount = activeProducts.Count;
                categoryDTO.ProductsOnSale = productsOnSale;
                categoryDTO.HasProductsOnSale = productsOnSale > 0;
                categoryDTO.SalePercentage = activeProducts.Count > 0 
                    ? Math.Round((decimal)productsOnSale / activeProducts.Count * 100, 1) 
                    : 0;

                if (prices.Any())
                {
                    categoryDTO.MinPrice = prices.Min();
                    categoryDTO.MaxPrice = prices.Max();
                    categoryDTO.AveragePrice = Math.Round(prices.Average(), 2);
                    categoryDTO.PriceRange = categoryDTO.MinPrice == categoryDTO.MaxPrice 
                        ? $"Rs.{categoryDTO.MinPrice:F2}"
                        : $"Rs.{categoryDTO.MinPrice:F2} - Rs.{categoryDTO.MaxPrice:F2}";
                }
                else
                {
                    SetEmptyPriceStatistics(categoryDTO);
                }

                _logger.LogDebug("Category {CategoryName}: {ProductCount} products, {OnSaleCount} on sale, Average price: Rs.{AveragePrice:F2}",
                    categoryDTO.Name, categoryDTO.ProductCount, categoryDTO.ProductsOnSale, categoryDTO.AveragePrice);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating statistics for category {CategoryId}", categoryDTO.Id);
                SetEmptyStatistics(categoryDTO);
            }
        }

        private static List<ProductDTO> GetAllProductsInCategory(CategoryDTO category)
        {
            var products = new List<ProductDTO>();

            foreach (var subCategory in category.SubCategories)
            {
                foreach (var subSubCategory in subCategory.SubSubCategories)
                {
                    products.AddRange(subSubCategory.Products);
                }
            }

            return products;
        }

        private static void SetEmptyStatistics(CategoryDTO category)
        {
            category.ProductCount = 0;
            category.ProductsOnSale = 0;
            category.HasProductsOnSale = false;
            category.SalePercentage = 0;
            SetEmptyPriceStatistics(category);
        }

        private static void SetEmptyPriceStatistics(CategoryDTO category)
        {
            category.AveragePrice = 0;
            category.MinPrice = 0;
            category.MaxPrice = 0;
            category.PriceRange = "Rs.0.00";
        }
    }
}