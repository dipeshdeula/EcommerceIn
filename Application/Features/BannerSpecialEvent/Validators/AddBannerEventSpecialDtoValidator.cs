using Application.Dto.BannerEventSpecialDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums.BannerEventSpecial;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Validators
{
    public class AddBannerEventSpecialDtoValidator : AbstractValidator<AddBannerEventSpecialDTO>
    {
        private readonly IBannerEventSpecialRepository _bannerEventRepository;
        private readonly INepalTimeZoneService _nepalTimeService;
        private readonly ILogger<AddBannerEventSpecialDtoValidator> _logger;

        public AddBannerEventSpecialDtoValidator(
            IBannerEventSpecialRepository bannerEventRepository,
            INepalTimeZoneService nepalTimeService,
            ILogger<AddBannerEventSpecialDtoValidator> logger)
        {
            _bannerEventRepository = bannerEventRepository;
            _nepalTimeService = nepalTimeService;
            _logger = logger;

            SetupValidationRules();
        }

        private void SetupValidationRules()
        {
            // ✅ BASIC INFO VALIDATION
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Event name is required.")
                .Length(3, 100)
                .WithMessage("Event name must be between 3 and 100 characters.")
                .MustAsync(BeUniqueEventName)
                .WithMessage("Event name '{PropertyValue}' already exists.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Event description is required.")
                .Length(10, 1000)
                .WithMessage("Event description must be between 10 and 1000 characters.");

            RuleFor(x => x.TagLine)
                .MaximumLength(200)
                .WithMessage("Tag line cannot exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.TagLine));

            // ✅ BUSINESS RULES VALIDATION
            RuleFor(x => x.EventType)
                .IsInEnum()
                .WithMessage("Invalid event type selected.");

            RuleFor(x => x.PromotionType)
                .IsInEnum()
                .WithMessage("Invalid promotion type selected.");

            // ✅ DISCOUNT VALIDATION WITH COMPLEX BUSINESS RULES
            RuleFor(x => x.DiscountValue)
                .GreaterThan(0)
                .WithMessage("Discount value must be greater than 0.")
                .Must(BeValidDiscountValue)
                .WithMessage("Invalid discount value for selected promotion type. Percentage: 1-100, Fixed Amount: 1-100,000.");

            RuleFor(x => x.MaxDiscountAmount)
                .GreaterThan(0)
                .When(x => x.MaxDiscountAmount.HasValue)
                .WithMessage("Maximum discount amount must be greater than 0.")
                .Must((dto, maxDiscount) => BeValidMaxDiscount(dto, maxDiscount))
                .When(x => x.MaxDiscountAmount.HasValue)
                .WithMessage("Maximum discount amount must be appropriate for the promotion type.");

            RuleFor(x => x.MinOrderValue)
                .GreaterThan(0)
                .When(x => x.MinOrderValue.HasValue)
                .WithMessage("Minimum order value must be greater than 0.");

            // ✅ NEPAL TIME VALIDATION (Using correct STRING properties)
            RuleFor(x => x.StartDateNepal)
                .NotEmpty()
                .WithMessage("Start date is required.")
                .Must(BeValidNepalTimeString)
                .WithMessage("Start date must be a valid date/time format (YYYY-MM-DDTHH:mm:ss).")
                .Must(BeInFutureNepalTime)
                .WithMessage("Start date cannot be in the past (Nepal time).");

            RuleFor(x => x.EndDateNepal)
                .NotEmpty()
                .WithMessage("End date is required.")
                .Must(BeValidNepalTimeString)
                .WithMessage("End date must be a valid date/time format (YYYY-MM-DDTHH:mm:ss).");

            // ✅ DATE RANGE VALIDATION
            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage("End date must be after start date.");

            // ✅ DURATION VALIDATION
            RuleFor(x => x)
                .Must(HaveValidDuration)
                .WithMessage("Event duration must be between 30 minutes and 365 days.");

            // ✅ USAGE LIMITS VALIDATION
            RuleFor(x => x.MaxUsageCount)
                .GreaterThan(0)
                .When(x => x.MaxUsageCount.HasValue)
                .WithMessage("Maximum usage count must be greater than 0.")
                .LessThanOrEqualTo(1000000)
                .When(x => x.MaxUsageCount.HasValue)
                .WithMessage("Maximum usage count cannot exceed 1,000,000.");

            RuleFor(x => x.MaxUsagePerUser)
                .GreaterThan(0)
                .When(x => x.MaxUsagePerUser.HasValue)
                .WithMessage("Maximum usage per user must be greater than 0.")
                .LessThanOrEqualTo(100)
                .When(x => x.MaxUsagePerUser.HasValue)
                .WithMessage("Maximum usage per user cannot exceed 100.");

            RuleFor(x => x.Priority)
                .InclusiveBetween(1, 100)
                .When(x => x.Priority.HasValue)
                .WithMessage("Priority must be between 1 and 100.");

            // ✅ ACTIVE TIME SLOT VALIDATION
            RuleFor(x => x.ActiveTimeSlot)
                .Must(BeValidTimeSpanString)
                .When(x => !string.IsNullOrEmpty(x.ActiveTimeSlot))
                .WithMessage("Active time slot must be in format HH:mm:ss (e.g., 14:30:00).");

            // ✅ BUSINESS LOGIC VALIDATION
            RuleFor(x => x)
                .MustAsync(NotConflictWithExistingEvents)
                .WithMessage("Event conflicts with existing high-priority events during the specified time period.");
        }

        // ✅ CUSTOM VALIDATION METHODS
        private bool BeValidDiscountValue(AddBannerEventSpecialDTO dto, decimal discountValue)
        {
            return dto.PromotionType switch
            {
                PromotionType.Percentage => discountValue > 0 && discountValue <= 100,
                PromotionType.FixedAmount => discountValue > 0 && discountValue <= 100000,
                _ => false
            };
        }

        private bool BeValidMaxDiscount(AddBannerEventSpecialDTO dto, decimal? maxDiscount)
        {
            if (!maxDiscount.HasValue) return true;

            return dto.PromotionType switch
            {
                PromotionType.Percentage => maxDiscount.Value >= 1, // At least Rs. 1 for percentage
                PromotionType.FixedAmount => maxDiscount.Value >= dto.DiscountValue, // At least the base discount
                _ => false
            };
        }

        private bool BeValidNepalTimeString(string? dateTimeString)
        {
            if (string.IsNullOrEmpty(dateTimeString)) return false;

            if (!DateTime.TryParse(dateTimeString, out var parsedDate)) return false;

            // ✅ REASONABLE DATE RANGE
            return parsedDate > new DateTime(2020, 1, 1) && parsedDate < new DateTime(2030, 12, 31);
        }

        private bool BeInFutureNepalTime(string? startDateString)
        {
            if (string.IsNullOrEmpty(startDateString)) return false;
            if (!DateTime.TryParse(startDateString, out var startDate)) return false;

            var currentNepalTime = _nepalTimeService.GetNepalCurrentTime();
            return startDate >= currentNepalTime.AddMinutes(-30); // 30-minute buffer for clock differences
        }

        private bool HaveValidDateRange(AddBannerEventSpecialDTO dto)
        {
            if (!DateTime.TryParse(dto.StartDateNepal, out var startDate)) return false;
            if (!DateTime.TryParse(dto.EndDateNepal, out var endDate)) return false;

            return endDate > startDate;
        }

        private bool HaveValidDuration(AddBannerEventSpecialDTO dto)
        {
            if (!DateTime.TryParse(dto.StartDateNepal, out var startDate)) return false;
            if (!DateTime.TryParse(dto.EndDateNepal, out var endDate)) return false;

            var duration = endDate - startDate;
            return duration.TotalMinutes >= 30 && duration.TotalDays <= 365;
        }

        private bool BeValidTimeSpanString(string? timeSlotString)
        {
            if (string.IsNullOrEmpty(timeSlotString)) return true;

            return TimeSpan.TryParse(timeSlotString, out var timeSpan) &&
                   timeSpan >= TimeSpan.Zero &&
                   timeSpan < TimeSpan.FromDays(1);
        }

        private async Task<bool> BeUniqueEventName(string name, CancellationToken cancellationToken)
        {
            try
            {
                var existingEvents = await _bannerEventRepository.GetAllAsync(
                    predicate: e => e.Name.ToLower() == name.ToLower() && !e.IsDeleted,
                    cancellationToken: cancellationToken);
                return !existingEvents.Any();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking event name uniqueness for: {EventName}", name);
                return true; // Allow creation if check fails
            }
        }

        private async Task<bool> NotConflictWithExistingEvents(AddBannerEventSpecialDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ PARSE STRING DATES TO DATETIME
                if (!DateTime.TryParse(dto.StartDateNepal, out var startDateNepal)) return true;
                if (!DateTime.TryParse(dto.EndDateNepal, out var endDateNepal)) return true;

                // ✅ CONVERT TO UTC FOR DATABASE QUERY
                var startDateUtc = _nepalTimeService.ConvertFromNepalToUtc(startDateNepal);
                var endDateUtc = _nepalTimeService.ConvertFromNepalToUtc(endDateNepal);

                // ✅ ENSURE UTC KIND FOR POSTGRESQL
                startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);

                var priority = dto.Priority ?? 1;

                var conflictingEvents = await _bannerEventRepository.GetAllAsync(
                    predicate: e => e.IsActive &&
                                !e.IsDeleted &&
                                e.Priority >= priority &&
                                e.StartDate <= endDateUtc &&
                                e.EndDate >= startDateUtc,
                    cancellationToken: cancellationToken);

                if (conflictingEvents.Any())
                {
                    _logger.LogInformation("Found {Count} conflicting events for '{EventName}' during {StartDate} to {EndDate}",
                        conflictingEvents.Count(), dto.Name, dto.StartDateNepal, dto.EndDateNepal);
                }

                return !conflictingEvents.Any();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking event conflicts for: {EventName}", dto.Name);
                return true; // Allow creation if conflict check fails
            }
        }
    }
}