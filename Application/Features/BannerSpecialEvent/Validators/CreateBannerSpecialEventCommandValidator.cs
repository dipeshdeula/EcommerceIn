using Application.Features.BannerSpecialEvent.Commands;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Validators
{
    public class CreateBannerSpecialEventCommandValidator : AbstractValidator<CreateBannerSpecialEventCommand>
    {
        private readonly IBannerEventSpecialRepository _bannerEventRepository;
        private readonly INepalTimeZoneService _nepalTimeService;
        private readonly ILogger<CreateBannerSpecialEventCommandValidator> _logger;

        public CreateBannerSpecialEventCommandValidator(
            IBannerEventSpecialRepository bannerEventRepository,
            INepalTimeZoneService nepalTimeService,
            ILogger<CreateBannerSpecialEventCommandValidator> logger)
        {
            _bannerEventRepository = bannerEventRepository;
            _nepalTimeService = nepalTimeService;
            _logger = logger;

            SetupValidationRules();
        }

        private void SetupValidationRules()
        {
            // ✅ BASIC VALIDATION
            RuleFor(x => x.bannerSpecialDTO.EventDto.Name)
                .NotEmpty().WithMessage("Event name is required.")
                .Length(3, 100).WithMessage("Event name must be between 3 and 100 characters.");

            RuleFor(x => x.bannerSpecialDTO.EventDto.Description)
                .NotEmpty().WithMessage("Event description is required.")
                .Length(10, 1000).WithMessage("Event description must be between 10 and 1000 characters.");

            // ✅ DATE FORMAT VALIDATION (Using correct STRING properties)
            RuleFor(x => x.bannerSpecialDTO.EventDto.StartDateNepal)
                .NotEmpty().WithMessage("Start date is required.")
                .Must(BeValidDateTimeString).WithMessage("Start date must be in valid format (YYYY-MM-DDTHH:mm:ss).");

            RuleFor(x => x.bannerSpecialDTO.EventDto.EndDateNepal)
                .NotEmpty().WithMessage("End date is required.")
                .Must(BeValidDateTimeString).WithMessage("End date must be in valid format (YYYY-MM-DDTHH:mm:ss).");

            // ✅ BUSINESS LOGIC VALIDATION (Fixed to use parsed dates)
            RuleFor(x => x.bannerSpecialDTO.EventDto)
                .Must(HaveValidDateRange).WithMessage("End date must be after start date.");

            RuleFor(x => x.bannerSpecialDTO.EventDto)
                .Must(HaveValidDuration).WithMessage("Event duration must be between 30 minutes and 365 days.");

            RuleFor(x => x.bannerSpecialDTO.EventDto)
                .Must(HaveValidStartTime).WithMessage("Event start time cannot be more than 1 hour in the past (Nepal time).");

            RuleFor(x => x.bannerSpecialDTO.EventDto.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than 0.");

            // ✅ CONFLICT VALIDATION (Non-blocking)
            RuleFor(x => x)
                .MustAsync(NotHaveConflictingEvents)
                .WithMessage("Event conflicts with existing high-priority events during the specified time period.");
        }

        private bool BeValidDateTimeString(string? dateString)
        {
            if (string.IsNullOrEmpty(dateString)) return false;
            return DateTime.TryParse(dateString, out var parsedDate) &&
                   parsedDate > new DateTime(2020, 1, 1) &&
                   parsedDate < new DateTime(2030, 12, 31);
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

        private bool HaveValidStartTime(AddBannerEventSpecialDTO dto)
        {
            if (!DateTime.TryParse(dto.StartDateNepal, out var startDate)) return false;

            var currentNepalTime = _nepalTimeService.GetNepalCurrentTime();
            return startDate >= currentNepalTime.AddHours(-1); // Allow 1 hour buffer
        }

        private async Task<bool> NotHaveConflictingEvents(CreateBannerSpecialEventCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var dto = command.bannerSpecialDTO.EventDto;

                // ✅ PARSE NEPAL DATES FROM STRINGS
                if (!DateTime.TryParse(dto.StartDateNepal, out var startDateNepal))
                {
                    _logger.LogWarning("Invalid start date format for conflict validation: {StartDate}", dto.StartDateNepal);
                    return true; // Let other validation handle this
                }

                if (!DateTime.TryParse(dto.EndDateNepal, out var endDateNepal))
                {
                    _logger.LogWarning("Invalid end date format for conflict validation: {EndDate}", dto.EndDateNepal);
                    return true; // Let other validation handle this
                }

                // ✅ CONVERT TO UTC WITH PROPER KIND FOR POSTGRESQL QUERY
                var startDateUtc = _nepalTimeService.ConvertFromNepalToUtc(startDateNepal);
                var endDateUtc = _nepalTimeService.ConvertFromNepalToUtc(endDateNepal);

                // ✅ ENSURE UTC KIND FOR POSTGRESQL
                startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);

                var priority = dto.Priority ?? 1;

                // ✅ QUERY FOR CONFLICTING EVENTS (Using UTC dates)
                var conflictingEvents = await _bannerEventRepository.GetAllAsync(
                    predicate: e => e.IsActive &&
                                !e.IsDeleted &&
                                e.Priority >= priority &&
                                e.StartDate <= endDateUtc &&
                                e.EndDate >= startDateUtc,
                    cancellationToken: cancellationToken);

                if (conflictingEvents.Any())
                {
                    _logger.LogInformation("Found {Count} conflicting events for priority {Priority} during {StartDate} to {EndDate}",
                        conflictingEvents.Count(), priority, dto.StartDateNepal, dto.EndDateNepal);
                }

                return !conflictingEvents.Any();
            }
            catch (Exception ex)
            {
                // ✅ LOG ERROR BUT DON'T FAIL VALIDATION
                _logger.LogWarning(ex, "Conflict validation failed, proceeding with creation: {ErrorMessage}", ex.Message);
                return true; // Allow creation to proceed
            }
        }
    }
}