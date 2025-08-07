using Application.Common;
using Application.Dto.ProductDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.ProductFeat.Queries
{
    public record GetProductByIdQuery(int productId, int PageNumber, int PageSize) : IRequest<Result<ProductDTO>>
    {
        // Add properties for admin support and user context
        public int? UserId { get; set; }
        public bool IsAdminRequest { get; set; } = false;
        public bool IncludeDeleted { get; set; } = false;
    }

    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductPricingService _pricingService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<GetProductByIdQueryHandler> _logger;

        public GetProductByIdQueryHandler(
            IProductRepository productRepository,
            IProductPricingService pricingService,
            IHybridCacheService cacheService,
            ILogger<GetProductByIdQueryHandler> logger)
        {
            _productRepository = productRepository;
            _pricingService = pricingService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<ProductDTO>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching product {ProductId} - IsAdmin: {IsAdmin}, IncludeDeleted: {IncludeDeleted}", 
                    request.productId, request.IsAdminRequest, request.IncludeDeleted);

                //  Build query based on admin permissions
                IQueryable<Product> query = _productRepository.Queryable;

                // Apply deletion filter based on admin permissions
                if (!request.IsAdminRequest || !request.IncludeDeleted)
                {
                    query = query.Where(p => !p.IsDeleted);
                }

                // Include related data after applying filters
                query = query.Include(p => p.Images);

                var product = await query.FirstOrDefaultAsync(p => p.Id == request.productId, cancellationToken);

                if (product == null)
                {
                    var message = request.IsAdminRequest 
                        ? $"Product with ID {request.productId} not found"
                        : $"Product with ID {request.productId} not found or unavailable";
                    
                    _logger.LogWarning("Product {ProductId} not found - IsAdmin: {IsAdmin}", request.productId, request.IsAdminRequest);
                    return Result<ProductDTO>.Failure(message);
                }

                var productDTO = product.ToDTO();

                // Apply pricing logic based on product status
                if (!productDTO.IsDeleted)
                {
                    // Apply dynamic pricing for active products
                    var productList = new List<ProductDTO> { productDTO };
                    await productList.ApplyPricingAsync(_pricingService, _cacheService, request.UserId, cancellationToken);
                    
                    _logger.LogDebug("Applied dynamic pricing for active product {ProductId}: EffectivePrice={EffectivePrice}, IsOnSale={IsOnSale}", 
                        productDTO.Id, productDTO.Pricing?.EffectivePrice, productDTO.IsOnSale);
                }
                else if (request.IsAdminRequest)
                {
                    // Set basic pricing for deleted products (admin view)
                    productDTO.Pricing = CreateBasicPricingForDeletedProduct(productDTO);
                    productDTO.BasePrice = 0;
                    productDTO.ProductDiscountAmount = 0;
                    productDTO.HasProductDiscount = false;
                    productDTO.FormattedBasePrice = "Rs.0.00";
                    productDTO.FormattedDiscountAmount = "Rs.0.00";                    
                   
                    
                    _logger.LogDebug("Applied admin pricing for deleted product {ProductId}", productDTO.Id);
                }

                // Enhanced logging and context message
                var contextMessage = request.IsAdminRequest 
                    ? $"Product '{productDTO.Name}' retrieved for admin (Status: {(productDTO.IsDeleted ? "DELETED" : "ACTIVE")})"
                    : $"Product '{productDTO.Name}' retrieved successfully";

                _logger.LogInformation("Product {ProductId} retrieved successfully: Name='{ProductName}', IsDeleted={IsDeleted}, Price={Price}", 
                    request.productId, productDTO.Name, productDTO.IsDeleted, productDTO.CurrentPrice);

                return Result<ProductDTO>.Success(productDTO, contextMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId} - IsAdmin: {IsAdmin}", 
                    request.productId, request.IsAdminRequest);
                return Result<ProductDTO>.Failure($"Failed to retrieve product: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates basic pricing information for deleted products (admin view)
        /// </summary>
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
                ActiveEventId = null,
                ActiveEventName = null,
                EventTagLine = null,
                PromotionType = null,
                EventStartDate = null,
                EventEndDate = null,
                HasActiveEvent = false,
                IsEventActive = false,
                EventTimeRemaining = null,
                IsEventExpiringSoon = false,
                FormattedOriginalPrice = $"Rs.{product.MarketPrice:F2}",
                FormattedEffectivePrice = "Rs.0.00 (DELETED)",
                FormattedSavings = "",
                FormattedDiscountBreakdown = "",
                EventStatus = "N/A - Product Deleted",
                HasFreeShipping = false,
                IsPriceStable = false,
                CalculatedAt = DateTime.UtcNow
            };
        }
    }
}