﻿using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Extension;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartStockService _stockService;
        private readonly IProductPricingService _productPricingService;
        private readonly ILogger<CartService> _logger;

        public CartService(
            IUnitOfWork unitOfWork,
            ICartStockService stockService,
            IProductPricingService productPricingService,
            ILogger<CartService> logger)
        {
            _unitOfWork = unitOfWork;
            _stockService = stockService;
            _productPricingService = productPricingService;
            _logger = logger;
        }

        public async Task<Result<CartItemDTO>> AddItemToCartAsync(int userId, AddToCartItemDTO request)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    _logger.LogInformation("Adding item to cart: UserId={UserId}, ProductId={ProductId}, Quantity={Quantity}",
                        userId, request.ProductId, request.Quantity);

                    // 1. Get product with current pricing (using your existing service)
                    var productPricing = await _productPricingService.GetCartPriceAsync(
                        request.ProductId, request.Quantity, userId);

                    if (!productPricing.IsStockAvailable)
                    {
                        return Result<CartItemDTO>.Failure($"Product with ID {request.ProductId} is not available");
                    }

                    // 2. Validate cart operation using your existing validation
                    var validation = productPricing.ValidateForCartOperation(request.Quantity);
                    if (!validation.IsValid)
                    {
                        return Result<CartItemDTO>.Failure(validation.ErrorMessage);
                    }

                    // 3. Check if item already exists in cart (using your repository pattern)
                    var existingCartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                        predicate: c => c.UserId == userId &&
                                      c.ProductId == request.ProductId &&
                                      !c.IsDeleted &&
                                      c.ExpiresAt > DateTime.UtcNow);

                    if (existingCartItem != null)
                    {
                        // Update existing item
                        return await UpdateExistingCartItem(existingCartItem, request.Quantity);
                    }

                    // 4. Reserve stock
                    var stockReservation = await _stockService.TryReserveStockAsync(
                        request.ProductId, request.Quantity, userId);

                    if (!stockReservation.Success)
                    {
                        return Result<CartItemDTO>.Failure(stockReservation.ErrorMessage);
                    }

                    // 5. Create cart item with current pricing
                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        ReservedPrice = productPricing.EffectivePrice,
                        AppliedEventId = productPricing.AppliedEventId,
                        EventDiscountAmount = productPricing.EventDiscountAmount,
                        IsStockReserved = true,
                        ReservationToken = stockReservation.ReservationToken,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    // 6. Save to database (using your repository pattern)
                    var createdCartItem = await _unitOfWork.CartItems.AddAsync(cartItem);
                    await _unitOfWork.SaveChangesAsync();

                    // 7. Convert to DTO
                    var cartItemDto = createdCartItem.ToDTO();

                    _logger.LogInformation(" Cart item added successfully: CartItemId={CartItemId}, UserId={UserId}",
                        cartItemDto.Id, userId);

                    return Result<CartItemDTO>.Success(cartItemDto, "Item added to cart successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item to cart: UserId={UserId}, ProductId={ProductId}",
                    userId, request.ProductId);
                return Result<CartItemDTO>.Failure($"Failed to add item to cart: {ex.Message}");
            }
        }

        /*        public async Task<CartSummaryDTO> GetCartSummaryAsync(int userId, CancellationToken cancellationToken = default)
                {
                    try
                    {

                        var cartItems = await _unitOfWork.CartItems.GetAllAsync(
                            predicate: c => c.UserId == userId && !c.IsDeleted,
                            includeProperties: "Product,Product.Images",
                            orderBy: o => o.OrderByDescending(x => x.CreatedAt),
                            cancellationToken: cancellationToken);

                        //  Separate expired and active items properly
                        var currentTime = DateTime.UtcNow;
                        var activeItems = cartItems.Where(c => !c.IsDeleted && c.ExpiresAt > currentTime).ToList();
                        var expiredItems = cartItems.Where(c => c.ExpiresAt <= currentTime).ToList();

                        // Validation errors collection 
                        var validationErrors = new List<string>();

                        // Check for out of stock items
                        var outOfStockItems = activeItems.Where(c => c.Product?.StockQuantity <= 0).ToList();
                        if (outOfStockItems.Any())
                        {
                            validationErrors.Add($"{outOfStockItems.Count} item(s) are out of stock");
                        }

                        // Check for expired items
                        if (expiredItems.Any())
                        {
                            validationErrors.Add($"{expiredItems.Count} item(s) have expired and need to be refreshed");
                        }

                        var summary = new CartSummaryDTO
                        {
                            UserId = userId,
                            TotalItems = activeItems.Count,
                            TotalQuantity = activeItems.Sum(c => c.Quantity),
                            SubTotal = activeItems.Sum(c => (c.ReservedPrice ?? 0) * c.Quantity),
                            TotalDiscount = activeItems.Sum(c => (c.EventDiscountAmount ?? 0) * c.Quantity),
                            EstimatedTotal = activeItems.Sum(c => ((c.ReservedPrice ?? 0) - (c.EventDiscountAmount ?? 0)) * c.Quantity),

                            //  All status properties now defined
                            CanCheckout = activeItems.Any() && !expiredItems.Any() && !outOfStockItems.Any(),
                            HasExpiredItems = expiredItems.Any(),
                            HasOutOfStockItems = outOfStockItems.Any(),
                            ExpiredItemsCount = expiredItems.Count,
                            EarliestExpiration = activeItems.Any() ? activeItems.Min(c => c.ExpiresAt) : null,

                            //  Validation errors
                            ValidationErrors = validationErrors,

                            // Items collection
                            Items = activeItems.Select(c => c.ToDTO()).ToList()
                        };

                        return summary;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get cart summary: UserId={UserId}", userId);
                        return new CartSummaryDTO
                        {
                            UserId = userId,
                            ValidationErrors = new List<string> { "Failed to load cart summary" }
                        };
                    }
                }
        */

        public async Task<Result<IEnumerable<CartItemDTO>>> GetCartItemsAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1.  Get basic cart items (minimal database load)
                var cartItems = await _unitOfWork.CartItems.GetAllAsync(
                    predicate: c => c.UserId == userId && !c.IsDeleted,
                    includeProperties: "Product,Product.Images,AppliedEvent",
                    cancellationToken: cancellationToken);

                if (!cartItems.Any())
                {
                    return Result<IEnumerable<CartItemDTO>>.Success(new List<CartItemDTO>(), "Cart is empty");
                }

                // 2.  Get current pricing for all products in one call (optimized)
                var productIds = cartItems.Select(c => c.ProductId).Distinct().ToList();
                var currentPricingInfo = await _productPricingService.GetEffectivePricesAsync(productIds, userId, cancellationToken);

                // 3.  Convert to DTOs with enhanced pricing (computed on-the-fly)
                var cartItemDtos = cartItems.Select(cartItem =>
                {
                    var currentPricing = currentPricingInfo.FirstOrDefault(p => p.ProductId == cartItem.ProductId);
                    return cartItem.ToEnhancedDTO(currentPricing);
                }).ToList();

                return Result<IEnumerable<CartItemDTO>>.Success(cartItemDtos, "Cart items retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to get cart items: UserId={UserId}", userId);
                return Result<IEnumerable<CartItemDTO>>.Failure($"Failed to get cart items: {ex.Message}");
            }
        }

        public async Task<Result<CartItemDTO>> UpdateCartItemAsync(int userId, int cartItemId, int newQuantity)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                   
                    var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                        predicate: c => c.Id == cartItemId && c.UserId == userId && !c.IsDeleted);

                    if (cartItem == null)
                    {
                        return Result<CartItemDTO>.Failure($"Cart item with ID {cartItemId} not found");
                    }

                    // Update stock reservation
                    var stockUpdated = await _stockService.UpdateReservationAsync(
                        cartItem.ProductId, cartItem.Quantity, newQuantity);

                    if (!stockUpdated)
                    {
                        return Result<CartItemDTO>.Failure("Failed to update stock reservation");
                    }

                    // Update cart item
                    cartItem.Quantity = newQuantity;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                    cartItem.ExpiresAt = DateTime.UtcNow.AddMinutes(30); // Reset expiration

                    await _unitOfWork.CartItems.UpdateAsync(cartItem);
                    await _unitOfWork.SaveChangesAsync();

                    var cartItemDto = cartItem.ToDTO();
                    return Result<CartItemDTO>.Success(cartItemDto, "Cart item updated successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to update cart item: CartItemId={CartItemId}", cartItemId);
                return Result<CartItemDTO>.Failure($"Failed to update cart item: {ex.Message}");
            }
        }

        public async Task<Result<CartItemDTO>> RemoveCartItemAsync(int userId, int cartItemId)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Using your repository pattern
                    var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
                        predicate: c => c.Id == cartItemId && c.UserId == userId && !c.IsDeleted);

                    if (cartItem == null)
                    {
                        return Result<CartItemDTO>.Failure($"Cart item with ID {cartItemId} not found");
                    }

                    // Release stock reservation
                    if (cartItem.IsStockReserved)
                    {
                        await _stockService.ReleaseStockAsync(cartItem.ProductId, cartItem.Quantity);
                    }

                    // Using your repository's RemoveAsync method
                    await _unitOfWork.CartItems.RemoveAsync(cartItem);
                    await _unitOfWork.SaveChangesAsync();

                    var cartItemDto = cartItem.ToDTO();
                    return Result<CartItemDTO>.Success(cartItemDto, "Cart item removed successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to remove cart item: CartItemId={CartItemId}", cartItemId);
                return Result<CartItemDTO>.Failure($"Failed to remove cart item: {ex.Message}");
            }
        }

        public async Task<Result<string>> ClearCartAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Using your repository pattern
                    var cartItems = await _unitOfWork.CartItems.GetAllAsync(
                        predicate: c => c.UserId == userId && !c.IsDeleted,
                        cancellationToken: cancellationToken);

                    if (!cartItems.Any()) // Check if cart is already empty
                    {
                        return Result<string>.Success("Cart is already empty.");
                    }

                    // Release stock reservations first
                    foreach (var item in cartItems)
                    {
                        if (item.IsStockReserved)
                        {
                            await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity, cancellationToken);
                        }
                    }

                    //  RemoveRangeAsync called once, outside the loop
                    await _unitOfWork.CartItems.RemoveRangeAsync(cartItems, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Cart cleared successfully: UserId={UserId}, RemovedItems={Count}",
                        userId, cartItems.Count());

                    return Result<string>.Success($"Cart cleared successfully. Removed {cartItems.Count()} items.");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cart: UserId={UserId}", userId);
                return Result<string>.Failure($"Failed to clear cart: {ex.Message}");
            }
        }

        public async Task<bool> ValidateCartAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
               
                var cartItems = await _unitOfWork.CartItems.GetAllAsync(
                    predicate: c => c.UserId == userId && !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow,
                    cancellationToken: cancellationToken);

                if (cartItems?.Any() == true) 
                {
                    foreach (var item in cartItems)
                    {
                        var validation = await _productPricingService.ValidateCartPriceAsync(
                            item.ProductId, item.ReservedPrice , userId);

                        if (!validation)
                        {
                            _logger.LogWarning(" Cart validation failed for item: ProductId={ProductId}, UserId={UserId}",
                                item.ProductId, userId);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to validate cart: UserId={UserId}", userId);
                return false;
            }
        }

        private async Task<Result<CartItemDTO>> UpdateExistingCartItem(CartItem existingItem, int additionalQuantity)
        {
            var newQuantity = existingItem.Quantity + additionalQuantity;

            // Update stock reservation
            var stockUpdated = await _stockService.UpdateReservationAsync(
                existingItem.ProductId, existingItem.Quantity, newQuantity);

            if (!stockUpdated)
            {
                return Result<CartItemDTO>.Failure("Failed to update stock reservation");
            }

            // Update cart item
            existingItem.Quantity = newQuantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
            existingItem.ExpiresAt = DateTime.UtcNow.AddMinutes(30); // Reset expiration

            await _unitOfWork.CartItems.UpdateAsync(existingItem);
            await _unitOfWork.SaveChangesAsync();

            var cartItemDto = existingItem.ToDTO();
            return Result<CartItemDTO>.Success(cartItemDto, "Cart item quantity updated successfully");
        }
    }
}