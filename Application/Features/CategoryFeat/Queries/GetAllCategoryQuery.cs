using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Features.CategoryFeat.Queries
{
    public record GetAllCategoryQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        bool IncludeProducts = false,
        bool IncludeDeleted = false,
        int? UserId = null
    ) : IRequest<Result<IEnumerable<CategoryDTO>>>;

    public class GetAllCategoryQueryHandler : IRequestHandler<GetAllCategoryQuery, Result<IEnumerable<CategoryDTO>>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<GetAllCategoryQueryHandler> _logger;

        public GetAllCategoryQueryHandler(
            ICategoryRepository categoryRepository,
            IProductPricingService pricingService,
            IHybridCacheService cacheService,
            ILogger<GetAllCategoryQueryHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _pricingService = pricingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<CategoryDTO>>> Handle(GetAllCategoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Build predicate
                var predicate = BuildPredicate(request);

                // Get categories with full navigation properties
                var includeProperties = request.IncludeProducts 
                    ? "SubCategories,SubCategories.SubSubCategories,SubCategories.SubSubCategories.Products,SubCategories.SubSubCategories.Products.Images"
                    : "SubCategories,SubCategories.SubSubCategories,SubCategories.SubSubCategories.Products";

                var categories = await _categoryRepository.GetAllAsync(
                    predicate: predicate,
                    orderBy: query => query.OrderByDescending(c => c.Id),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties: includeProperties,
                    cancellationToken: cancellationToken);

                var totalCount = await _categoryRepository.CountAsync(predicate, cancellationToken);

                if (!categories.Any())
                {
                    return Result<IEnumerable<CategoryDTO>>.Success(
                        new List<CategoryDTO>(),
                        "No categories found matching the criteria.",
                        totalCount,
                        request.PageNumber,
                        request.PageSize);
                }

                // Convert to DTOs
                var categoryDTOs = categories.Select(c => c.ToDTO()).ToList();

                // Calculate product statistics for each category
                await CalculateCategoryStatistics(categoryDTOs, request.UserId, cancellationToken);

                _logger.LogInformation("Retrieved {Count} categories with statistics calculated", categoryDTOs.Count);

                return Result<IEnumerable<CategoryDTO>>.Success(
                    categoryDTOs,
                    "Categories fetched successfully",
                    totalCount,
                    request.PageNumber,
                    request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return Result<IEnumerable<CategoryDTO>>.Failure($"Failed to retrieve categories: {ex.Message}");
            }
        }

        private static Expression<Func<Category, bool>> BuildPredicate(GetAllCategoryQuery request)
        {
            Expression<Func<Category, bool>> predicate = c => true;

            // Apply deletion filter
            if (!request.IncludeDeleted)
            {
                predicate = CombinePredicates(predicate, c => !c.IsDeleted);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                predicate = CombinePredicates(predicate, c => 
                    c.Name.ToLower().Contains(searchTerm) ||
                    c.Description.ToLower().Contains(searchTerm));
            }

            return predicate;
        }

        private async Task CalculateCategoryStatistics(List<CategoryDTO> categoryDTOs, int? userId, CancellationToken cancellationToken)
        {
            foreach (var category in categoryDTOs)
            {
                try
                {
                    // Get all products in this category (from all subcategories)
                    var allProducts = GetAllProductsInCategory(category);

                    if (!allProducts.Any())
                    {
                        SetEmptyStatistics(category);
                        continue;
                    }

                    // Filter out deleted products for statistics
                    var activeProducts = allProducts.Where(p => !p.IsDeleted).ToList();

                    if (!activeProducts.Any())
                    {
                        SetEmptyStatistics(category);
                        continue;
                    }

                    // Apply pricing to products if needed
                    await activeProducts.ApplyPricingAsync(_pricingService, _cacheService, userId, cancellationToken);

                    // Calculate statistics
                    var prices = activeProducts.Select(p => p.CurrentPrice).Where(price => price > 0).ToList();
                    var productsOnSale = activeProducts.Count(p => p.IsOnSale);

                    category.ProductCount = activeProducts.Count;
                    category.ProductsOnSale = productsOnSale;
                    category.HasProductsOnSale = productsOnSale > 0;
                    category.SalePercentage = activeProducts.Count > 0 
                        ? Math.Round((decimal)productsOnSale / activeProducts.Count * 100, 1) 
                        : 0;

                    if (prices.Any())
                    {
                        category.MinPrice = prices.Min();
                        category.MaxPrice = prices.Max();
                        category.AveragePrice = Math.Round(prices.Average(), 2);
                        category.PriceRange = category.MinPrice == category.MaxPrice 
                            ? $"Rs.{category.MinPrice:F2}"
                            : $"Rs.{category.MinPrice:F2} - Rs.{category.MaxPrice:F2}";
                    }
                    else
                    {
                        SetEmptyPriceStatistics(category);
                    }

                    _logger.LogDebug("Category {CategoryName}: {ProductCount} products, {OnSaleCount} on sale, Price range: {PriceRange}",
                        category.Name, category.ProductCount, category.ProductsOnSale, category.PriceRange);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calculating statistics for category {CategoryId}", category.Id);
                    SetEmptyStatistics(category);
                }
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

        // Combine two predicates with AND logic
        private static Expression<Func<Category, bool>> CombinePredicates(
            Expression<Func<Category, bool>> first,
            Expression<Func<Category, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(Category), "c");
            
            var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
            var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
            
            var combinedBody = Expression.AndAlso(firstBody, secondBody);
            
            return Expression.Lambda<Func<Category, bool>>(combinedBody, parameter);
        }

        // Helper to replace parameter in expression
        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
        }

        // Parameter replacer for expression trees
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}