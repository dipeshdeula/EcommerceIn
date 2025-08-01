using Application.Common;
using Domain.Entities.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(eventId);
                if (bannerEvent == null) return false;

                // check if event is active
                if (!bannerEvent.IsActive || bannerEvent.IsDeleted)
                {
                    _logger.LogWarning("Event {EventId} is not found", eventId);
                    return false;
                }

                // check if event is within valid time range
                var now = DateTime.UtcNow;
                if (now < bannerEvent.StartDate || now > bannerEvent.EndDate)
                {
                    _logger.LogWarning("Event {EventId} is outside valid time range", eventId);
                    return false;
                }

                // Check global usage limit
                if (bannerEvent.CurrentUsageCount >= bannerEvent.MaxUsageCount)
                {
                    _logger.LogWarning("Event {EventId} has reached global usage limit", eventId);
                    return false;
                }

                // Check per-user limit
                if (bannerEvent.MaxUsagePerUser > 0)
                {
                    var userUsageCount = await GetUserEventUsageCountAsync(eventId, userId);
                    if (userUsageCount >= bannerEvent.MaxUsagePerUser)
                    {
                        _logger.LogWarning("User {UserId} has reached usage limit for event {EventId}", userId, eventId);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can use event {EventId}", userId, eventId);
                return false;
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
                var usage = new EventUsage
                {
                    BannerEventId = eventId,
                    UserId = userId,
                    OrderId = orderId,
                    DiscountApplied = discountApplied,
                    UsedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.EventUsages.AddAsync(usage);

                // Update event usage count
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(eventId);
                if (bannerEvent != null)
                {
                    bannerEvent.CurrentUsageCount++;
                    await _unitOfWork.BannerEventSpecials.UpdateAsync(bannerEvent);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Recorded event usage: User {UserId}, Event {EventId}, Discount Rs.{Discount}",
                    userId, eventId, discountApplied);
                return Result<string>.Success("Event usage Recorded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording event usage: User {UserId}, Event {EventId}, Order {OrderId}",
                    userId, eventId, orderId);
                return Result<string>.Failure($"Failed to record event usage: {ex.Message}");

            }
        }

        public async Task<int> GetUserEventUsageCountAsync(int eventId, int userId)
        {
            try
            {
                return await _unitOfWork.EventUsages.CountAsync(
                    predicate: u => u.BannerEventId == eventId &&
                                   u.UserId == userId &&
                                   !u.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user event usage count: User {UserId}, Event {EventId}", userId, eventId);
                return 0;
            }
        }

        public async Task<bool> HasUserReachedEventLimitAsync(int eventId, int userId)
        {
            try
            {
                var bannerEvent = await _unitOfWork.BannerEventSpecials.GetByIdAsync(eventId);
                if (bannerEvent?.MaxUsagePerUser <= 0) return false;

                var userUsageCount = await GetUserEventUsageCountAsync(eventId, userId);
                return userUsageCount >= bannerEvent?.MaxUsagePerUser;
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
