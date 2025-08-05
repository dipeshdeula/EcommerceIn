using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utilities;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.ProductFeat.Queries
{
    public record GetAllProductQuery(
        int PageNumber ,
        int PageSize ,
        int? UserId ,
        bool? OnSaleOnly ,
        bool? PrioritizeEventProducts , 
        string? SearchTerm ,
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

                // ✅ 1. Get event-highlighted products first (if prioritization enabled and not admin request)
                if (request.PrioritizeEventProducts == true && !request.IsAdminRequest)
                {
                    var eventProductIds = await _pricingService.GetEventHighlightedProductIdsAsync(cancellationToken);

                    if (eventProductIds.Any())
                    {
                        // ✅ Build predicate for event products with conditional deletion filter
                        System.Linq.Expressions.Expression<Func<Product, bool>> eventPredicate = BuildProductPredicate(
                            includeDeleted: request.IncludeDeleted,
                            searchTerm: request.SearchTerm,
                            productIds: eventProductIds);

                        // Get event products first
                        var eventProducts = await _productRepository.GetAllAsync(
                            predicate: eventPredicate,
                            orderBy: query => query.OrderByDescending(p => p.Id),
                            take: Math.Min(request.PageSize, eventProductIds.Count),
                            includeProperties: "Images",
                            includeDeleted: request.IncludeDeleted, // ✅ Pass includeDeleted flag
                            cancellationToken: cancellationToken);

                        var eventProductDTOs = eventProducts.Select(p => p.ToDTO()).ToList();
                        
                        // ✅ Only apply pricing for non-deleted products or admin requests
                        if (!request.IsAdminRequest || eventProductDTOs.Any(p => !p.IsDeleted))
                        {
                            await eventProductDTOs.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                        }

                        // Filter by sale status if requested (skip for deleted products)
                        if (request.OnSaleOnly == true)
                        {
                            eventProductDTOs = eventProductDTOs.Where(p => p.IsOnSale || p.IsDeleted).ToList();
                        }

                        productDTOs.AddRange(eventProductDTOs);
                    }
                }

                // ✅ 2. Fill remaining slots with regular products (or get all for admin)
                var remainingSlots = request.IsAdminRequest ? request.PageSize : request.PageSize - productDTOs.Count;
                if (remainingSlots > 0)
                {
                    var existingProductIds = request.IsAdminRequest ? new List<int>() : productDTOs.Select(p => p.Id).ToList();

                    // ✅ Build predicate for regular products with conditional deletion filter
                    System.Linq.Expressions.Expression<Func<Product, bool>> regularPredicate = BuildProductPredicate(
                        includeDeleted: request.IncludeDeleted,
                        searchTerm: request.SearchTerm,
                        excludeProductIds: existingProductIds);

                    var regularProducts = await _productRepository.GetAllAsync(
                        predicate: regularPredicate,
                        orderBy: query => request.IsAdminRequest 
                            ? query.OrderByDescending(p => p.Id).ThenBy(p => p.IsDeleted) // ✅ Admin: Active first, then deleted
                            : query.OrderByDescending(p => p.Id),
                        skip: request.IsAdminRequest ? (request.PageNumber - 1) * request.PageSize : 0,
                        take: remainingSlots,
                        includeProperties: "Images",
                        includeDeleted: request.IncludeDeleted, // ✅ Pass includeDeleted flag
                        cancellationToken: cancellationToken);

                    var regularProductDTOs = regularProducts.Select(p => p.ToDTO()).ToList();

                    // ✅ Apply pricing only for active products or admin requests
                    if (!request.IsAdminRequest || regularProductDTOs.Any(p => !p.IsDeleted))
                    {
                        await regularProductDTOs.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                    }

                    // Filter by sale status if requested (skip for deleted products)
                    if (request.OnSaleOnly == true)
                    {
                        regularProductDTOs = regularProductDTOs.Where(p => p.IsOnSale || p.IsDeleted).ToList();
                    }

                    productDTOs.AddRange(regularProductDTOs);
                }

                // ✅ 3. Final sorting based on request type
                if (request.IsAdminRequest)
                {
                    // ✅ Admin sorting: Active products first, then deleted, with newest first
                    productDTOs = productDTOs
                        .OrderBy(p => p.IsDeleted)                    // Active products first
                        .ThenByDescending(p => p.Id)           // Newest first within each group
                        .ThenByDescending(p => p.Pricing?.HasActiveEvent ?? false) // Event products priority
                        .Take(request.PageSize)
                        .ToList();
                }
                else if (request.PrioritizeEventProducts ?? false)
                {
                    // ✅ Customer sorting: Event products first, highest discounts
                    productDTOs = productDTOs
                        .OrderByDescending(p => p.Pricing?.HasActiveEvent ?? false) // Event products first
                        .ThenByDescending(p => p.Pricing?.TotalDiscountPercentage ?? 0) // Highest discount first
                        .ThenByDescending(p => p.Id) // Newest first
                        .Take(request.PageSize)
                        .ToList();
                }

                // ✅ Calculate statistics
                var onSaleCount = productDTOs.Count(p => p.IsOnSale);
                var eventProductCount = productDTOs.Count(p => p.Pricing?.HasActiveEvent ?? false);
                var deletedCount = productDTOs.Count(p => p.IsDeleted);
                var activeCount = productDTOs.Count(p => !p.IsDeleted);

                // ✅ Enhanced logging for admin requests
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

                // ✅ Enhanced success message
                var message = request.IsAdminRequest 
                    ? $"Products retrieved successfully for admin. Active: {activeCount}, Deleted: {deletedCount}, On Sale: {onSaleCount}"
                    : $"Products retrieved successfully. {onSaleCount} items on sale.";

                return Result<IEnumerable<ProductDTO>>.Success(productDTOs, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with dynamic pricing (Admin: {IsAdmin}, IncludeDeleted: {IncludeDeleted})", 
                    request.IsAdminRequest, request.IncludeDeleted);
                return Result<IEnumerable<ProductDTO>>.Failure($"Failed to retrieve products: {ex.Message}");
            }
        }

         private static System.Linq.Expressions.Expression<Func<Product, bool>> BuildProductPredicate(
            bool includeDeleted,
            string? searchTerm = null,
            List<int>? productIds = null,
            List<int>? excludeProductIds = null)
        {
            System.Linq.Expressions.Expression<Func<Product, bool>> predicate = p => true;

            // ✅ Apply deletion filter conditionally
            if (!includeDeleted)
            {
                predicate = p => !p.IsDeleted;
            }

            // ✅ Apply search term filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var currentPredicate = predicate;
                predicate = p => currentPredicate.Compile()(p) && p.Name.Contains(searchTerm);
            }

            // ✅ Apply product IDs filter (for event products)
            if (productIds?.Any() == true)
            {
                var currentPredicate = predicate;
                predicate = p => currentPredicate.Compile()(p) && productIds.Contains(p.Id);
            }

            // ✅ Apply exclude product IDs filter (to avoid duplicates)
            if (excludeProductIds?.Any() == true)
            {
                var currentPredicate = predicate;
                predicate = p => currentPredicate.Compile()(p) && !excludeProductIds.Contains(p.Id);
            }

            return predicate;
        }
        

    }
}