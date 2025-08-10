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
                var productDTOs = new List<ProductDTO>();
                int totalCount = 0;

                //  FIX: Get total count FIRST for proper pagination
                var countPredicate = BuildProductPredicate(
                    includeDeleted: request.IncludeDeleted,
                    searchTerm: request.SearchTerm);

                totalCount = await _productRepository.CountAsync(countPredicate, cancellationToken);

                // 1. Get event-highlighted products first (if prioritization enabled and not admin request)
                if (request.PrioritizeEventProducts == true && !request.IsAdminRequest)
                {
                    var eventProductIds = await _pricingService.GetEventHighlightedProductIdsAsync(cancellationToken);

                    if (eventProductIds.Any())
                    {
                        var eventPredicate = BuildProductPredicate(
                            includeDeleted: request.IncludeDeleted,
                            searchTerm: request.SearchTerm,
                            productIds: eventProductIds);

                        var eventProducts = await _productRepository.GetAllAsync(
                            predicate: eventPredicate,
                            orderBy: query => query.OrderByDescending(p => p.Id),
                            take: Math.Min(request.PageSize, eventProductIds.Count),
                            includeProperties: "Images",
                            includeDeleted: request.IncludeDeleted,
                            cancellationToken: cancellationToken);

                        var eventProductDTOs = eventProducts.Select(p => p.ToDTO()).ToList();
                        
                        // Apply pricing to active products
                        var activeEventProducts = eventProductDTOs.Where(p => !p.IsDeleted).ToList();
                        if (activeEventProducts.Any())
                        {
                            await activeEventProducts.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                        }

                        // Filter by sale status if requested
                        if (request.OnSaleOnly == true)
                        {
                            eventProductDTOs = eventProductDTOs.Where(p => p.IsOnSale || (request.IsAdminRequest && p.IsDeleted)).ToList();
                        }

                        productDTOs.AddRange(eventProductDTOs);
                    }
                }

                // 2. Fill remaining slots with regular products
                var remainingSlots = request.PageSize - productDTOs.Count;
                if (remainingSlots > 0)
                {
                    var existingProductIds = productDTOs.Select(p => p.Id).ToList();
                    var skip = request.IsAdminRequest ? (request.PageNumber - 1) * request.PageSize : 0;

                    var regularPredicate = BuildProductPredicate(
                        includeDeleted: request.IncludeDeleted,
                        searchTerm: request.SearchTerm,
                        excludeProductIds: existingProductIds);

                    var regularProducts = await _productRepository.GetAllAsync(
                        predicate: regularPredicate,
                        orderBy: query => request.IsAdminRequest 
                            ? query.OrderBy(p => p.IsDeleted).ThenByDescending(p => p.Id)  // Active first, then deleted
                            : query.OrderByDescending(p => p.Id),
                        skip: skip,
                        take: remainingSlots,
                        includeProperties: "Images",
                        includeDeleted: request.IncludeDeleted,
                        cancellationToken: cancellationToken);

                    var regularProductDTOs = regularProducts.Select(p => p.ToDTO()).ToList();

                    // Apply pricing to active products
                    var activeRegularProducts = regularProductDTOs.Where(p => !p.IsDeleted).ToList();
                    if (activeRegularProducts.Any())
                    {
                        await activeRegularProducts.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                    }

                    // Filter by sale status if requested
                    if (request.OnSaleOnly == true)
                    {
                        regularProductDTOs = regularProductDTOs.Where(p => p.IsOnSale || (request.IsAdminRequest && p.IsDeleted)).ToList();
                    }

                    productDTOs.AddRange(regularProductDTOs);
                }

                // 3. Final sorting
                if (request.IsAdminRequest)
                {
                    productDTOs = productDTOs
                        .OrderBy(p => p.IsDeleted)
                        .ThenByDescending(p => p.Id)
                        .ThenByDescending(p => p.Pricing?.HasActiveEvent ?? false)
                        .Take(request.PageSize)
                        .ToList();
                }
                else if (request.PrioritizeEventProducts ?? false)
                {
                    productDTOs = productDTOs
                        .OrderByDescending(p => p.Pricing?.HasActiveEvent ?? false)
                        .ThenByDescending(p => p.Pricing?.TotalDiscountPercentage ?? 0)
                        .ThenByDescending(p => p.Id)
                        .Take(request.PageSize)
                        .ToList();
                }

                // Calculate statistics
                var onSaleCount = productDTOs.Count(p => p.IsOnSale);
                var eventProductCount = productDTOs.Count(p => p.Pricing?.HasActiveEvent ?? false);
                var deletedCount = productDTOs.Count(p => p.IsDeleted);
                var activeCount = productDTOs.Count(p => !p.IsDeleted);

                // Enhanced logging
                if (request.IsAdminRequest)
                {
                    _logger.LogInformation("Admin retrieved {Count} products (Active: {ActiveCount}, Deleted: {DeletedCount}, OnSale: {OnSaleCount}, Event: {EventCount}) for user {UserId}",
                        productDTOs.Count, activeCount, deletedCount, onSaleCount, eventProductCount, request.UserId);
                }
                else
                {
                    _logger.LogInformation("Retrieved {Count} products with dynamic pricing for user {UserId}. {OnSaleCount} items on sale, {EventCount} event products",
                        productDTOs.Count, request.UserId, onSaleCount, eventProductCount);
                }

                var message = request.IsAdminRequest 
                    ? $"Products retrieved successfully for admin. Active: {activeCount}, Deleted: {deletedCount}, On Sale: {onSaleCount}"
                    : $"Products retrieved successfully. {onSaleCount} items on sale.";

                //  Return with proper pagination information
                return Result<IEnumerable<ProductDTO>>.Success(
                    productDTOs, 
                    message,
                    totalCount,
                    request.PageNumber,
                    request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with dynamic pricing (Admin: {IsAdmin}, IncludeDeleted: {IncludeDeleted})", 
                    request.IsAdminRequest, request.IncludeDeleted);
                return Result<IEnumerable<ProductDTO>>.Failure($"Failed to retrieve products: {ex.Message}");
            }
        }

        private static Expression<Func<Product, bool>> BuildProductPredicate(
            bool includeDeleted,
            string? searchTerm = null,
            List<int>? productIds = null,
            List<int>? excludeProductIds = null)
        {
            Expression<Func<Product, bool>> predicate = p => true;

            // Apply deletion filter
            if (!includeDeleted)
            {
                predicate = CombinePredicates(predicate, p => !p.IsDeleted);
            }

            // Apply search term filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                predicate = CombinePredicates(predicate, p => 
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower) ||
                    p.Sku.ToLower().Contains(searchLower));
            }

            // Apply product IDs filter
            if (productIds?.Any() == true)
            {
                predicate = CombinePredicates(predicate, p => productIds.Contains(p.Id));
            }

            // Apply exclude product IDs filter
            if (excludeProductIds?.Any() == true)
            {
                predicate = CombinePredicates(predicate, p => !excludeProductIds.Contains(p.Id));
            }

            return predicate;
        }

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

        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
        }

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