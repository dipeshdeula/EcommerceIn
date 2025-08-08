using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Commands
{
    public record ApplyPromoCodeToCartCommand(
         string Code,
         int UserId,
         List<int>? CartItemIds = null,
         decimal? OrderTotal = null,
         decimal? ShippingCost = null,
         string? CustomerTier = null,
         string? DeliveryAddress = null,
         bool IsCheckout = false,
         bool UpdateCartPrices = true
     ) : IRequest<Result<PromoCodeDiscountResultDTO>>;


     public class ApplyPromoCodeToCartCommandHandler : IRequestHandler<ApplyPromoCodeToCartCommand, Result<PromoCodeDiscountResultDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly ILogger<ApplyPromoCodeToCartCommandHandler> _logger;
        
        public ApplyPromoCodeToCartCommandHandler(
            IUnitOfWork unitOfWork,
            INepalTimeZoneService nepalTimeZoneService,
            ILogger<ApplyPromoCodeToCartCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _nepalTimeZoneService = nepalTimeZoneService;
            _logger = logger;
        }
        
        public async Task<Result<PromoCodeDiscountResultDTO>> Handle(ApplyPromoCodeToCartCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ STEP 1: Get user's active cart items
                var cartItems = await GetUserCartItemsAsync(request.UserId, request.CartItemIds, cancellationToken);
                if (!cartItems.Any())
                {
                    return Result<PromoCodeDiscountResultDTO>.Failure("No items in cart to apply promo code");
                }
                
                // ✅ STEP 2: Get and validate promo code
                var promoCode = await _unitOfWork.PromoCodes.GetAsync(
                    predicate: p => p.Code.ToLower() == request.Code.ToLower() && !p.IsDeleted,
                    includeProperties: "PromoCodeUsages,Category",
                    cancellationToken: cancellationToken
                );
                
                if (promoCode == null)
                {
                    return Result<PromoCodeDiscountResultDTO>.Success(CreateInvalidResult("Promo code not found", cartItems, request.ShippingCost ?? 0));
                }
                
                // ✅ STEP 3: Validate promo code against cart
                var validationResult = await ValidatePromoCodeAgainstCartAsync(promoCode, cartItems, request);
                if (!validationResult.IsValid)
                {
                    return Result<PromoCodeDiscountResultDTO>.Success(validationResult);
                }
                
                // ✅ STEP 4: Calculate discounts for qualifying items
                var discountResult = CalculateCartDiscounts(promoCode, cartItems, request.ShippingCost ?? 0);
                
                // ✅ STEP 5: Update cart item prices if requested
                if (request.UpdateCartPrices)
                {
                    await UpdateCartItemPricesAsync(cartItems, discountResult, promoCode, cancellationToken);
                }
                
                _logger.LogInformation("✅ Promo code '{Code}' applied to cart for user {UserId}, total discount: Rs.{Discount}", 
                    request.Code, request.UserId, discountResult.DiscountAmount);
                
                return Result<PromoCodeDiscountResultDTO>.Success(discountResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promo code '{Code}' to cart for user {UserId}", 
                    request.Code, request.UserId);
                return Result<PromoCodeDiscountResultDTO>.Failure($"Error applying promo code: {ex.Message}");
            }
        }
        
        private async Task<List<CartItem>> GetUserCartItemsAsync(int userId, List<int>? specificCartItemIds, CancellationToken cancellationToken)
        {
            if (specificCartItemIds?.Any() == true)
            {
                var result = await _unitOfWork.CartItems.GetAllAsync(
                    predicate: c => c.UserId == userId && 
                                  !c.IsDeleted && 
                                  c.ExpiresAt > DateTime.UtcNow &&
                                  specificCartItemIds.Contains(c.Id),
                    includeProperties: "Product,Product.Category",
                    cancellationToken: cancellationToken
                );
                return result.ToList();
            }
            else
            {
                var result = await _unitOfWork.CartItems.GetAllAsync(
                    predicate: c => c.UserId == userId && !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow,
                    includeProperties: "Product,Product.Category",
                    cancellationToken: cancellationToken
                );
                return result.ToList();
            }
        }
        
        private async Task<PromoCodeDiscountResultDTO> ValidatePromoCodeAgainstCartAsync(PromoCode promoCode, List<CartItem> cartItems, ApplyPromoCodeToCartCommand request)
        {
            var errors = new List<string>();
            
            // ✅ TIMEZONE-AWARE VALIDITY CHECK
            if (!promoCode.IsValidNow(_nepalTimeZoneService))
            {
                if (!promoCode.IsActive)
                    errors.Add("This promo code is currently inactive");
                else if (!promoCode.IsStartedAtTime(_nepalTimeZoneService.GetUtcCurrentTime()))
                {
                    var startNepal = _nepalTimeZoneService.ConvertFromUtcToNepal(promoCode.StartDate);
                    errors.Add($"This promo code is not valid until {startNepal:MMM dd, yyyy HH:mm} NPT");
                }
                else if (promoCode.IsExpiredAtTime(_nepalTimeZoneService.GetUtcCurrentTime()))
                {
                    var endNepal = _nepalTimeZoneService.ConvertFromUtcToNepal(promoCode.EndDate);
                    errors.Add($"This promo code expired on {endNepal:MMM dd, yyyy HH:mm} NPT");
                }
                else if (promoCode.MaxTotalUsage.HasValue && promoCode.CurrentUsageCount >= promoCode.MaxTotalUsage.Value)
                    errors.Add("This promo code has reached its usage limit");
            }
            
            // ✅ USER-SPECIFIC VALIDATION
            if (!promoCode.CanUserUse(request.UserId))
            {
                errors.Add($"You have already used this promo code the maximum number of times ({promoCode.MaxUsagePerUser})");
            }
            
            // ✅ CART-SPECIFIC VALIDATION
            var cartSubtotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity);
            var totalAmount = promoCode.ApplyToShipping ? cartSubtotal + (request.ShippingCost ?? 0) : cartSubtotal;
            
            if (promoCode.MinOrderAmount.HasValue && totalAmount < promoCode.MinOrderAmount.Value)
            {
                errors.Add($"Minimum order amount of Rs.{promoCode.MinOrderAmount.Value:F2} required (current: Rs.{totalAmount:F2})");
            }
            
            // ✅ CATEGORY VALIDATION
            if (promoCode.CategoryId.HasValue)
            {
                var hasMatchingCategory = cartItems.Any(c => c.Product?.CategoryId == promoCode.CategoryId.Value);
                if (!hasMatchingCategory)
                {
                    errors.Add("This promo code is not valid for any items in your cart");
                }
            }
            
            // ✅ CUSTOMER TIER VALIDATION
            if (!string.IsNullOrEmpty(promoCode.CustomerTier) && 
                promoCode.CustomerTier != request.CustomerTier &&
                promoCode.CustomerTier != "All")
            {
                errors.Add($"This promo code is only valid for {promoCode.CustomerTier} customers");
            }
            
            if (errors.Any())
            {
                return CreateInvalidResult(errors.First(), cartItems, request.ShippingCost ?? 0, errors);
            }
            
            return new PromoCodeDiscountResultDTO { IsValid = true };
        }
        
        private PromoCodeDiscountResultDTO CalculateCartDiscounts(PromoCode promoCode, List<CartItem> cartItems, decimal shippingCost)
        {
            var qualifyingItems = GetQualifyingCartItems(promoCode, cartItems);
            var affectedCartItems = new List<CartItemDiscountDTO>();
            
            decimal totalDiscount = 0;
            decimal shippingDiscount = 0;
            
            foreach (var cartItem in qualifyingItems)
            {
                var itemTotal = cartItem.ReservedPrice * cartItem.Quantity;
                var itemDiscount = promoCode.Type switch
                {
                    PromoCodeType.Percentage => (itemTotal * promoCode.DiscountValue) / 100,
                    PromoCodeType.FixedAmount => Math.Min(promoCode.DiscountValue, itemTotal),
                    PromoCodeType.FreeShipping => 0,
                    _ => 0
                };
                
                if (promoCode.MaxDiscountAmount.HasValue)
                {
                    itemDiscount = Math.Min(itemDiscount, promoCode.MaxDiscountAmount.Value / qualifyingItems.Count);
                }
                
                totalDiscount += itemDiscount;
                
                affectedCartItems.Add(new CartItemDiscountDTO
                {
                    CartItemId = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product?.Name ?? "Unknown Product",
                    Quantity = cartItem.Quantity,
                    OriginalPrice = cartItem.ReservedPrice,
                    DiscountAmount = itemDiscount / cartItem.Quantity,
                    DiscountedPrice = cartItem.ReservedPrice - (itemDiscount / cartItem.Quantity),
                    TotalSavings = itemDiscount
                });
            }
            
            if (promoCode.Type == PromoCodeType.FreeShipping)
            {
                shippingDiscount = shippingCost;
                totalDiscount = shippingCost;
            }
            
            if (promoCode.MaxDiscountAmount.HasValue && totalDiscount > promoCode.MaxDiscountAmount.Value)
            {
                var reductionFactor = promoCode.MaxDiscountAmount.Value / totalDiscount;
                totalDiscount = promoCode.MaxDiscountAmount.Value;
                
                foreach (var item in affectedCartItems)
                {
                    item.DiscountAmount *= reductionFactor;
                    item.TotalSavings *= reductionFactor;
                    item.DiscountedPrice = item.OriginalPrice - item.DiscountAmount;
                }
            }
            
            var cartSubtotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity);
            
            return new PromoCodeDiscountResultDTO
            {
                IsValid = true,
                PromoCodeId = promoCode.Id,
                Code = promoCode.Code,
                Name = promoCode.Name,
                Type = promoCode.Type,
                OriginalSubtotal = cartSubtotal,
                OriginalShipping = shippingCost,
                OriginalTotal = cartSubtotal + shippingCost,
                DiscountAmount = totalDiscount - shippingDiscount,
                ShippingDiscount = shippingDiscount,
                FinalSubtotal = cartSubtotal - (totalDiscount - shippingDiscount),
                FinalShipping = shippingCost - shippingDiscount,
                FinalTotal = cartSubtotal + shippingCost - totalDiscount,
                AffectedCartItems = affectedCartItems,
                QualifyingItemsCount = qualifyingItems.Count,
                QualifyingCategoryIds = promoCode.CategoryId.HasValue ? new List<int> { promoCode.CategoryId.Value } : new(),
                AppliedToShipping = promoCode.ApplyToShipping,
                CanStackWithEvents = promoCode.StackableWithEvents,
                RemainingUsage = promoCode.MaxTotalUsage.HasValue ? promoCode.MaxTotalUsage.Value - promoCode.CurrentUsageCount : null,
                UserRemainingUsage = promoCode.MaxUsagePerUser.HasValue ? promoCode.MaxUsagePerUser.Value - promoCode.PromoCodeUsages.Count(u => u.UserId == cartItems.First().UserId) : null,
                FormattedDiscount = promoCode.Type switch
                {
                    PromoCodeType.Percentage => $"{promoCode.DiscountValue}% OFF",
                    PromoCodeType.FixedAmount => $"Rs.{promoCode.DiscountValue} OFF",
                    PromoCodeType.FreeShipping => "FREE SHIPPING",
                    _ => "DISCOUNT APPLIED"
                },
                FormattedSavings = $"You saved Rs.{totalDiscount:F2}!",
                SuccessMessage = $"Promo code '{promoCode.Code}' applied successfully!",
                AppliedAt = DateTime.UtcNow,
                TimeZoneInfo = "NPT"
            };
        }
        
        private List<CartItem> GetQualifyingCartItems(PromoCode promoCode, List<CartItem> cartItems)
        {
            if (!promoCode.CategoryId.HasValue)
                return cartItems;
            
            return cartItems.Where(c => c.Product?.CategoryId == promoCode.CategoryId.Value).ToList();
        }
        
        private async Task UpdateCartItemPricesAsync(List<CartItem> cartItems, PromoCodeDiscountResultDTO discountResult, PromoCode promoCode, CancellationToken cancellationToken)
        {
            foreach (var affectedItem in discountResult.AffectedCartItems)
            {
                var cartItem = cartItems.First(c => c.Id == affectedItem.CartItemId);
                cartItem.ApplyPromoCode(promoCode, affectedItem.DiscountAmount);
                await _unitOfWork.CartItems.UpdateAsync(cartItem, cancellationToken);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        private PromoCodeDiscountResultDTO CreateInvalidResult(string errorMessage, List<CartItem> cartItems, decimal shippingCost, List<string>? allErrors = null)
        {
            var cartSubtotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity);
            
            return new PromoCodeDiscountResultDTO
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                ValidationErrors = allErrors ?? new List<string> { errorMessage },
                OriginalSubtotal = cartSubtotal,
                OriginalShipping = shippingCost,
                OriginalTotal = cartSubtotal + shippingCost,
                FinalSubtotal = cartSubtotal,
                FinalShipping = shippingCost,
                FinalTotal = cartSubtotal + shippingCost,
                DiscountAmount = 0,
                ShippingDiscount = 0
            };
        }
    }

    
}
