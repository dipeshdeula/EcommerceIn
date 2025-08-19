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
    public record UpdatePromoCodeCommand(
        int Id,
        UpdatePromoCodeDTO PromoCodeData,
        int ModifiedByUserId
    ) : IRequest<Result<PromoCodeDTO>>;
    
    public class UpdatePromoCodeCommandHandler : IRequestHandler<UpdatePromoCodeCommand, Result<PromoCodeDTO>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UpdatePromoCodeCommandHandler> _logger;
        
        public UpdatePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            INepalTimeZoneService nepalTimeZoneService,
            IDistributedCache cache,
            ILogger<UpdatePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _nepalTimeZoneService = nepalTimeZoneService;
            _cache = cache;
            _logger = logger;
        }
        
        public async Task<Result<PromoCodeDTO>> Handle(UpdatePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var inputDto = request.PromoCodeData;
                
                var promoCode = await _promoCodeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (promoCode == null || promoCode.IsDeleted)
                {
                    return Result<PromoCodeDTO>.Failure("Promo code not found");
                }

                _logger.LogInformation("🔄 Updating promo code {Id}: {Code}", request.Id, promoCode.Code);

                //  VALIDATE DATE FORMATS FIRST USING TimeParsingHelper
                if (!inputDto.HasValidDateFormats)
                {
                    var errors = new List<string>();
                    if (!string.IsNullOrEmpty(inputDto.StartDateParsingError))
                        errors.Add($"Start Date: {inputDto.StartDateParsingError}");
                    if (!string.IsNullOrEmpty(inputDto.EndDateParsingError))
                        errors.Add($"End Date: {inputDto.EndDateParsingError}");
                    
                    return Result<PromoCodeDTO>.Failure($"Invalid date format(s): {string.Join(", ", errors)}");
                }

                //  1. UPDATE TEXT FIELDS (only if provided)
                if (!string.IsNullOrWhiteSpace(inputDto.Name))
                    promoCode.Name = inputDto.Name.Trim();
                
                if (!string.IsNullOrWhiteSpace(inputDto.Description))
                    promoCode.Description = inputDto.Description.Trim();
                
                if (!string.IsNullOrWhiteSpace(inputDto.AdminNotes))
                    promoCode.AdminNotes = inputDto.AdminNotes.Trim();

                //  2. UPDATE NUMERIC FIELDS (only if provided and valid)
                if (inputDto.DiscountValue.HasValue)
                {
                    if (inputDto.DiscountValue.Value <= 0 || inputDto.DiscountValue.Value > 100)
                    {
                        return Result<PromoCodeDTO>.Failure("Discount value must be between 0.01 and 100");
                    }
                    promoCode.DiscountValue = inputDto.DiscountValue.Value;
                }
                
                if (inputDto.MaxDiscountAmount.HasValue)
                {
                    if (inputDto.MaxDiscountAmount.Value < 0)
                    {
                        return Result<PromoCodeDTO>.Failure("Max discount amount cannot be negative");
                    }
                    promoCode.MaxDiscountAmount = inputDto.MaxDiscountAmount.Value;
                }
                
                if (inputDto.MinOrderAmount.HasValue)
                {
                    if (inputDto.MinOrderAmount.Value < 0)
                    {
                        return Result<PromoCodeDTO>.Failure("Min order amount cannot be negative");
                    }
                    promoCode.MinOrderAmount = inputDto.MinOrderAmount.Value;
                }
                
                if (inputDto.MaxTotalUsage.HasValue)
                {
                    if (inputDto.MaxTotalUsage.Value <= 0)
                    {
                        return Result<PromoCodeDTO>.Failure("Max total usage must be greater than 0");
                    }
                    promoCode.MaxTotalUsage = inputDto.MaxTotalUsage.Value;
                }
                
                if (inputDto.MaxUsagePerUser.HasValue)
                {
                    if (inputDto.MaxUsagePerUser.Value <= 0)
                    {
                        return Result<PromoCodeDTO>.Failure("Max usage per user must be greater than 0");
                    }
                    promoCode.MaxUsagePerUser = inputDto.MaxUsagePerUser.Value;
                }

                //  3. UPDATE BOOLEAN FLAGS (only if provided)
                if (inputDto.IsActive.HasValue)
                    promoCode.IsActive = inputDto.IsActive.Value;
                
                if (inputDto.ApplyToShipping.HasValue)
                    promoCode.ApplyToShipping = inputDto.ApplyToShipping.Value;
                
                if (inputDto.StackableWithEvents.HasValue)
                    promoCode.StackableWithEvents = inputDto.StackableWithEvents.Value;

                //  4. HANDLE NEPAL TIMEZONE DATE UPDATES USING TimeParsingHelper
                DateTime? newStartDateUtc = null;
                DateTime? newEndDateUtc = null;

                if (inputDto.HasDateUpdates)
                {
                    _logger.LogInformation("📅 Processing Nepal date updates for promo code {Id}", request.Id);

                    //  PARSE START DATE USING TimeParsingHelper
                    if (!string.IsNullOrEmpty(inputDto.StartDateNepal))
                    {
                        var startDateResult = TimeParsingHelper.ParseFlexibleDateTime(inputDto.StartDateNepal);
                        if (!startDateResult.Succeeded)
                        {
                            return Result<PromoCodeDTO>.Failure($"Invalid start date: {startDateResult.Message}. " +
                                $"Supported formats: {string.Join(", ", TimeParsingHelper.GetSupportedFormats())}");
                        }

                        var startDateNepal = startDateResult.Data;

                        //  CONVERT TO UTC WITH PROPER LOGGING
                        try
                        {
                            newStartDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(startDateNepal);
                            newStartDateUtc = DateTime.SpecifyKind(newStartDateUtc.Value, DateTimeKind.Utc);

                            _logger.LogInformation("📅 START DATE CONVERSION: Input '{Input}' → Parsed '{NepalParsed}' → UTC '{UtcResult}'", 
                                inputDto.StartDateNepal,
                                TimeParsingHelper.FormatForNepalDisplay(startDateNepal),
                                newStartDateUtc.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, " Error converting start date from Nepal to UTC");
                            return Result<PromoCodeDTO>.Failure($"Error converting start date: {ex.Message}");
                        }
                    }

                    //  PARSE END DATE USING TimeParsingHelper
                    if (!string.IsNullOrEmpty(inputDto.EndDateNepal))
                    {
                        var endDateResult = TimeParsingHelper.ParseFlexibleDateTime(inputDto.EndDateNepal);
                        if (!endDateResult.Succeeded)
                        {
                            return Result<PromoCodeDTO>.Failure($"Invalid end date: {endDateResult.Message}. " +
                                $"Supported formats: {string.Join(", ", TimeParsingHelper.GetSupportedFormats())}");
                        }

                        var endDateNepal = endDateResult.Data;

                        //  CONVERT TO UTC WITH PROPER LOGGING
                        try
                        {
                            newEndDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(endDateNepal);
                            newEndDateUtc = DateTime.SpecifyKind(newEndDateUtc.Value, DateTimeKind.Utc);

                            _logger.LogInformation("📅 END DATE CONVERSION: Input '{Input}' → Parsed '{NepalParsed}' → UTC '{UtcResult}'", 
                                inputDto.EndDateNepal,
                                TimeParsingHelper.FormatForNepalDisplay(endDateNepal),
                                newEndDateUtc.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, " Error converting end date from Nepal to UTC");
                            return Result<PromoCodeDTO>.Failure($"Error converting end date: {ex.Message}");
                        }
                    }

                    //  VALIDATE DATE RANGE AFTER CONVERSION
                    var effectiveStartDate = newStartDateUtc ?? promoCode.StartDate;
                    var effectiveEndDate = newEndDateUtc ?? promoCode.EndDate;

                    if (effectiveEndDate <= effectiveStartDate)
                    {
                        var startDisplay = newStartDateUtc.HasValue ? 
                            TimeParsingHelper.FormatForNepalDisplay(_nepalTimeZoneService.ConvertFromUtcToNepal(effectiveStartDate)) :
                            TimeParsingHelper.FormatForNepalDisplay(_nepalTimeZoneService.ConvertFromUtcToNepal(promoCode.StartDate));
                        
                        var endDisplay = newEndDateUtc.HasValue ? 
                            TimeParsingHelper.FormatForNepalDisplay(_nepalTimeZoneService.ConvertFromUtcToNepal(effectiveEndDate)) :
                            TimeParsingHelper.FormatForNepalDisplay(_nepalTimeZoneService.ConvertFromUtcToNepal(promoCode.EndDate));

                        return Result<PromoCodeDTO>.Failure($"End date ({endDisplay}) must be after start date ({startDisplay})");
                    }

                    //  VALIDATE START DATE NOT TOO FAR IN PAST
                    if (newStartDateUtc.HasValue)
                    {
                        var currentNepalTime = _nepalTimeZoneService.GetNepalCurrentTime();
                        var startNepalTime = _nepalTimeZoneService.ConvertFromUtcToNepal(newStartDateUtc.Value);
                        var timeDiff = currentNepalTime - startNepalTime;
                        
                        if (timeDiff.TotalHours > 1)
                        {
                            return Result<PromoCodeDTO>.Failure($"Start time cannot be more than 1 hour in the past. " +
                                $"Current Nepal time: {TimeParsingHelper.FormatForNepalDisplay(currentNepalTime)}, " +
                                $"Your start time: {TimeParsingHelper.FormatForNepalDisplay(startNepalTime)}");
                        }
                    }

                    //  UPDATE DATES IN DATABASE (UTC)
                    if (newStartDateUtc.HasValue)
                    {
                        promoCode.StartDate = newStartDateUtc.Value;
                        _logger.LogInformation(" Updated start date in database: {UtcDate}", newStartDateUtc.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    
                    if (newEndDateUtc.HasValue)
                    {
                        promoCode.EndDate = newEndDateUtc.Value;
                        _logger.LogInformation(" Updated end date in database: {UtcDate}", newEndDateUtc.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }

                //  5. UPDATE AUDIT FIELDS
                promoCode.LastModifiedByUserId = request.ModifiedByUserId;
                promoCode.UpdatedAt = DateTime.UtcNow;
                
                //  6. SAVE CHANGES
                await _promoCodeRepository.UpdateAsync(promoCode, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);
                
                //  7. CLEAR CACHE
                await _cache.RemoveAsync($"promo_code_{promoCode.Code.ToLower()}");
                
                //  8. CONVERT TO DTO WITH NEPAL TIMEZONE INFO
                var result = promoCode.ToPromoCodeDTO(_nepalTimeZoneService);
                
                _logger.LogInformation(" Updated promo code {Id}: {Code} by user {UserId} - Active: {IsActive}, Valid: {StartDate} to {EndDate}",
                    request.Id, promoCode.Code, request.ModifiedByUserId, result.IsActive, 
                    result.FormattedStartDate, result.FormattedEndDate);
                
                return Result<PromoCodeDTO>.Success(result, "Promo code updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error updating promo code {Id}: {Error}", request.Id, ex.Message);
                return Result<PromoCodeDTO>.Failure($"Error updating promo code: {ex.Message}");
            }
        }
    }
}