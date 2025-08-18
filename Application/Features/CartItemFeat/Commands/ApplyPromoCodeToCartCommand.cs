using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Application.Dto.ShippingDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Commands
{
    public record ApplyPromoCodeToCartCommand(
         string Code,
         int UserId
         
     ) : IRequest<Result<PromoCodeDiscountResultDTO>>;

     public class ApplyPromoCodeToCartCommandHandler : IRequestHandler<ApplyPromoCodeToCartCommand, Result<PromoCodeDiscountResultDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly ILogger<ApplyPromoCodeToCartCommandHandler> _logger;
        private readonly IServiceProvider _serviceProvider;
        public ApplyPromoCodeToCartCommandHandler(
            IUnitOfWork unitOfWork,
            INepalTimeZoneService nepalTimeZoneService,
            ICartItemRepository cartItemRepository,
            ILogger<ApplyPromoCodeToCartCommandHandler> logger,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _nepalTimeZoneService = nepalTimeZoneService;
            _cartItemRepository = cartItemRepository;
            _logger = logger;
            _serviceProvider = serviceProvider;        
            _logger = logger;
        }
        
        public async Task<Result<PromoCodeDiscountResultDTO>> Handle(ApplyPromoCodeToCartCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //  STEP 1: Get user's active cart items
                var cartItems = await _cartItemRepository.GetByUserIdAsync(request.UserId);
                if (!cartItems.Any())
                {
                    return Result<PromoCodeDiscountResultDTO>.Failure("No items in cart to apply promo code");
                }
                
                //  STEP 2: Get and validate promo code
                var promoCode = await _unitOfWork.PromoCodes.GetAsync(
                    predicate: p => p.Code.ToLower() == request.Code.ToLower() && !p.IsDeleted,
                    includeProperties: "PromoCodeUsages",
                    cancellationToken: cancellationToken
                );
                
                if (promoCode == null)
                {
                    return Result<PromoCodeDiscountResultDTO>.Failure("Promo code not found");
                }
                
                //  STEP 3: Validate promo code against cart
                var validationResult = await ValidatePromoCodeAgainstCartAsync(promoCode, cartItems, request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return Result<PromoCodeDiscountResultDTO>.Success(validationResult);
                }
                
                //  STEP 4: Calculate discounts for qualifying items
                var discountResult = await CalculateCartDiscountsAsync(promoCode, cartItems, request.UserId, cancellationToken);

                //  STEP 5: Update cart item prices if requested

                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // update cart item prices
                    await UpdateCartItemPricesAsync(cartItems, discountResult, promoCode, cancellationToken);

                    // record promo code usage
                    await RecordPromoCodeUsageAsync(promoCode, request.UserId, discountResult.DiscountAmount, cancellationToken);

                    // update promo code usage count
                    promoCode.IncrementUsageCount();
                    await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);                  


                });

                
                
                _logger.LogInformation(" Promo code '{Code}' applied to cart for user {UserId}, total discount: Rs.{Discount}", 
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
        
   private async Task<PromoCodeDiscountResultDTO> ValidatePromoCodeAgainstCartAsync(
            PromoCode promoCode, 
            IEnumerable<CartItem> cartItems, 
            ApplyPromoCodeToCartCommand request,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var cartSubtotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity);

            var currentShippingCost = await CalculateCurrentShippingCostAsync(cartSubtotal, request.UserId, cancellationToken);

            _logger.LogInformation("Promo validation: Subtotal=Rs.{Subtotal}, CurrentShipping=Rs.{Shipping}", 
            cartSubtotal, currentShippingCost);

            //  TIMEZONE-AWARE VALIDITY CHECK
            if (!promoCode.IsValidAtTime(_nepalTimeZoneService.GetUtcCurrentTime()))
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
            
            //  REAL-TIME USER-SPECIFIC VALIDATION (Query database for fresh usage data)
            if (promoCode.MaxUsagePerUser.HasValue)
            {
                var userUsageCount = await _unitOfWork.PromoCodeUsages.CountAsync(
                    predicate: u => u.PromoCodeId == promoCode.Id && 
                                   u.UserId == request.UserId && 
                                   !u.IsDeleted,
                    cancellationToken: cancellationToken
                );
                
                _logger.LogInformation(" User {UserId} has used promo code '{Code}' {UsageCount}/{MaxUsage} times", 
                    request.UserId, promoCode.Code, userUsageCount, promoCode.MaxUsagePerUser.Value);
                
                if (userUsageCount >= promoCode.MaxUsagePerUser.Value)
                {
                    errors.Add($"You have already used this promo code the maximum number of times ({promoCode.MaxUsagePerUser})");
                }
            }

            //  CART-SPECIFIC VALIDATION
            var totalAmount = promoCode.ApplyToShipping ? cartSubtotal + currentShippingCost : cartSubtotal;
            
            if (promoCode.MinOrderAmount.HasValue && totalAmount < promoCode.MinOrderAmount.Value)
            {
                errors.Add($"Minimum order amount of Rs.{promoCode.MinOrderAmount.Value:F2} required (current: Rs.{totalAmount:F2})");
            }
            
            //  CATEGORY-SPECIFIC VALIDATION
            if (promoCode.CategoryId.HasValue)
            {
                var qualifyingItems = cartItems.Where(c => c.Product?.CategoryId == promoCode.CategoryId.Value);
                if (!qualifyingItems.Any())
                {
                    errors.Add($"This promo code only applies to items from the {promoCode.Category?.Name ?? "specified"} category");
                }
            }
            
            if (errors.Any())
            {
                return CreateInvalidResult(errors.First(), cartItems.ToList(), currentShippingCost, errors);
            }
            
            return new PromoCodeDiscountResultDTO { IsValid = true };
        }
        
        /// <summary>
        ///  NEW METHOD: Calculate current shipping cost for cart total
        /// </summary>
        private async Task<decimal> CalculateCurrentShippingCostAsync(decimal cartSubtotal, int userId, CancellationToken cancellationToken)
        {
            try
            {
                // Get shipping service through DI
                var shippingService = _serviceProvider.GetRequiredService<IShippingService>();
                
                var shippingRequest = new ShippingRequestDTO
                {
                    UserId = userId,
                    OrderTotal = cartSubtotal
                };
                
                var shippingResult = await shippingService.CalculateShippingAsync(shippingRequest, cancellationToken);
                
                if (shippingResult.Succeeded && shippingResult.Data != null)
                {
                    _logger.LogInformation("📦 Calculated shipping: Rs.{Cost} for subtotal Rs.{Subtotal}", 
                        shippingResult.Data.FinalShippingCost, cartSubtotal);
                    return shippingResult.Data.FinalShippingCost;
                }
                
                _logger.LogWarning(" Failed to calculate shipping, defaulting to 0");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping for promo code validation");
                return 0;
            }
        }
                
         /// <summary>
        ///  RECORD PROMO CODE USAGE IN DATABASE
        /// </summary>
        private async Task RecordPromoCodeUsageAsync(
            PromoCode promoCode,
            int userId,
            decimal totalDiscountAmount,
            CancellationToken cancellationToken)
        {
            try
            {
                var usage = new PromoCodeUsage
                {
                    PromoCodeId = promoCode.Id,
                    UserId = userId,
                    DiscountAmount = totalDiscountAmount,
                    UsageContext = "Cart Application",
                    UsedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.PromoCodeUsages.AddAsync(usage, cancellationToken);

                _logger.LogInformation(" Recorded promo code usage: UserId={UserId}, PromoCodeId={PromoCodeId}, Discount=Rs.{Discount}",
                    userId, promoCode.Id, totalDiscountAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to record promo code usage for user {UserId}, promo code {PromoCodeId}",
                    userId, promoCode.Id);
                throw; // Re-throw to ensure transaction rollback
            }
        }
        private async Task<PromoCodeDiscountResultDTO> CalculateCartDiscountsAsync(PromoCode promoCode, IEnumerable<CartItem> cartItems, int userId,CancellationToken cancellationToken)
        {
            var qualifyingItems = GetQualifyingCartItems(promoCode, cartItems);
            var affectedCartItems = new List<CartItemDiscountDTO>();

            var cartSubtotal = cartItems.Sum(c => c.ReservedPrice * c.Quantity);
            
            //  RECALCULATE SHIPPING COST BASED ON CURRENT SUBTOTAL
            var originalShippingCost = await CalculateCurrentShippingCostAsync(cartSubtotal, userId, cancellationToken);
            
            decimal totalDiscount = 0;
            decimal shippingDiscount = 0;

            //  CALCULATE ITEM DISCOUNTS
            foreach (var cartItem in qualifyingItems)
            {
                var itemTotal = cartItem.ReservedPrice * cartItem.Quantity; // Use per-item total
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
                    DiscountAmount = itemDiscount / cartItem.Quantity, // Per unit discount
                    DiscountedPrice = cartItem.ReservedPrice - (itemDiscount / cartItem.Quantity),
                    TotalSavings = itemDiscount
                });
            }

            //  HANDLE FREE SHIPPING
            if (promoCode.Type == PromoCodeType.FreeShipping && promoCode.ApplyToShipping)
            {
                shippingDiscount = originalShippingCost;
                totalDiscount += shippingDiscount;
            }
            else if (promoCode.ApplyToShipping && promoCode.Type == PromoCodeType.Percentage)
            {
                // Apply percentage discount to shipping as well
                shippingDiscount = (originalShippingCost * promoCode.DiscountValue) / 100;
                totalDiscount += shippingDiscount;
            }

            //  APPLY MAX DISCOUNT LIMIT
            if (promoCode.MaxDiscountAmount.HasValue && totalDiscount > promoCode.MaxDiscountAmount.Value)
            {
                var reductionFactor = promoCode.MaxDiscountAmount.Value / totalDiscount;
                var originalTotalDiscount = totalDiscount;
                totalDiscount = promoCode.MaxDiscountAmount.Value;

                // Reduce item discounts proportionally
                foreach (var item in affectedCartItems)
                {
                    item.DiscountAmount *= reductionFactor;
                    item.TotalSavings *= reductionFactor;
                    item.DiscountedPrice = item.OriginalPrice - item.DiscountAmount;
                }

                // Reduce shipping discount proportionally
                if (shippingDiscount > 0)
                {
                    shippingDiscount *= reductionFactor;
                }

                _logger.LogInformation("⚡ Applied max discount limit: Rs.{Original} → Rs.{Limited}", 
                    originalTotalDiscount, totalDiscount);
            }

            //  CALCULATE FINAL AMOUNTS
            var itemsDiscount = totalDiscount - shippingDiscount;
            var finalSubtotal = cartSubtotal - itemsDiscount;
            var finalShippingCost = Math.Max(0, originalShippingCost - shippingDiscount);
            var finalTotal = finalSubtotal + finalShippingCost;

            _logger.LogInformation("💰 Promo calculation: Subtotal Rs.{Sub} - ItemDiscount Rs.{ItemDisc} = Rs.{FinalSub}, Shipping Rs.{Ship} - ShipDiscount Rs.{ShipDisc} = Rs.{FinalShip}, Total Rs.{Total}", 
                cartSubtotal, itemsDiscount, finalSubtotal, originalShippingCost, shippingDiscount, finalShippingCost, finalTotal);

            return new PromoCodeDiscountResultDTO
            {
                IsValid = true,
                PromoCodeId = promoCode.Id,
                Code = promoCode.Code,
                Name = promoCode.Name,
                Type = promoCode.Type,
                OriginalSubtotal = cartSubtotal,
                OriginalShipping = originalShippingCost,
                OriginalTotal = cartSubtotal + originalShippingCost,
                DiscountAmount = itemsDiscount, // Only item discount, not including shipping
                ShippingDiscount = shippingDiscount,
                FinalSubtotal = finalSubtotal,
                FinalShipping = finalShippingCost,
                FinalTotal = finalTotal,
                AffectedCartItems = affectedCartItems,
                QualifyingItemsCount = qualifyingItems.Count,
                QualifyingCategoryIds = promoCode.CategoryId.HasValue ? new List<int> { promoCode.CategoryId.Value } : new(),
                AppliedToShipping = promoCode.ApplyToShipping,
                CanStackWithEvents = promoCode.StackableWithEvents,
                RemainingUsage = promoCode.MaxTotalUsage.HasValue ? promoCode.MaxTotalUsage.Value - promoCode.CurrentUsageCount : null,
                UserRemainingUsage = promoCode.MaxUsagePerUser.HasValue ? promoCode.MaxUsagePerUser.Value - promoCode.PromoCodeUsages.Count(u => u.UserId == userId) : null,
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
        
        private List<CartItem> GetQualifyingCartItems(PromoCode promoCode, IEnumerable<CartItem> cartItems)
        {
            if (!promoCode.CategoryId.HasValue)
                return cartItems.ToList();
            
            return cartItems.Where(c => c.Product?.CategoryId == promoCode.CategoryId.Value).ToList();
        }
        
        private async Task UpdateCartItemPricesAsync(IEnumerable<CartItem> cartItems, PromoCodeDiscountResultDTO discountResult, PromoCode promoCode, CancellationToken cancellationToken)
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
