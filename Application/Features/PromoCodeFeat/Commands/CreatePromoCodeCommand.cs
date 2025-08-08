using Application.Common;
using Application.Common.Helper;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Commands
{
    public record CreatePromoCodeCommand(AddPromoCodeDTO PromoCodeDTO, int CreatedByUserId) : IRequest<Result<PromoCodeDTO>>;
    
    public class CreatePromoCodeCommandHandler : IRequestHandler<CreatePromoCodeCommand, Result<PromoCodeDTO>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly ILogger<CreatePromoCodeCommandHandler> _logger;

        
        public CreatePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            INepalTimeZoneService nepalTimeZoneService,            
            ILogger<CreatePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _nepalTimeZoneService = nepalTimeZoneService;
            _logger = logger;
        }
        
        public async Task<Result<PromoCodeDTO>> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var inputDto = request.PromoCodeDTO;
                
                //  VALIDATE AND CONVERT NEPAL TIMES TO UTC
                if (!DateTime.TryParse(inputDto.StartDateNepal, out var startDateNepal))
                {
                    return Result<PromoCodeDTO>.Failure("Invalid start date format. Use YYYY-MM-DDTHH:mm:ss");
                }
                
                if (!DateTime.TryParse(inputDto.EndDateNepal, out var endDateNepal))
                {
                    return Result<PromoCodeDTO>.Failure("Invalid end date format. Use YYYY-MM-DDTHH:mm:ss");
                }
                
                //  VALIDATE DATE RANGE
                if (endDateNepal <= startDateNepal)
                {
                    return Result<PromoCodeDTO>.Failure("End date must be after start date");
                }
                
                //  VALIDATE START DATE NOT TOO FAR IN PAST
                var currentNepalTime = _nepalTimeZoneService.GetNepalCurrentTime();
                var timeDiff = currentNepalTime - startDateNepal;
                if (timeDiff.TotalHours > 1)
                {
                    return Result<PromoCodeDTO>.Failure($"Start time cannot be more than 1 hour in the past. " +
                        $"Current Nepal time: {TimeParsingHelper.FormatForNepalDisplay(currentNepalTime)}, " +
                        $"Your start time: {TimeParsingHelper.FormatForNepalDisplay(startDateNepal)}");
                }
                
                //  CONVERT TO UTC WITH PROPER KIND HANDLING
                var startDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(startDateNepal);
                var endDateUtc = _nepalTimeZoneService.ConvertFromNepalToUtc(endDateNepal);
                
                //  ENSURE UTC KIND FOR POSTGRESQL
                startDateUtc = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
                endDateUtc = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);
                
                _logger.LogDebug("Time conversion: Nepal {StartNepal}-{EndNepal} → UTC {StartUtc}-{EndUtc}",
                    startDateNepal.ToString("yyyy-MM-dd HH:mm:ss"), endDateNepal.ToString("yyyy-MM-dd HH:mm:ss"),
                    startDateUtc.ToString("yyyy-MM-dd HH:mm:ss"), endDateUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                
                //  CHECK FOR DUPLICATE CODE
                var existingPromoCode = await _promoCodeRepository.FirstOrDefaultAsync(
                    p => p.Code == inputDto.Code && !p.IsDeleted
                    
                );
                
                if (existingPromoCode != null)
                {
                    return Result<PromoCodeDTO>.Failure($"Promo code '{inputDto.Code}' already exists");
                }
                
                //  CREATE ENTITY WITH UTC DATES
                var promoCodeEntity = inputDto.ToEntity(startDateUtc, endDateUtc, request.CreatedByUserId);
                
                //  SAVE TO DATABASE
                var savedEntity = await _promoCodeRepository.AddAsync(promoCodeEntity, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                
                //  CONVERT TO DTO WITH NEPAL TIMEZONE INFO
                var result = savedEntity.ToPromoCodeDTO(_nepalTimeZoneService);
                
                _logger.LogInformation(" Promo code created: {Code} ({Id}) - Valid from {Start} to {End} Nepal Time",
                    result.Code, result.Id, result.FormattedStartDate, result.FormattedEndDate);
                
                return Result<PromoCodeDTO>.Success(result, "Promo code created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promo code: {Code}", request.PromoCodeDTO.Code);
                return Result<PromoCodeDTO>.Failure($"Error creating promo code: {ex.Message}");
            }
        }
    }
}