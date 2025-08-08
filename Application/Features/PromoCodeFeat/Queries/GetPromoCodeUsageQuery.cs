using Application.Common;
using Application.Dto.PromoCodeDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Queries
{
    public record GetPromoCodeUsageQuery(int PromoCodeId) : IRequest<Result<List<PromoCodeUsageDTO>>>;
    
    public class GetPromoCodeUsageQueryHandler : IRequestHandler<GetPromoCodeUsageQuery, Result<List<PromoCodeUsageDTO>>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly ILogger<GetPromoCodeUsageQueryHandler> _logger;
        
        public GetPromoCodeUsageQueryHandler(
            IPromoCodeRepository promoCodeRepository,
            ILogger<GetPromoCodeUsageQueryHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _logger = logger;
        }
        
        public async Task<Result<List<PromoCodeUsageDTO>>> Handle(GetPromoCodeUsageQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify promo code exists
                var promoCodeExists = await _promoCodeRepository.FirstOrDefaultAsync(
                    predicate: p => p.Id == request.PromoCodeId && !p.IsDeleted
                   
                );
                
                if (promoCodeExists == null)
                {
                    return Result<List<PromoCodeUsageDTO>>.Failure("Promo code not found");
                }
                
                // Get usage history - you'll need to implement this in repository
                var usageHistory = await _promoCodeRepository.GetUserUsageHistoryAsync(
                    promoCodeExists.Id, cancellationToken);
                
                var usageDTOs = usageHistory
                    .Where(u => u.PromoCodeId == request.PromoCodeId)
                    .Select(u => u.ToPromoCodeUsageDTO())
                    .ToList();
                
                return Result<List<PromoCodeUsageDTO>>.Success(usageDTOs, 
                    $"Retrieved {usageDTOs.Count} usage records for promo code");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promo code usage for {PromoCodeId}", request.PromoCodeId);
                return Result<List<PromoCodeUsageDTO>>.Failure($"Error retrieving usage history: {ex.Message}");
            }
        }
    }
}