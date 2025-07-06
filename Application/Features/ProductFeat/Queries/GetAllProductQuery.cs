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
        int PageNumber = 1,
        int PageSize = 10,
        int? UserId = null,
        bool OnSaleOnly = false,
        bool PrioritizeEventProducts = false, 
        string? SearchTerm = null
    ) : IRequest<Result<IEnumerable<ProductDTO>>>;

    public class GetAllProductQueryHandler : IRequestHandler<GetAllProductQuery, Result<IEnumerable<ProductDTO>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _pricingService;
        private readonly ILogger<GetAllProductQueryHandler> _logger;

        public GetAllProductQueryHandler(
            IProductRepository productRepository,
            IProductPricingService pricingService,
            ILogger<GetAllProductQueryHandler> logger)
        {
            _productRepository = productRepository;
            _pricingService = pricingService;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<ProductDTO>>> Handle(GetAllProductQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var productDTOs = new List<ProductDTO>();

                //  1. Get event-highlighted products first (if prioritization enabled)
                if (request.PrioritizeEventProducts)
                {
                    var eventProductIds = await _pricingService.GetEventHighlightedProductIdsAsync(cancellationToken);

                    if (eventProductIds.Any())
                    {
                        // Build predicate for event products
                        System.Linq.Expressions.Expression<Func<Product, bool>> eventPredicate = p =>
                            !p.IsDeleted && eventProductIds.Contains(p.Id);

                        if (!string.IsNullOrEmpty(request.SearchTerm))
                        {
                            eventPredicate = p => !p.IsDeleted &&
                                                eventProductIds.Contains(p.Id) &&
                                                p.Name.ToLower().Contains(request.SearchTerm.ToLower());
                        }

                        // Get event products first
                        var eventProducts = await _productRepository.GetAllAsync(
                            predicate: eventPredicate,
                            //orderBy: query => query.OrderByDescending(p => p.Id),
                            take: Math.Min(request.PageSize, eventProductIds.Count),
                            includeProperties: "Product.Images",
                            cancellationToken: cancellationToken)
                            .QuickSort(p => p.Name);

                        var eventProductDTOs = eventProducts.Select(p => p.ToDTO()).ToList();
                        await eventProductDTOs.ApplyPricingAsync(_pricingService, request.UserId, cancellationToken);

                        // Filter by sale status if requested
                        if (request.OnSaleOnly)
                        {
                            eventProductDTOs = eventProductDTOs.Where(p => p.IsOnSale).ToList();
                        }

                        productDTOs.AddRange(eventProductDTOs);
                    }
                }

                // 2. Fill remaining slots with regular products (if needed)
                var remainingSlots = request.PageSize - productDTOs.Count;
                if (remainingSlots > 0)
                {
                    var existingProductIds = productDTOs.Select(p => p.Id);

                    // Build predicate for regular products
                    System.Linq.Expressions.Expression<Func<Product, bool>> regularPredicate = p =>
                        !p.IsDeleted && !existingProductIds.Contains(p.Id);

                    if (!string.IsNullOrEmpty(request.SearchTerm))
                    {
                        regularPredicate = p => !p.IsDeleted &&
                                              !existingProductIds.Contains(p.Id) &&
                                              p.Name.ToLower().Contains(request.SearchTerm.ToLower());
                    }

                    var regularProducts = await _productRepository.GetAllAsync(
                        predicate: regularPredicate,
                        skip: request.PrioritizeEventProducts ? 0 : (request.PageNumber - 1) * request.PageSize,
                        take: remainingSlots,
                        includeProperties: "Images",
                        cancellationToken: cancellationToken)
                        .QuickSort(p=>p.Name);

                    var regularProductDTOs = regularProducts.Select(p => p.ToDTO()).ToList();
                    await regularProductDTOs.ApplyPricingAsync(_pricingService, request.UserId, cancellationToken);

                    // Filter by sale status if requested
                    if (request.OnSaleOnly)
                    {
                        regularProductDTOs = regularProductDTOs.Where(p => p.IsOnSale).ToList();
                    }

                    productDTOs.AddRange(regularProductDTOs);
                }

                //  3. Final sorting - event products first, then regular products
                if (request.PrioritizeEventProducts)
                {
                    productDTOs = productDTOs
                        .OrderByDescending(p => p.Pricing.HasActiveEvent) // Event products first
                        .ThenByDescending(p => p.Pricing.TotalDiscountPercentage) // Highest discount first
                        .ThenByDescending(p => p.Id) // Newest first
                        .Take(request.PageSize)
                        .ToList();
                }

                var onSaleCount = productDTOs.Count(p => p.IsOnSale);

                _logger.LogInformation("Retrieved {Count} products with dynamic pricing for user {UserId}. {OnSaleCount} items on sale, {EventCount} event products",
                    productDTOs.Count, request.UserId, onSaleCount, productDTOs.Count(p => p.Pricing.HasActiveEvent));

                return Result<IEnumerable<ProductDTO>>.Success(productDTOs,
                    $"Products retrieved successfully. {onSaleCount} items on sale.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products with dynamic pricing");
                return Result<IEnumerable<ProductDTO>>.Failure($"Failed to retrieve products: {ex.Message}");
            }
        }
    }
}