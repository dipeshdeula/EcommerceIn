using Application.Common;
using Application.Common.Helper;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Commands
{
    public record CreatePromoCodeCommand(AddPromoCodeDTO PromoCodeDTO, int CreatedByUserId) : IRequest<Result<PromoCodeDTO>>;
    
    public class CreatePromoCodeCommandHandler : IRequestHandler<CreatePromoCodeCommand, Result<PromoCodeDTO>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CreatePromoCodeCommandHandler> _logger;

        public CreatePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            INepalTimeZoneService nepalTimeZoneService,
            IDistributedCache cache,
            ILogger<CreatePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _nepalTimeZoneService = nepalTimeZoneService;
            _cache = cache;
            _logger = logger;
        }
        
        public async Task<Result<PromoCodeDTO>> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var inputDto = request.PromoCodeDTO;
                
                _logger.LogInformation("🆕 Creating promo code: {Code}", inputDto.Code);

                // ✅ 1. PARSE DATES USING TimeParsingHelper (Best for input parsing)
                var startDateResult = TimeParsingHelper.ParseNepalDateTime(inputDto.StartDateNepal);
                if (!startDateResult.Succeeded)
                {
                    return Result<PromoCodeDTO>.Failure($"Invalid start date: {startDateResult.Message}. " +
                        $"Supported formats: {string.Join(", ", TimeParsingHelper.GetNepalSupportedFormats())}");
                }

                var endDateResult = TimeParsingHelper.ParseNepalDateTime(inputDto.EndDateNepal);
                if (!endDateResult.Succeeded)
                {
                    return Result<PromoCodeDTO>.Failure($"Invalid end date: {endDateResult.Message}. " +
                        $"Supported formats: {string.Join(", ", TimeParsingHelper.GetNepalSupportedFormats())}");
                }

                var startDateNepal = startDateResult.Data;
                var endDateNepal = endDateResult.Data;

                _logger.LogInformation("📅 Parsed Nepal dates: Start '{StartInput}' → {StartParsed}, End '{EndInput}' → {EndParsed}",
                    inputDto.StartDateNepal, startDateNepal.ToString("yyyy-MM-dd HH:mm:ss"),
                    inputDto.EndDateNepal, endDateNepal.ToString("yyyy-MM-dd HH:mm:ss"));

                // ✅ 2. VALIDATE DATE RANGE IN NEPAL TIME
                if (endDateNepal <= startDateNepal)
                {
                    return Result<PromoCodeDTO>.Failure($"End date ({TimeParsingHelper.FormatForNepalDisplay(endDateNepal)}) " +
                        $"must be after start date ({TimeParsingHelper.FormatForNepalDisplay(startDateNepal)})");
                }

                // ✅ 3. VALIDATE TIMING USING NepalTimeZoneService (Best for business logic)
                var currentNepalTime = _nepalTimeZoneService.GetNepalCurrentTime();
                var timeDiff = currentNepalTime - startDateNepal;
                if (timeDiff.TotalHours > 1)
                {
                    return Result<PromoCodeDTO>.Failure($"Start time cannot be more than 1 hour in the past. " +
                        $"Current Nepal time: {currentNepalTime.ToNepalTimeString(_nepalTimeZoneService, "MMM dd, yyyy h:mm tt")}, " +
                        $"Your start time: {startDateNepal.ToNepalTimeString(_nepalTimeZoneService, "MMM dd, yyyy h:mm tt")}");
                }

                // ✅ 4. VALIDATE FUTURE DATES (business rules)
                if (startDateNepal > currentNepalTime.AddYears(2))
                {
                    return Result<PromoCodeDTO>.Failure("Start date cannot be more than 2 years in the future");
                }

                var durationDays = (endDateNepal - startDateNepal).TotalDays;
                if (durationDays > 365)
                {
                    return Result<PromoCodeDTO>.Failure("Promo code duration cannot exceed 365 days");
                }

                // ✅ 5. CONVERT TO UTC USING NepalTimeZoneService + DateTimeExtensions
                DateTime startDateUtc, endDateUtc;
                
                try
                {
                    // Using your DateTimeExtensions for clean code
                    startDateUtc = startDateNepal.ToUtcFromNepal(_nepalTimeZoneService);
                    endDateUtc = endDateNepal.ToUtcFromNepal(_nepalTimeZoneService);

                    // Ensure UTC kind for PostgreSQL
                    startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                    endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);

                    _logger.LogInformation("🔄 DATE CONVERSION: Start Nepal {StartNepal} → UTC {StartUtc}, End Nepal {EndNepal} → UTC {EndUtc}",
                        startDateNepal.ToString("yyyy-MM-dd HH:mm:ss"),
                        startDateUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                        endDateNepal.ToString("yyyy-MM-dd HH:mm:ss"),
                        endDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));

                    // ✅ VERIFY CONVERSION (Debug)
                    var verifyStartNepal = startDateUtc.ToNepalTime(_nepalTimeZoneService);
                    var verifyEndNepal = endDateUtc.ToNepalTime(_nepalTimeZoneService);
                    
                    if (Math.Abs((verifyStartNepal - startDateNepal).TotalSeconds) > 1 || 
                        Math.Abs((verifyEndNepal - endDateNepal).TotalSeconds) > 1)
                    {
                        _logger.LogWarning("⚠️ Timezone conversion verification failed!");
                        return Result<PromoCodeDTO>.Failure("Timezone conversion error. Please try again.");
                    }

                    _logger.LogDebug("✅ Timezone conversion verified successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Error converting Nepal time to UTC");
                    return Result<PromoCodeDTO>.Failure($"Error converting dates to UTC: {ex.Message}");
                }

                // ✅ 6. CHECK FOR DUPLICATE CODE
                var existingPromoCode = await _promoCodeRepository.FirstOrDefaultAsync(
                    p => p.Code.ToLower() == inputDto.Code.ToLower() && !p.IsDeleted
                  
                );

                if (existingPromoCode != null)
                {
                    return Result<PromoCodeDTO>.Failure($"Promo code '{inputDto.Code}' already exists");
                }

                // ✅ 7. VALIDATE BUSINESS RULES
                var businessValidation = ValidateBusinessRules(inputDto);
                if (!businessValidation.Succeeded)
                {
                    return Result<PromoCodeDTO>.Failure(businessValidation.Message);
                }

                // ✅ 8. CREATE ENTITY WITH UTC DATES
                var promoCodeEntity = inputDto.ToEntity(startDateUtc, endDateUtc, request.CreatedByUserId);

                // ✅ 9. SAVE TO DATABASE
                var savedEntity = await _promoCodeRepository.AddAsync(promoCodeEntity, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                // ✅ 10. CLEAR RELATED CACHE
                await ClearRelatedCache(inputDto.Code);

                // ✅ 11. CONVERT TO DTO WITH NEPAL TIMEZONE INFO
                var result = savedEntity.ToPromoCodeDTO(_nepalTimeZoneService);

                _logger.LogInformation("✅ Promo code created: {Code} ({Id}) by user {UserId} - Duration: {Duration} days, Valid: {StartNepal} to {EndNepal}",
                    result.Code, result.Id, request.CreatedByUserId, Math.Round(durationDays, 1),
                    result.FormattedStartDate, result.FormattedEndDate);

                return Result<PromoCodeDTO>.Success(result, "Promo code created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error creating promo code: {Code} - {Error}", request.PromoCodeDTO.Code, ex.Message);
                return Result<PromoCodeDTO>.Failure($"Error creating promo code: {ex.Message}");
            }
        }

        // ✅ EXTRACTED BUSINESS VALIDATION METHOD
        private Result<PromoCodeDTO> ValidateBusinessRules(AddPromoCodeDTO inputDto)
        {
            if (inputDto.DiscountValue <= 0 || inputDto.DiscountValue > 100)
            {
                return Result<PromoCodeDTO>.Failure("Discount value must be between 0.01 and 100");
            }

            if (inputDto.MaxDiscountAmount < 0)
            {
                return Result<PromoCodeDTO>.Failure("Max discount amount cannot be negative");
            }

            if (inputDto.MinOrderAmount < 0)
            {
                return Result<PromoCodeDTO>.Failure("Min order amount cannot be negative");
            }

            if (inputDto.MaxTotalUsage <= 0)
            {
                return Result<PromoCodeDTO>.Failure("Max total usage must be greater than 0");
            }

            if (inputDto.MaxUsagePerUser <= 0)
            {
                return Result<PromoCodeDTO>.Failure("Max usage per user must be greater than 0");
            }

            // ✅ BUSINESS RULE: Validate discount logic
            if (inputDto.DiscountValue == 100 && inputDto.MinOrderAmount <= 0)
            {
                return Result<PromoCodeDTO>.Failure("100% discount requires a minimum order amount");
            }

            return Result<PromoCodeDTO>.Success(null!,"validation checked");
        }

        // ✅ EXTRACTED CACHE MANAGEMENT METHOD
        private async Task ClearRelatedCache(string promoCode)
        {
            try
            {
                var cacheKeys = new[]
                {
                    "active_promo_codes",
                    $"promo_code_{promoCode.ToLower()}",
                    "valid_promo_codes",
                    "promo_codes_by_category"
                };

                foreach (var key in cacheKeys)
                {
                    await _cache.RemoveAsync(key);
                }

                _logger.LogDebug("🗑️ Cleared {Count} cache entries for promo code: {Code}", cacheKeys.Length, promoCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear cache after creating promo code {Code}", promoCode);
            }
        }
    }
}