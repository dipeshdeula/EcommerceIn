using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Queries
{
    public record CalculateCheckoutPricingQuery(
        int UserId,
        decimal ShippingCost,
        decimal TaxRate = 0.13m
    ) : IRequest<Result<OrderPricingSummaryDTO>>;

    public class CalculateCheckoutPricingQueryHandler : IRequestHandler<CalculateCheckoutPricingQuery, Result<OrderPricingSummaryDTO>>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CalculateCheckoutPricingQueryHandler> _logger;
        
        public CalculateCheckoutPricingQueryHandler(
            IMediator mediator,
            ILogger<CalculateCheckoutPricingQueryHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task<Result<OrderPricingSummaryDTO>> Handle(CalculateCheckoutPricingQuery request, CancellationToken cancellationToken)
        {
            try
            {
                //  Get cart summary first using existing query
                var cartSummaryResult = await _mediator.Send(new GetCartSummaryWithPromoQuery(request.UserId), cancellationToken);
                if (!cartSummaryResult.Succeeded)
                {
                    return Result<OrderPricingSummaryDTO>.Failure(cartSummaryResult.Message);
                }
                
                var cartSummary = cartSummaryResult.Data!;
                
                //  Calculate shipping discounts
                var shippingDiscounts = cartSummary.AppliedPromoCodes
                    .Where(p => p.Type == PromoCodeType.FreeShipping)
                    .Sum(p => p.TotalDiscount);
                
                var finalShipping = Math.Max(0, request.ShippingCost - shippingDiscounts);
                
                //  Calculate tax
                var taxableAmount = cartSummary.FinalSubtotal + finalShipping;
                var finalTax = taxableAmount * request.TaxRate;
                
                var orderSummary = new OrderPricingSummaryDTO
                {
                    //  ORIGINAL AMOUNTS
                    OriginalSubtotal = cartSummary.OriginalSubtotal,
                    OriginalShipping = request.ShippingCost,
                    OriginalTax = (cartSummary.OriginalSubtotal + request.ShippingCost) * request.TaxRate,
                    OriginalTotal = cartSummary.OriginalSubtotal + request.ShippingCost + ((cartSummary.OriginalSubtotal + request.ShippingCost) * request.TaxRate),
                    
                    //  DISCOUNT BREAKDOWN
                    ProductDiscounts = 0,
                    EventDiscounts = cartSummary.EventDiscounts,
                    PromoCodeDiscounts = cartSummary.PromoCodeDiscounts,
                    ShippingDiscounts = shippingDiscounts,
                    TotalDiscounts = cartSummary.TotalDiscounts + shippingDiscounts,
                    
                    //  FINAL AMOUNTS
                    FinalSubtotal = cartSummary.FinalSubtotal,
                    FinalShipping = finalShipping,
                    FinalTax = finalTax,
                    FinalTotal = cartSummary.FinalSubtotal + finalShipping + finalTax,
                    
                    //  APPLIED PROMO CODES
                    AppliedPromoCodes = cartSummary.AppliedPromoCodes.Select(p => new OrderPromoCodeSummaryDTO
                    {
                        PromoCodeId = p.PromoCodeId,
                        Code = p.Code,
                        Name = p.Name,
                        Type = p.Type,
                        DiscountAmount = p.TotalDiscount,
                        ShippingDiscount = p.Type == PromoCodeType.FreeShipping ? p.TotalDiscount : 0,
                        AppliedToShipping = p.Type == PromoCodeType.FreeShipping,
                        AffectedItemsCount = p.AffectedItemsCount,
                        FormattedDiscount = p.FormattedDiscount
                    }).ToList(),
                    
                    //  CART SUMMARY
                    TotalItems = cartSummary.TotalItems,
                    TotalQuantity = cartSummary.TotalQuantity,
                    Items = cartSummary.Items.Select(i => new OrderItemPricingDTO
                    {
                        CartItemId = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.OriginalPrice,
                        LineTotal = i.OriginalPrice * i.Quantity,
                        TotalDiscount = (i.OriginalPrice - i.FinalPrice) * i.Quantity,
                        FinalLineTotal = i.FinalPrice * i.Quantity,
                        HasPromoDiscount = i.HasPromoDiscount,
                        HasEventDiscount = i.HasEventDiscount,
                        AppliedPromoCode = i.AppliedPromoCode,
                        AppliedEvent = i.AppliedEventName
                    }).ToList(),
                    
                    //  VALIDATION
                    IsValidForCheckout = cartSummary.CanCheckout,
                    ValidationErrors = cartSummary.ValidationErrors,
                    
                    //  METADATA
                    Currency = "NPR",
                    CalculatedAt = DateTime.UtcNow,
                    TimeZone = "NPT"
                };
                
                return Result<OrderPricingSummaryDTO>.Success(orderSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating checkout pricing for user {UserId}", request.UserId);
                return Result<OrderPricingSummaryDTO>.Failure($"Error calculating checkout pricing: {ex.Message}");
            }
        }
    }
}