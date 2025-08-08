using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Queries
{
    public record GetPromoCodeByIdQuery(int Id) : IRequest<Result<PromoCodeDTO>>;
    
    public class GetPromoCodeByIdQueryHandler : IRequestHandler<GetPromoCodeByIdQuery, Result<PromoCodeDTO>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly INepalTimeZoneService _nepalTimeZoneService;
        private readonly ILogger<GetPromoCodeByIdQueryHandler> _logger;
        
        public GetPromoCodeByIdQueryHandler(
            IPromoCodeRepository promoCodeRepository,
            INepalTimeZoneService nepalTimeZoneService,
            ILogger<GetPromoCodeByIdQueryHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _nepalTimeZoneService = nepalTimeZoneService;
            _logger = logger;
        }
        
        public async Task<Result<PromoCodeDTO>> Handle(GetPromoCodeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                
                var promoCode = await _promoCodeRepository.GetQueryable()
                    .Include(p => p.CreatedByUser)
                    .Include(p => p.LastModifiedByUser)
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);
                
                if (promoCode == null)
                {
                    return Result<PromoCodeDTO>.Failure("Promo code not found");
                }
                
                var result = promoCode.ToPromoCodeDTO();
                return Result<PromoCodeDTO>.Success(result, "Promo code retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promo code {Id}", request.Id);
                return Result<PromoCodeDTO>.Failure($"Error retrieving promo code: {ex.Message}");
            }
        }
    }
}