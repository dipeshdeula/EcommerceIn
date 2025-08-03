using Application.Common;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class EventUsageService : IEventUsageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EventUsageService> _logger;

        public EventUsageService(IUnitOfWork unitOfWork, ILogger<EventUsageService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> CanUserUseEventAsync(int eventId, int userId)
        {
            try
            {
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetAsync(
                    predicate: e => e.Id == eventId && e.IsActive && !e.IsDeleted);

                if (bannerEvent == null)
                {
                    _logger.LogWarning("Event {EventId} not found or inactive", eventId);
                    return false;
                }

                // check if event is within valid time range
                var now = DateTime.UtcNow;
                if (now < bannerEvent.StartDate || now > bannerEvent.EndDate)
                {
                    _logger.LogWarning("Event {EventId} is outside valid time range. Current: {Now}, Valid: {Start} - {End}",
                         eventId, now, bannerEvent.StartDate, bannerEvent.EndDate);
                    return false;
                }

                // Check global usage limit
                if (bannerEvent.CurrentUsageCount >= bannerEvent.MaxUsageCount)
                {
                    _logger.LogWarning("Event {EventId} has reached global usage limit: {Current}/{Max}",
                       eventId, bannerEvent.CurrentUsageCount, bannerEvent.MaxUsageCount);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can use event {EventId}", userId, eventId);
                return false;
            }
        }

        // Get total usage including cart items and completed orders (all products)
        public async Task<int> GetTotalUserEventUsageAsync(int eventId, int userId)
        {
            try
            {
                // step 1 : Get completed order usage
                var completedUsage = await _unitOfWork.EventUsages.GetUsageCountByEventAndUserAsync(eventId, userId);

                // step 2 : Get current cart items with this event (all products)
                var cartUsage = await _unitOfWork.CartItems.CountActiveCartItemsByEventAsync(userId, eventId, 0); // 0 = all products
                var totalUsage = completedUsage + cartUsage;

                _logger.LogDebug("User {UserId} event {EventId} usage: Completed={Completed}, InCart={InCart}, Total={Total}",
                    userId, eventId, completedUsage, cartUsage, totalUsage);

                return totalUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total user event usage: User {UserId}, Event {EventId}", userId, eventId);
                return int.MaxValue; // Fail safe - assume limit reached
            }
        }

        // Fixed: Product-specific event usage count
        public async Task<int> GetUserEventUsageCountAsync(int eventId, int userId, int productId)
        {
            return await GetUserProductEventUsageCountAsync(eventId, userId, productId);
        }

        public async Task<int> GetUserProductEventUsageCountAsync(int eventId, int userId, int productId)
        {
            try
            {
                // STEP 1: Count completed orders for this specific product
                var completedUsage = await _unitOfWork.EventUsages.CountAsync(
                    predicate: u => u.BannerEventId == eventId &&
                                u.UserId == userId &&
                                u.IsActive &&
                                !u.IsDeleted);

                // STEP 2: Count EXISTING cart items for this specific product
                var existingCartUsage = await _unitOfWork.CartItems.CountActiveCartItemsByEventAsync(userId, eventId, productId);

                var totalCurrentUsage = completedUsage + existingCartUsage;

                _logger.LogDebug("User {UserId} product {ProductId} event {EventId} usage: Completed={Completed}, ExistingCart={ExistingCart}, Total={Total}",
                    userId, productId, eventId, completedUsage, existingCartUsage, totalCurrentUsage);

                return totalCurrentUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user product event usage: User {UserId}, Product {ProductId}, Event {EventId}", userId, productId, eventId);
                return 0;
            }
        }

        // check if user can add specific quantity to cart (global)
        public async Task<Result<string>> CanUserAddQuantityToCartAsync(int eventId, int userId, int requestedQuantity)
        {
            try
            {
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetAsync(
                    predicate: e => e.Id == eventId && e.IsActive && !e.IsDeleted);

                if (bannerEvent == null)
                {
                    return Result<string>.Failure("Event not found or inactive");
                }

                // Check if event is valid
                if (!await CanUserUseEventAsync(eventId, userId))
                {
                    return Result<string>.Failure("User cannot use this event at this time, limit reached");
                }

                // Check if adding this quantity would exceed limits
                if (bannerEvent.MaxUsagePerUser > 0)
                {
                    var currentUsage = await GetTotalUserEventUsageAsync(eventId, userId);
                    var wouldBeTotal = currentUsage + requestedQuantity;

                    _logger.LogInformation("User {UserId} event {EventId}: Current={Current}, Requested={Requested}, WouldBe={WouldBe}, Max={Max}",
                    userId, eventId, currentUsage, requestedQuantity, wouldBeTotal, bannerEvent.MaxUsagePerUser);

                    if (wouldBeTotal > bannerEvent.MaxUsagePerUser)
                    {
                        var remainingUsage = bannerEvent.MaxUsagePerUser - currentUsage;
                        return Result<string>.Failure(
                            $"You can only add {remainingUsage} more items with this event discount. " +
                            $"You have used {currentUsage} out of {bannerEvent.MaxUsagePerUser} allowed uses.");
                    }
                }

                _logger.LogInformation("User {UserId} can add {Quantity} items for event {EventId}",
               userId, requestedQuantity, eventId);

                return Result<string>.Success("User can add requested quantity");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can add quantity to cart: User {UserId}, Event {EventId}, Quantity {Quantity}",
                    userId, eventId, requestedQuantity);
                return Result<string>.Failure("Error validating cart addition");
            }
        }

        // Fixed: Product-specific cart validation
        public async Task<Result<bool>> CanUserAddQuantityToCartForProductAsync(int eventId, int userId, int productId, int requestedQuantity)
        {
            try
            {
                var eventDetails = await _unitOfWork.BannerEventSpecials.GetAsync(
                    predicate: e => e.Id == eventId && e.IsActive && !e.IsDeleted);

                if (eventDetails == null)
                {
                    return Result<bool>.Failure("Event not found or inactive");
                }

                // Count product-specific cart items with this event
                var currentProductQuantity = await GetUserProductEventUsageCountAsync(eventId, userId, productId);
                var wouldBeQuantity = currentProductQuantity + requestedQuantity;

                _logger.LogInformation("User {UserId} product {ProductId} event {EventId}: Current={Current}, Requested={Requested}, WouldBe={WouldBe}, Max={Max}",
                    userId, productId, eventId, currentProductQuantity, requestedQuantity, wouldBeQuantity, eventDetails.MaxUsagePerUser);

                if (wouldBeQuantity > eventDetails.MaxUsagePerUser)
                {
                    var remaining = eventDetails.MaxUsagePerUser - currentProductQuantity;
                    return Result<bool>.Failure($"You can only add {remaining} more items of this product with this event discount. You have used {currentProductQuantity} out of {eventDetails.MaxUsagePerUser} allowed uses for this product.");
                }

                _logger.LogInformation("User {UserId} can add {RequestedQuantity} items of product {ProductId} for event {EventId}",
                    userId, requestedQuantity, productId, eventId);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product-specific event usage for user {UserId}, product {ProductId}, event {EventId}",
                    userId, productId, eventId);
                return Result<bool>.Failure("Error validating event usage");
            }
        }

        public async Task<Result<string>> RecordEventUsageAsync(int eventId, int userId, int orderId, decimal discountApplied)
        {
            try
            {
                // Validate inputs
                if (eventId <= 0) return Result<string>.Failure("Invalid event ID");
                if (userId <= 0) return Result<string>.Failure("Invalid user ID");
                if (orderId <= 0) return Result<string>.Failure("Invalid order ID");
                if (discountApplied < 0) return Result<string>.Failure("Discount cannot be negative");

                // Check if user can still use the event
                var canUse = await CanUserUseEventAsync(eventId, userId);
                if (!canUse)
                {
                    return Result<string>.Failure("User cannot use this event at this time");
                }

                // Check for duplicate usage on same order
                var existingUsage = await _unitOfWork.EventUsages.GetAllAsync(
                    predicate: u => u.OrderId == orderId && u.BannerEventId == eventId && !u.IsDeleted,
                    cancellationToken: default
                    );

                if (existingUsage.Any())
                {
                    return Result<string>.Failure("Event already applied to this order");
                }

                // Database Operation
                var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var usage = new EventUsage
                    {
                        BannerEventId = eventId,
                        UserId = userId,
                        OrderId = orderId,
                        DiscountApplied = discountApplied,
                        UsedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false
                    };
                    await _unitOfWork.EventUsages.AddAsync(usage);

                    // Update banner event usage count
                    var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(eventId);
                    if (bannerEvent != null)
                    {
                        bannerEvent.CurrentUsageCount++;
                        bannerEvent.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.BannerEventSpecials.UpdateAsync(bannerEvent);
                    }

                    // save all changes 
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Recorded event usage: User {UserId},Event {EventId},Order {OrderId},Discount Rs.{Discount}",
                        userId, eventId, orderId, discountApplied);

                    return "Event usage recorded successfully";
                });

                return Result<string>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording event usage: User {UserId}, Event {EventId}, Order {OrderId}",
                    userId, eventId, orderId);
                return Result<string>.Failure($"Failed to record event usage: {ex.Message}");
            }
        }

        public async Task<bool> HasUserReachedEventLimitAsync(int eventId, int userId)
        {
            try
            {
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetAsync(
                    predicate: e => e.Id == eventId);

                if (bannerEvent == null) return true;

                var userUsageCount = await GetTotalUserEventUsageAsync(eventId, userId);
                return userUsageCount >= bannerEvent.MaxUsagePerUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user event limit: User {UserId}, Event {EventId}", userId, eventId);
                return true; // Return true (limit reached) on error for safety
            }
        }

        public async Task<Result<EventUsage>> CreateEventUsageAsync(EventUsage eventUsage)
        {
            try
            {
                if (eventUsage == null)
                    return Result<EventUsage>.Failure("Event usage cannot be null");

                // Validate the event usage
                var validationResult = ValidateEventUsage(eventUsage);
                if (!validationResult.Succeeded)
                    return Result<EventUsage>.Failure(validationResult.Message);

                // Check if user can use the event
                var canUse = await CanUserUseEventAsync(eventUsage.BannerEventId, eventUsage.UserId);
                if (!canUse)
                    return Result<EventUsage>.Failure("User cannot use this event");

                await _unitOfWork.EventUsages.AddAsync(eventUsage);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created event usage: ID {UsageId}, User {UserId}, Event {EventId}",
                    eventUsage.Id, eventUsage.UserId, eventUsage.BannerEventId);

                return Result<EventUsage>.Success(eventUsage, "Event usage created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event usage");
                return Result<EventUsage>.Failure($"Failed to create event usage: {ex.Message}");
            }
        }

        private Result<string> ValidateEventUsage(EventUsage eventUsage)
        {
            if (eventUsage.BannerEventId <= 0)
                return Result<string>.Failure("Invalid banner event ID");

            if (eventUsage.UserId <= 0)
                return Result<string>.Failure("Invalid user ID");

            if (eventUsage.DiscountApplied < 0)
                return Result<string>.Failure("Discount applied cannot be negative");

            return Result<string>.Success("Validation passed");
        }
    }
}