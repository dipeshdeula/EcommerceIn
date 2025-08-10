using System.Security.Cryptography.X509Certificates;
using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Queries
{
    public record GetCartSummaryWithPromoQuery(
        int UserId
    ) : IRequest<Result<CartSummaryWithPromoDTO>>;

    public class GetCartSummaryWithPromoQueryHandler : IRequestHandler<GetCartSummaryWithPromoQuery, Result<CartSummaryWithPromoDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCartSummaryWithPromoQueryHandler> _logger;
        
        public GetCartSummaryWithPromoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetCartSummaryWithPromoQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        
        public async Task<Result<CartSummaryWithPromoDTO>> Handle(GetCartSummaryWithPromoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                //  Get all user's cart items with related data
                var cartItems = await _unitOfWork.CartItems.GetAllAsync(
                    predicate: c => c.UserId == request.UserId && !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow,
                    includeProperties: "Product,AppliedEvent,AppliedPromoCode_Navigation",
                    cancellationToken: cancellationToken
                );
                
                if (!cartItems.Any())
                {
                    return Result<CartSummaryWithPromoDTO>.Success(new CartSummaryWithPromoDTO
                    {
                        UserId = request.UserId,
                        TotalItems = 0,
                        CanCheckout = false,
                        ValidationErrors = new List<string> { "Cart is empty" }
                    });
                }
                
                //  Calculate totals
                var originalSubtotal = cartItems.Sum(c => (c.OriginalPrice > 0 ? c.OriginalPrice : c.ReservedPrice) * c.Quantity);
                var finalSubtotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity);
                var promoCodeDiscounts = cartItems.Sum(c => (c.PromoCodeDiscountAmount ?? 0) * c.Quantity);
                var eventDiscounts = cartItems.Sum(c => (c.EventDiscountAmount ?? 0) * c.Quantity);
                
                //  Group applied promo codes
                var appliedPromoCodes = cartItems
                    .Where(c => c.AppliedPromoCodeId.HasValue)
                    .GroupBy(c => new { c.AppliedPromoCodeId, c.AppliedPromoCode })
                    .Select(g => new AppliedPromoCodeSummaryDTO
                    {
                        PromoCodeId = g.Key.AppliedPromoCodeId ?? 0,
                        Code = g.Key.AppliedPromoCode ?? "",
                        Name = g.First().AppliedPromoCode_Navigation?.Name ?? "",
                        Type = g.First().AppliedPromoCode_Navigation?.Type ?? PromoCodeType.Percentage,
                        TotalDiscount = g.Sum(c => (c.PromoCodeDiscountAmount ?? 0) * c.Quantity),
                        AffectedItemsCount = g.Count(),
                        AppliedAt = g.Max(c => c.UpdatedAt),
                        FormattedDiscount = g.First().AppliedPromoCode_Navigation?.Type switch
                        {
                            PromoCodeType.Percentage => $"{g.First().AppliedPromoCode_Navigation?.DiscountValue}% OFF",
                            PromoCodeType.FixedAmount => $"Rs.{g.First().AppliedPromoCode_Navigation?.DiscountValue} OFF",
                            PromoCodeType.FreeShipping => "FREE SHIPPING",
                            _ => "DISCOUNT APPLIED"
                        }
                    })
                    .ToList();
                
                //  Convert cart items to DTOs
                var cartItemDTOs = cartItems.Select(c => new CartItemWithPromoDTO
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product?.Name ?? "Unknown Product",
                    Quantity = c.Quantity,
                    OriginalPrice = c.OriginalPrice > 0 ? c.OriginalPrice : c.ReservedPrice,
                    RegularDiscountAmount = c.RegularDiscountAmount,
                    EventDiscountAmount = c.EventDiscountAmount ?? 0,
                    PromoCodeDiscountAmount = c.PromoCodeDiscountAmount ?? 0,
                    FinalPrice = c.ReservedPrice,
                    TotalSavings = ((c.OriginalPrice > 0 ? c.OriginalPrice : c.ReservedPrice) - c.ReservedPrice) * c.Quantity,
                    AppliedEventId = c.AppliedEventId,
                    AppliedEventName = c.AppliedEvent?.Name,
                    AppliedPromoCodeId = c.AppliedPromoCodeId,
                    AppliedPromoCode = c.AppliedPromoCode,
                    HasPromoDiscount = c.PromoCodeDiscountAmount > 0,
                    HasEventDiscount = c.EventDiscountAmount > 0,
                    IsExpired = c.ExpiresAt <= DateTime.UtcNow,
                    ExpiresAt = c.ExpiresAt
                }).ToList();
                
                var summary = new CartSummaryWithPromoDTO
                {
                    UserId = request.UserId,
                    TotalItems = cartItems.Count(),
                    TotalQuantity = cartItems.Sum(c => c.Quantity),
                    OriginalSubtotal = originalSubtotal,
                    EventDiscounts = eventDiscounts,
                    PromoCodeDiscounts = promoCodeDiscounts,
                    TotalDiscounts = promoCodeDiscounts + eventDiscounts,
                    FinalSubtotal = finalSubtotal,
                    AppliedPromoCodes = appliedPromoCodes,
                    Items = cartItemDTOs,
                    CanCheckout = cartItems.Any() && !cartItemDTOs.Any(c => c.IsExpired),
                    HasExpiredItems = cartItemDTOs.Any(c => c.IsExpired),
                    HasOutOfStockItems = false,
                    ValidationErrors = new List<string>(),
                    LastUpdated = DateTime.UtcNow,
                    EarliestExpiration = cartItems.Min(c => c.ExpiresAt)
                };
                
                return Result<CartSummaryWithPromoDTO>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary with promo codes for user {UserId}", request.UserId);
                return Result<CartSummaryWithPromoDTO>.Failure($"Error getting cart summary: {ex.Message}");
            }
        }
    }
}