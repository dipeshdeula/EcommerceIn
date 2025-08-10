using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Queries
{
    public record GetPromoCodeUsageQuery(int PageNumber,int PageSize) : IRequest<Result<IEnumerable<PromoCodeUsageDTO>>>;
    
    public class GetPromoCodeUsageQueryHandler : IRequestHandler<GetPromoCodeUsageQuery, Result<IEnumerable<PromoCodeUsageDTO>>>
    {
        private readonly IPromocodeUsageRepository _promoCodeUsageRepository;
        private readonly ILogger<GetPromoCodeUsageQueryHandler> _logger;
        
        public GetPromoCodeUsageQueryHandler(
            IPromocodeUsageRepository promoCodeUsageRepository,
            ILogger<GetPromoCodeUsageQueryHandler> logger)
        {
            _promoCodeUsageRepository = promoCodeUsageRepository;
            _logger = logger;
        }
        
        public async Task<Result<IEnumerable<PromoCodeUsageDTO>>> Handle(GetPromoCodeUsageQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify promo code exists
                var promoCodeExists = await _promoCodeUsageRepository.GetAllAsync(
                    orderBy: q => q.OrderByDescending(promoUsage => promoUsage.Id),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take : request.PageSize,
                    cancellationToken: cancellationToken

                );
                
                if (promoCodeExists == null)
                {
                    return Result<IEnumerable<PromoCodeUsageDTO>>.Failure("Promo Usage not found");
                }

                var promoUsageDTO = promoCodeExists.Select(p => p.ToPromoCodeUsageDTO());
                
                
                
                return Result<IEnumerable<PromoCodeUsageDTO>>.Success(promoUsageDTO, 
                    $"Retrieved Promo usage successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promo code usage ");
                return Result<IEnumerable<PromoCodeUsageDTO>>.Failure($"Error retrieving usage history: {ex.Message}");
            }
        }
    }
}