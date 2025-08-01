using Application.Common;
using Application.Common.Helper;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Extension;
using Application.Interfaces.Services;
using Application.Enums;
using Domain.Entities;
using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record CreateBannerSpecialEventCommand(
        [FromBody] CreateBannerSpecialEventDTO bannerSpecialDTO
    ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class CreateBannerSpecialEventCommandHandler : IRequestHandler<CreateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateBannerSpecialEventCommandHandler> _logger;
        private readonly INepalTimeZoneService _nepalTimeZoneService;

        public CreateBannerSpecialEventCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateBannerSpecialEventCommandHandler> logger,
            INepalTimeZoneService nepalTimeZoneService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _nepalTimeZoneService = nepalTimeZoneService;
        }

        public async Task<Result<BannerEventSpecialDTO>> Handle(CreateBannerSpecialEventCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var inputDto = request.bannerSpecialDTO.EventDto;

                    _logger.LogInformation("Creating banner event: {EventName} | Type: {EventType} | Promotion: {PromotionType}",
                        inputDto.Name, inputDto.EventType, inputDto.PromotionType);

                    var startDateResult = TimeParsingHelper.ParseFlexibleDateTime(inputDto.StartDateNepal);
                    if (!startDateResult.Succeeded)
                    {
                        var errorMsg = $"Invalid start date format: '{inputDto.StartDateNepal}'. {startDateResult.Errors}";
                        _logger.LogWarning(" {Error}", errorMsg);
                        throw new ArgumentException(errorMsg);
                    }

                    var endDateResult = TimeParsingHelper.ParseFlexibleDateTime(inputDto.EndDateNepal);
                    if (!endDateResult.Succeeded)
                    {
                        var errorMsg = $"Invalid end date format: '{inputDto.EndDateNepal}'. {endDateResult.Errors}";
                        _logger.LogWarning(" {Error}", errorMsg);
                        throw new ArgumentException(errorMsg);
                    }

                    var startDateNepal = startDateResult.Data;
                    var endDateNepal = endDateResult.Data;

                    //  VALIDATE TIME RANGE
                    if (endDateNepal <= startDateNepal)
                    {
                        throw new ArgumentException($"End date ({TimeParsingHelper.FormatForNepalDisplay(endDateNepal)}) must be after start date ({TimeParsingHelper.FormatForNepalDisplay(startDateNepal)})");
                    }

                    //  VALIDATE MINIMUM DURATION (30 minutes)
                    var duration = endDateNepal - startDateNepal;
                    if (duration.TotalMinutes < 30)
                    {
                        throw new ArgumentException($"Event duration must be at least 30 minutes. Current duration: {duration.TotalMinutes:F0} minutes");
                    }

                    //  VALIDATE START TIME (not too far in past)
                    var currentNepalTime = _nepalTimeZoneService.GetNepalCurrentTime();
                    var timeDiff = currentNepalTime - startDateNepal;
                    if (timeDiff.TotalHours > 1)
                    {
                        throw new ArgumentException($"Event start time cannot be more than 1 hour in the past. " +
                            $"Current Nepal time: {TimeParsingHelper.FormatForNepalDisplay(currentNepalTime)}, " +
                            $"Your start time: {TimeParsingHelper.FormatForNepalDisplay(startDateNepal)}");
                    }

                    //  ACTIVE TIME SLOT HANDLING - FIXED TimeSpan conversion
                    string processedActiveTimeSlot = ProcessActiveTimeSlot(inputDto.ActiveTimeSlot, inputDto.EventType, startDateNepal, endDateNepal);

                    //  CONVERT TO UTC WITH PROPER KIND HANDLING
                    var startDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(startDateNepal);
                    var endDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(endDateNepal);

                    //  ENSURE UTC KIND FOR POSTGRESQL
                    startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                    endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);

                    _logger.LogDebug("Time conversion: Nepal {StartNepal}-{EndNepal} → UTC {StartUtc}-{EndUtc}",
                        startDateNepal.ToString("yyyy-MM-dd HH:mm:ss"), endDateNepal.ToString("yyyy-MM-dd HH:mm:ss"),
                        startDateUtc.ToString("yyyy-MM-dd HH:mm:ss"), endDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));

                    //  STOCK LEVEL VALIDATION USING ENUM
                    if (request.bannerSpecialDTO.ProductIds?.Any() == true)
                    {
                        await ValidateProductsAndStock(request.bannerSpecialDTO.ProductIds, cancellationToken);
                    }

                    //  CHECK FOR NAME UNIQUENESS (Prevent unique constraint violation)
                    var existingEvent = await _unitOfWork.BannerEventSpecials.GetAsync(
                        predicate: e => e.Name.ToLower() == inputDto.Name.ToLower() && !e.IsDeleted,
                        cancellationToken: cancellationToken);

                    if (existingEvent != null)
                    {
                        throw new ArgumentException($"Banner event with name '{inputDto.Name}' already exists (ID: {existingEvent.Id}). Please choose a different name.");
                    }

                    //   BUSINESS LOGIC: DETERMINE INITIAL STATUS USING ENUMS
                    var initialStatus = DetermineInitialEventStatus(inputDto.EventType, startDateNepal, currentNepalTime);
                    var shouldAutoActivate = ShouldAutoActivateEvent(inputDto.EventType, timeDiff);
                    TimeSpan? activeTimeSlotTimeSpan = ConvertTimeSlotToTimeSpan(processedActiveTimeSlot);

                    // CREATE BANNER EVENT ENTITY WITH PROFESSIONAL DEFAULTS
                    var bannerEvent = new BannerEventSpecial
                    {
                        Name = inputDto.Name.Trim(),
                        Description = inputDto.Description?.Trim() ?? string.Empty,
                        TagLine = inputDto.TagLine?.Trim(),
                        EventType = inputDto.EventType,
                        PromotionType = inputDto.PromotionType,
                        DiscountValue = inputDto.DiscountValue,
                        MaxDiscountAmount = inputDto.MaxDiscountAmount,
                        MinOrderValue = inputDto.MinOrderValue ?? 0,
                        StartDate = startDateUtc,
                        EndDate = endDateUtc,
                        ActiveTimeSlot = activeTimeSlotTimeSpan,
                        MaxUsageCount = inputDto.MaxUsageCount ?? GetDefaultMaxUsage(inputDto.EventType),
                        MaxUsagePerUser = inputDto.MaxUsagePerUser ?? GetDefaultMaxUsagePerUser(inputDto.EventType),
                        CurrentUsageCount = 0,
                        Priority = inputDto.Priority ?? GetDefaultPriority(inputDto.EventType),
                        Status = initialStatus,
                        IsActive = shouldAutoActivate,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    //  SAVE BANNER EVENT FIRST
                    var createdEvent = await _unitOfWork.BannerEventSpecials.AddAsync(bannerEvent, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken); // Save to get ID

                    _logger.LogInformation("Banner event created with ID: {EventId} | Status: {Status} | Active: {IsActive}",
                        createdEvent.Id, createdEvent.Status, createdEvent.IsActive);

                    //  CREATE EVENT RULES (if any) - FIXED TYPE CONVERSION
                    if (request.bannerSpecialDTO.Rules?.Any() == true)
                    {
                        await CreateEventRules(createdEvent.Id, request.bannerSpecialDTO.Rules, cancellationToken);
                    }

                    //  ASSOCIATE PRODUCTS (if any) - FIXED BULK INSERT
                    if (request.bannerSpecialDTO.ProductIds?.Any() == true)
                    {
                        await AssociateProductsWithEvent(createdEvent.Id, request.bannerSpecialDTO.ProductIds, cancellationToken);
                    }

                    //  FINAL SAVE
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // CONVERT TO DTO WITH NEPAL TIME DISPLAY
                    var resultDto = createdEvent.ToDTO(_nepalTimeZoneService);

                    _logger.LogInformation(" Banner event created successfully: {EventId} - {EventName} | " +
                        "Status: {Status} | Active: {IsActive} | " +
                        "Nepal Time: {StartDateNepal} to {EndDateNepal}",
                        createdEvent.Id, createdEvent.Name, createdEvent.Status, createdEvent.IsActive,
                        inputDto.StartDateNepal, inputDto.EndDateNepal);

                    var successMessage = GetSuccessMessage(createdEvent.Status, createdEvent.IsActive, createdEvent.Name);
                    return Result<BannerEventSpecialDTO>.Success(resultDto, successMessage);
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, " Invalid input data for banner event: {EventName}",
                    request.bannerSpecialDTO.EventDto.Name);
                return Result<BannerEventSpecialDTO>.Failure($"Validation error: {ex.Message}");
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx)
            {
                _logger.LogError(ex, " Database constraint violation: {ErrorCode} - {Message}",
                    pgEx.ErrorCode, pgEx.MessageText);

                var errorMessage = GetUserFriendlyErrorMessage(pgEx.ErrorCode.ToString(), pgEx.MessageText);
                return Result<BannerEventSpecialDTO>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to create banner event: {EventName}",
                    request.bannerSpecialDTO.EventDto.Name);
                return Result<BannerEventSpecialDTO>.Failure($"Failed to create banner event: {ex.Message}");
            }
        }

        // PROFESSIONAL BUSINESS LOGIC METHODS WITH ENUM USAGE

        /// <summary>
        ///  Validate products exist and have adequate stock using StockLevel enum
        /// </summary>
        private async Task ValidateProductsAndStock(List<int> productIds, CancellationToken cancellationToken)
        {
            var existingProducts = await _unitOfWork.Products.GetAllAsync(
                predicate: p => productIds.Contains(p.Id) && !p.IsDeleted,
                cancellationToken: cancellationToken);

            var existingProductIds = existingProducts.Select(p => p.Id).ToList();
            var missingProductIds = productIds.Except(existingProductIds).ToList();

            if (missingProductIds.Any())
            {
                throw new ArgumentException($"Products not found: {string.Join(", ", missingProductIds)}. Please verify product IDs exist and are not deleted.");
            }

            //  Check stock levels using StockLevel enum
            var lowStockProducts = existingProducts.Where(p => GetStockLevel(p.StockQuantity) == StockLevel.OutOfStock).ToList();

            if (lowStockProducts.Any())
            {
                var lowStockIds = string.Join(", ", lowStockProducts.Select(p => $"{p.Id} ({p.Name})"));
                _logger.LogWarning("Products with low/no stock included in event: {LowStockProducts}", lowStockIds);

                // Don't throw exception, just warn - business decision to allow events on low stock items
            }

            _logger.LogInformation(" Validated {ValidCount}/{TotalCount} products for event. Stock status checked.",
                existingProductIds.Count, productIds.Count);
        }

        /// <summary>
        ///  Determine stock level using StockLevel enum
        /// </summary>
        private StockLevel GetStockLevel(int stockQuantity)
        {
            return stockQuantity switch
            {
                0 => StockLevel.OutOfStock,
                <= 5 => StockLevel.Low,      // Assuming low stock threshold is 5
                <= 20 => StockLevel.Medium,  // Medium stock threshold
                _ => StockLevel.High         // High stock
            };
        }

        /// <summary>
        ///  Create event rules with proper error handling - FIXED TYPE CONVERSION
        /// </summary>
        private async Task CreateEventRules(int eventId, List<AddEventRuleDTO> ruleDtos, CancellationToken cancellationToken)
        {
            var ruleEntities = ruleDtos.Select(ruleDto => new EventRule
            {
                BannerEventId = eventId,
                Type = ruleDto.Type,
                TargetValue = ruleDto.TargetValue?.Trim() ?? string.Empty,
                Conditions = ruleDto.Conditions?.Trim() ?? string.Empty,
                DiscountType = ruleDto.DiscountType,
                DiscountValue = ruleDto.DiscountValue,
                MaxDiscount = ruleDto.MaxDiscount,
                MinOrderValue = ruleDto.MinOrderValue,
                Priority = ruleDto.Priority ,  
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            foreach (var rule in ruleEntities)
            {
                await _unitOfWork.EventRules.AddAsync(rule, cancellationToken);
            }

            _logger.LogInformation("✅ Created {Count} event rules", ruleEntities.Count);
        }

        /// <summary>
        ///  Associate products with event
        /// </summary>
        private async Task AssociateProductsWithEvent(int eventId, List<int> productIds, CancellationToken cancellationToken)
        {
            var productEntities = productIds.Select(productId => new EventProduct
            {
                BannerEventId = eventId,
                ProductId = productId,
                SpecificDiscount = null, // Can be set later for product-specific discounts
                AddedAt = DateTime.UtcNow,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            foreach (var product in productEntities)
            {
                await _unitOfWork.EventProducts.AddAsync(product, cancellationToken);
            }

            _logger.LogInformation("Associated {Count} products with event", productEntities.Count);
        }

        /// <summary>
        /// Process ActiveTimeSlot based on event type and validation - FIXED string return
        /// </summary>
        private string ProcessActiveTimeSlot(string? inputTimeSlot, EventType eventType, DateTime startDate, DateTime endDate)
        {
            // CASE 1: If provided, validate format
            if (!string.IsNullOrWhiteSpace(inputTimeSlot))
            {
                if (IsValidTimeSlotFormat(inputTimeSlot))
                {
                    return inputTimeSlot.Trim();
                }

                _logger.LogWarning("⚠️ Invalid time slot format provided: {TimeSlot}. Auto-generating from dates.", inputTimeSlot);
            }

            // CASE 2: Auto-generate based on YOUR ACTUAL EVENT TYPES
            return eventType switch
            {
                EventType.Flash => $"{startDate:HH:mm}-{endDate:HH:mm}",     // Precise timing for flash sales
                EventType.NewArrival => $"{startDate:HH:mm}-{endDate:HH:mm}", // Hour-specific timing  
                EventType.Loyalty => "00:00-23:59",                         // All day
                EventType.Occasional => "00:00-23:59",                      // All day during occasional
                EventType.Seasonal => "00:00-23:59",                        // All day during season
                EventType.Festive => $"{startDate:HH:mm}-{endDate:HH:mm}",  // Precise timing
                EventType.Clearance => "00:00-23:59",                       // All day clearance
                _ => $"{startDate:HH:mm}-{endDate:HH:mm}"                    // Default to precise timing
            };
        }

        private bool IsValidTimeSlotFormat(string timeSlot)
        {
            if (string.IsNullOrWhiteSpace(timeSlot)) return false;

            // Validate formats like "13:20-15:30", "1:20 PM - 3:30 PM", "00:00-23:59"
            var patterns = new[]
            {
                @"^\d{1,2}:\d{2}-\d{1,2}:\d{2}$",                    // "13:20-15:30"
                @"^\d{1,2}:\d{2}\s*-\s*\d{1,2}:\d{2}$",             // "13:20 - 15:30"
                @"^\d{1,2}:\d{2}\s*[APap][Mm]\s*-\s*\d{1,2}:\d{2}\s*[APap][Mm]$"  // "1:20 PM - 3:30 PM"
            };

            return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(timeSlot, pattern));
        }

        /// <summary>
        ///  Determine initial event status using business logic - FIXED with YOUR ENUMS
        /// </summary>
        private EventStatus DetermineInitialEventStatus(EventType eventType, DateTime startDate, DateTime currentTime)
        {
            var timeDiff = startDate - currentTime;

            return eventType switch
            {
                // Flash sales need immediate readiness if starting within 5 minutes
                EventType.Flash when timeDiff.TotalMinutes <= 5 => EventStatus.Scheduled,

                // Regular events start as Draft for safety based on timing
                _ when timeDiff.TotalHours > 24 => EventStatus.Draft,        // Far future = needs review
                _ when timeDiff.TotalMinutes <= 0 => EventStatus.Active,     // Already started
                _ when timeDiff.TotalHours <= 2 => EventStatus.Scheduled,   // Near future = auto-schedule
                _ => EventStatus.Draft                                       // Default to draft for safety
            };
        }

        /// <summary>
        ///  Determine if event should auto-activate based on type and timing
        /// </summary>
        private bool ShouldAutoActivateEvent(EventType eventType, TimeSpan timeDiff)
        {
            return eventType switch
            {
                //  Flash sales activate immediately if in window
                EventType.Flash => timeDiff.TotalMinutes <= 5 && timeDiff.TotalMinutes >= -5,  // 5 min window

                //  Others remain inactive until manually activated for safety
                _ => false
            };
        }

        /// <summary>
        ///  Get default max usage based on YOUR EVENT TYPES
        /// </summary>
        private int GetDefaultMaxUsage(EventType eventType)
        {
            return eventType switch
            {
                EventType.Flash => 100,            // Limited flash sale
                EventType.NewArrival => 200,       // New arrival promotion
                EventType.Loyalty => 500,          // Loyalty customer events
                EventType.Occasional => 300,       // Occasional events
                EventType.Seasonal => 2000,        // Seasonal events
                EventType.Festive => 1500,         // Festive events
                EventType.Clearance => 5000,       // Large clearance volume
                _ => 1000                           // Default safe limit
            };
        }

        /// <summary>
        ///  Get default max usage per user based on YOUR EVENT TYPES
        /// </summary>
        private int GetDefaultMaxUsagePerUser(EventType eventType)
        {
            return eventType switch
            {
                EventType.Flash => 1,              // One chance only
                EventType.NewArrival => 2,         // New arrival flexibility
                EventType.Loyalty => 3,            // Loyalty customer benefit
                EventType.Occasional => 2,         // Occasional flexibility
                EventType.Seasonal => 5,           // Seasonal generosity
                EventType.Festive => 3,            // Festive celebration
                EventType.Clearance => 10,         // Clearance allows more volume
                _ => 1                              // Default conservative limit
            };
        }

        /// <summary>
        ///  Get default priority based on YOUR EVENT TYPES
        /// </summary>
        private int GetDefaultPriority(EventType eventType)
        {
            return eventType switch
            {
                EventType.Flash => 10,             // Highest priority
                EventType.Festive => 9,            // Very high priority (festivals are important)
                EventType.Seasonal => 8,           // High priority
                EventType.NewArrival => 7,         // Medium-high priority
                EventType.Loyalty => 6,            // Medium priority
                EventType.Occasional => 4,         // Medium-low priority
                EventType.Clearance => 2,          // Lower priority
                _ => 5                              // Default medium priority
            };
        }

        /// <summary>
        ///  Get contextual success message based on status
        /// </summary>
        private string GetSuccessMessage(EventStatus status, bool isActive, string eventName)
        {
            return status switch
            {
                EventStatus.Active => $" Event '{eventName}' is now LIVE and accepting customers!",
                EventStatus.Scheduled => $"Event '{eventName}' is scheduled and will auto-activate at start time.",
                EventStatus.Draft => $"Event '{eventName}' created as DRAFT. Use activation command to make it live.",
                EventStatus.Expired => $"Event '{eventName}' completed successfully or Expired.",
                EventStatus.Cancelled => $" Event '{eventName}' was cancelled.",
                _ => $" Event '{eventName}' created successfully with status: {status}."
            };
        }

        // USER-FRIENDLY ERROR MESSAGES (keeping existing logic)
        private static string GetUserFriendlyErrorMessage(string errorCode, string? defaultMessage)
        {
            return errorCode switch
            {
                "23503" => "Invalid product reference. One or more products don't exist or are deleted.",
                "23505" => "Banner event with this name already exists. Please choose a different name.",
                "23514" => "Invalid data provided. Check discount values, dates, and other constraints.",
                "23P01" => "Conflicting data detected. Check for overlapping events or duplicate entries.",
                "42P01" => "Database table missing. Please contact administrator.",
                "42703" => "Database column missing. Please contact administrator.",
                "22001" => "Text data too long. Please shorten the name, description, or other text fields.",
                "22003" => "Numeric value out of range. Check discount values and percentages.",
                "08P01" => "Database connection issue. Please try again.",
                "53300" => "Database resource limit reached. Please try again later.",
                _ => $"Database error ({errorCode}): {defaultMessage ?? "Unknown database error. Please contact support if the problem persists."}"
            };
        }

        private static TimeSpan? ConvertTimeSlotToTimeSpan(string timeSlotString)
        {
            if (string.IsNullOrWhiteSpace(timeSlotString))
                return null;

            try
            {
                // Handle formats like "13:20-15:30" or "00:00-23:59"
                var parts = timeSlotString.Split('-');
                if (parts.Length == 2)
                {
                    // Parse start time and return as TimeSpan
                    if (TimeSpan.TryParse(parts[0].Trim(), out var startTime))
                    {
                        return startTime;
                    }
                }

                // Fallback: try to parse the whole string as TimeSpan
                if (TimeSpan.TryParse(timeSlotString, out var directTimeSpan))
                {
                    return directTimeSpan;
                }

                return null;
            }
            catch (Exception )
            {
                return null;
            }
        }
    }
}