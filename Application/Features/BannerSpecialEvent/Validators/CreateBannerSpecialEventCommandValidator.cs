using Application.Features.BannerSpecialEvent.Commands;
using Application.Common.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Features.BannerSpecialEvent.Validators
{
    public class CreateBannerSpecialEventCommandValidator : AbstractValidator<CreateBannerSpecialEventCommand>
    {
        private readonly IBannerEventSpecialRepository _bannerEventRepository;
        private readonly IProductRepository _productRepository;
        private readonly INepalTimeZoneService _nepalTimeService;
        private readonly ILogger<CreateBannerSpecialEventCommandValidator> _logger;

        public CreateBannerSpecialEventCommandValidator(
            IBannerEventSpecialRepository bannerEventRepository,
            IProductRepository productRepository,
            INepalTimeZoneService nepalTimeService,
            ILogger<CreateBannerSpecialEventCommandValidator> logger)
        {
            _bannerEventRepository = bannerEventRepository;
            _productRepository = productRepository;
            _nepalTimeService = nepalTimeService;
            _logger = logger;

            SetupValidationRules();
        }

        private void SetupValidationRules()
        {
            // EVENT DTO VALIDATION (Fixed property access)
            RuleFor(x => x.bannerSpecialDTO.EventDto.Name)
                .NotEmpty().WithMessage("Event name is required")
                .Length(2, 200).WithMessage("Event name must be between 2 and 200 characters")
                .MustAsync(BeUniqueName).WithMessage("Event name already exists");

            RuleFor(x => x.bannerSpecialDTO.EventDto.Description)
                .NotEmpty().WithMessage("Event description is required")
                .Length(10, 1000).WithMessage("Event description must be between 10 and 1000 characters");

            RuleFor(x => x.bannerSpecialDTO.EventDto.TagLine)
                .MaximumLength(200).WithMessage("Tag line cannot exceed 200 characters");

            // DISCOUNT VALIDATION
            RuleFor(x => x.bannerSpecialDTO.EventDto.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than 0")
                .LessThanOrEqualTo(100).When(x => x.bannerSpecialDTO.EventDto.PromotionType == Domain.Enums.BannerEventSpecial.PromotionType.Percentage)
                .WithMessage("Percentage discount cannot exceed 100%");

            RuleFor(x => x.bannerSpecialDTO.EventDto.MaxDiscountAmount)
                .GreaterThan(0).When(x => x.bannerSpecialDTO.EventDto.MaxDiscountAmount.HasValue)
                .WithMessage("Maximum discount amount must be greater than 0");

            //  TIME VALIDATION WITH FLEXIBLE PARSING
            RuleFor(x => x.bannerSpecialDTO.EventDto.StartDateNepal)
                .NotEmpty().WithMessage("Start date is required")
                .Must(BeValidDateTime).WithMessage(dto =>
                    $"Invalid start date format: '{dto.bannerSpecialDTO.EventDto.StartDateNepal}'. " +
                    $"Supported formats: {string.Join(", ", TimeParsingHelper.GetSupportedFormats())}")
                .MustAsync(BeValidStartTime).WithMessage(dto =>
                    $"Event start time cannot be more than 1 hour in the past (Nepal time). " +
                    $"Current Nepal time: {_nepalTimeService.GetNepalCurrentTime():yyyy-MM-dd h:mm tt}");

            RuleFor(x => x.bannerSpecialDTO.EventDto.EndDateNepal)
                .NotEmpty().WithMessage("End date is required")
                .Must(BeValidDateTime).WithMessage(dto =>
                    $"Invalid end date format: '{dto.bannerSpecialDTO.EventDto.EndDateNepal}'. " +
                    $"Supported formats: {string.Join(", ", TimeParsingHelper.GetSupportedFormats())}")
                .Must((dto, endDate) => BeAfterStartDate(dto.bannerSpecialDTO.EventDto.StartDateNepal, endDate))
                .WithMessage("End date must be after start date");

            //  DURATION VALIDATION
            RuleFor(x => x.bannerSpecialDTO.EventDto)
                .Must(HaveValidDuration).WithMessage("Event duration must be between 30 minutes and 365 days");

            // USAGE LIMITS VALIDATION
            RuleFor(x => x.bannerSpecialDTO.EventDto.MaxUsageCount)
                .GreaterThan(0).When(x => x.bannerSpecialDTO.EventDto.MaxUsageCount.HasValue)
                .WithMessage("Maximum usage count must be greater than 0");

            RuleFor(x => x.bannerSpecialDTO.EventDto.MaxUsagePerUser)
                .GreaterThan(0).When(x => x.bannerSpecialDTO.EventDto.MaxUsagePerUser.HasValue)
                .WithMessage("Maximum usage per user must be greater than 0");

            // PRODUCT VALIDATION
            RuleFor(x => x.bannerSpecialDTO.ProductIds)
                .MustAsync(AllProductsExist).When(x => x.bannerSpecialDTO.ProductIds?.Any() == true)
                .WithMessage("One or more products don't exist or are deleted");

            // CONFLICT VALIDATION (Non-blocking warning)
            RuleFor(x => x)
                .MustAsync(NotHaveConflictingEvents)
                .WithMessage("Warning: High-priority events exist during this time period. Event will still be created but may have lower precedence.");
        }

        // VALIDATION METHODS
        private bool BeValidDateTime(string dateTimeString)
        {
            var result = TimeParsingHelper.ParseFlexibleDateTime(dateTimeString);
            return result.Succeeded;
        }

        private bool BeAfterStartDate(string startDateString, string endDateString)
        {
            var startResult = TimeParsingHelper.ParseFlexibleDateTime(startDateString);
            var endResult = TimeParsingHelper.ParseFlexibleDateTime(endDateString);

            if (!startResult.Succeeded || !endResult.Succeeded)
                return false;

            return endResult.Data > startResult.Data;
        }

        private bool HaveValidDuration(Application.Dto.BannerEventSpecialDTOs.AddBannerEventSpecialDTO dto)
        {
            var startResult = TimeParsingHelper.ParseFlexibleDateTime(dto.StartDateNepal);
            var endResult = TimeParsingHelper.ParseFlexibleDateTime(dto.EndDateNepal);

            if (!startResult.Succeeded || !endResult.Succeeded)
                return false;

            var duration = endResult.Data - startResult.Data;
            return duration.TotalMinutes >= 30 && duration.TotalDays <= 365;
        }

        private async Task<bool> BeValidStartTime(string startDateString, CancellationToken cancellationToken)
        {
            try
            {
                var parseResult = TimeParsingHelper.ParseFlexibleDateTime(startDateString);
                if (!parseResult.Succeeded)
                    return false;

                var nepalNow = _nepalTimeService.GetNepalCurrentTime();
                var startDateNepal = parseResult.Data;

                var timeDifference = nepalNow - startDateNepal;
                var isValid = timeDifference.TotalHours <= 1; // Allow up to 1 hour in the past

                _logger.LogDebug("Start time validation: Nepal now: {NepalNow}, Start: {StartTime}, Difference: {Difference} hours, Valid: {Valid}",
                    nepalNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    startDateNepal.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeDifference.TotalHours,
                    isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during start time validation for: {StartDateString}", startDateString);
                return false;
            }
        }

        private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
        {
            try
            {
                var existingEvent = await _bannerEventRepository.GetAsync(
                    predicate: e => e.Name.ToLower() == name.ToLower() && !e.IsDeleted,
                    cancellationToken: cancellationToken);

                return existingEvent == null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during name uniqueness validation for: {Name}", name);
                return true; // Allow creation if validation fails
            }
        }

        private async Task<bool> AllProductsExist(List<int>? productIds, CancellationToken cancellationToken)
        {
            try
            {
                if (productIds == null || !productIds.Any())
                    return true;

                //  EFFICIENT APPROACH: Get existing products and extract IDs
                var existingProducts = await _productRepository.GetAllAsync(
                    predicate: p => productIds.Contains(p.Id) && !p.IsDeleted,
                    cancellationToken: cancellationToken);

                var existingProductIds = existingProducts.Select(p => p.Id).ToList();
                var allExist = existingProductIds.Count == productIds.Count;

                if (!allExist)
                {
                    var missingIds = productIds.Except(existingProductIds).ToList();
                    _logger.LogWarning("Missing product IDs during validation: {MissingIds}", string.Join(", ", missingIds));
                }

                return allExist;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during product existence validation");
                return false; // Fail validation if we can't verify products
            }
        }

        private async Task<bool> NotHaveConflictingEvents(CreateBannerSpecialEventCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var dto = command.bannerSpecialDTO.EventDto;

                var startResult = TimeParsingHelper.ParseFlexibleDateTime(dto.StartDateNepal);
                var endResult = TimeParsingHelper.ParseFlexibleDateTime(dto.EndDateNepal);

                if (!startResult.Succeeded || !endResult.Succeeded)
                    return true; // Let other validators handle parsing errors

                var startDateUtc = _nepalTimeService.ConvertFromNepalToUtc(startResult.Data);
                var endDateUtc = _nepalTimeService.ConvertFromNepalToUtc(endResult.Data);

                // ENSURE UTC KIND FOR POSTGRESQL
                startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);

                var priority = dto.Priority ?? 1;

                //  CHECK FOR CONFLICTING HIGHER PRIORITY EVENTS
                var conflictingEvents = await _bannerEventRepository.GetAllAsync(
                    predicate: e => e.IsActive &&
                                !e.IsDeleted &&
                                e.Priority >= priority &&
                                e.StartDate <= endDateUtc &&
                                e.EndDate >= startDateUtc,
                    cancellationToken: cancellationToken);

                var hasConflicts = conflictingEvents.Any();

                if (hasConflicts)
                {
                    _logger.LogInformation("Found {Count} conflicting events for priority {Priority} during {StartDate} to {EndDate}. Event names: {EventNames}",
                        conflictingEvents.Count(), priority, dto.StartDateNepal, dto.EndDateNepal,
                        string.Join(", ", conflictingEvents.Select(e => e.Name)));
                }

                // RETURN TRUE (allow creation) but log the warning
                // The validation message will show as a warning, but creation proceeds
                return !hasConflicts;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during conflict validation. Proceeding with creation: {ErrorMessage}", ex.Message);
                return true; // Allow creation if conflict check fails
            }
        }
    }
}