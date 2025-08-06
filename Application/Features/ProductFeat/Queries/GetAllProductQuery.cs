using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utilities;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Features.ProductFeat.Queries
{
    public record GetAllProductQuery(
        int PageNumber,
        int PageSize,
        int? UserId,
        bool? OnSaleOnly,
        bool? PrioritizeEventProducts,
        string? SearchTerm,
        bool IncludeDeleted = false,
        bool IsAdminRequest = false
    ) : IRequest<Result<IEnumerable<ProductDTO>>>;

    public class GetAllProductQueryHandler : IRequestHandler<GetAllProductQuery, Result<IEnumerable<ProductDTO>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<GetAllProductQueryHandler> _logger;

        public GetAllProductQueryHandler(
            IProductRepository productRepository,
            IProductPricingService pricingService,
            IHybridCacheService cacheService,
            ILogger<GetAllProductQueryHandler> logger)
        {
            _productRepository = productRepository;
            _pricingService = pricingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<ProductDTO>>> Handle(GetAllProductQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Build the main predicate
                var predicate = BuildMainPredicate(request);

                // 2. Get total count using same predicate
                var totalCount = await _productRepository.CountAsync(predicate, cancellationToken);

                if (totalCount == 0)
                {
                    return Result<IEnumerable<ProductDTO>>.Success(
                        new List<ProductDTO>(),
                        "No products found matching the criteria.",
                        0,
                        request.PageNumber,
                        request.PageSize);
                }

                // 3. Get products with pagination
                var products = await _productRepository.GetAllAsync(
                    predicate: predicate,
                    orderBy: GetOrderByExpression(request),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties: "Images",
                    cancellationToken: cancellationToken);

                var productDTOs = products.Select(p => p.ToDTO()).ToList();

                // 4. Apply pricing only to active products
                var activeProducts = productDTOs.Where(p => !p.IsDeleted).ToList();
                if (activeProducts.Any())
                {
                    await activeProducts.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                }

                // 5. Handle deleted products
                HandleDeletedProducts(productDTOs);

                // 6. Apply OnSaleOnly filter after pricing
                if (request.OnSaleOnly == true)
                {
                    productDTOs = productDTOs.Where(p => p.IsOnSale).ToList();
                    // Recalculate total count for OnSale filter
                    totalCount = productDTOs.Count;
                }

                // 7. Calculate statistics and create response
                var stats = CalculateStatistics(productDTOs);
                LogResults(request, stats, totalCount);
                var message = CreateSuccessMessage(request, stats, totalCount);

                return Result<IEnumerable<ProductDTO>>.Success(
                    productDTOs,
                    message,
                    totalCount,
                    request.PageNumber,
                    request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products: Admin={IsAdmin}, IncludeDeleted={IncludeDeleted}",
                    request.IsAdminRequest, request.IncludeDeleted);
                return Result<IEnumerable<ProductDTO>>.Failure($"Failed to retrieve products: {ex.Message}");
            }
        }

        // Build the main predicate based on request parameters
        private static Expression<Func<Product, bool>> BuildMainPredicate(GetAllProductQuery request)
        {
            Expression<Func<Product, bool>> predicate = p => true;

            // Apply deletion filter based on user type and request
            if (request.IsAdminRequest && request.IncludeDeleted)
            {
                // Admin wants to see all products (including deleted)
                // No additional filter needed
            }
            else
            {
                // Customer or admin wanting only active products
                predicate = CombinePredicates(predicate, p => !p.IsDeleted);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                predicate = CombinePredicates(predicate, p => 
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm) ||
                    p.Sku.ToLower().Contains(searchTerm));
            }

            return predicate;
        }

        // Get ordering expression
        private static Func<IQueryable<Product>, IOrderedQueryable<Product>> GetOrderByExpression(GetAllProductQuery request)
        {
            if (request.IsAdminRequest)
            {
                // Admin: Show active products first, then by newest
                return query => query
                    .OrderBy(p => p.IsDeleted)
                    .ThenByDescending(p => p.Id);
            }
            else
            {
                // Customer: Show newest first
                return query => query.OrderByDescending(p => p.Id);
            }
        }

        // Handle deleted products (set appropriate values)
        private static void HandleDeletedProducts(List<ProductDTO> productDTOs)
        {
            var deletedProducts = productDTOs.Where(p => p.IsDeleted).ToList();
            foreach (var deletedProduct in deletedProducts)
            {
                // Set pricing info for deleted products
                deletedProduct.Pricing = new ProductPricingDTO
                {
                    ProductId = deletedProduct.Id,
                    ProductName = deletedProduct.Name,
                    OriginalPrice = deletedProduct.MarketPrice,
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
                    HasActiveEvent = false,
                    IsEventActive = false,
                    FormattedOriginalPrice = $"Rs.{deletedProduct.MarketPrice:F2}",
                    FormattedEffectivePrice = "Rs.0.00",
                    FormattedSavings = "",
                    IsPriceStable = false,
                    CalculatedAt = DateTime.UtcNow
                };

                // Override computed properties for deleted products
                deletedProduct.BasePrice = 0;
                deletedProduct.ProductDiscountAmount = 0;
                deletedProduct.HasProductDiscount = false;
                deletedProduct.FormattedBasePrice = "Rs.0.00";
                deletedProduct.FormattedDiscountAmount = "Rs.0.00";
            }
        }

        // Combine two predicates with AND logic
        private static Expression<Func<Product, bool>> CombinePredicates(
            Expression<Func<Product, bool>> first,
            Expression<Func<Product, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(Product), "p");
            
            var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
            var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
            
            var combinedBody = Expression.AndAlso(firstBody, secondBody);
            
            return Expression.Lambda<Func<Product, bool>>(combinedBody, parameter);
        }

        // Helper to replace parameter in expression
        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
        }

        // Calculate statistics for logging
        private static ProductStatistics CalculateStatistics(List<ProductDTO> productDTOs)
        {
            return new ProductStatistics
            {
                TotalProducts = productDTOs.Count,
                OnSaleCount = productDTOs.Count(p => p.IsOnSale),
                EventProductCount = productDTOs.Count(p => p.Pricing?.HasActiveEvent ?? false),
                DeletedCount = productDTOs.Count(p => p.IsDeleted),
                ActiveCount = productDTOs.Count(p => !p.IsDeleted)
            };
        }

        // Log results
        private void LogResults(GetAllProductQuery request, ProductStatistics stats, int totalCount)
        {
            if (request.IsAdminRequest)
            {
                _logger.LogInformation("Admin retrieved {Count}/{TotalCount} products (Active: {ActiveCount}, Deleted: {DeletedCount}, OnSale: {OnSaleCount}) - Page {PageNumber}",
                    stats.TotalProducts, totalCount, stats.ActiveCount, stats.DeletedCount, stats.OnSaleCount, request.PageNumber);
            }
            else
            {
                _logger.LogInformation("Customer retrieved {Count}/{TotalCount} products - Page {PageNumber}. {OnSaleCount} items on sale",
                    stats.TotalProducts, totalCount, request.PageNumber, stats.OnSaleCount);
            }
        }

        // Create success message
        private static string CreateSuccessMessage(GetAllProductQuery request, ProductStatistics stats, int totalCount)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            
            if (request.IsAdminRequest)
            {
                return $"Products retrieved successfully for admin. Page {request.PageNumber} of {totalPages}. Active: {stats.ActiveCount}, Deleted: {stats.DeletedCount}, On Sale: {stats.OnSaleCount}";
            }

            return $"Products retrieved successfully. Page {request.PageNumber} of {totalPages}. {stats.OnSaleCount} items on sale.";
        }

        // Helper classes
        private class ProductStatistics
        {
            public int TotalProducts { get; set; }
            public int OnSaleCount { get; set; }
            public int EventProductCount { get; set; }
            public int DeletedCount { get; set; }
            public int ActiveCount { get; set; }
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