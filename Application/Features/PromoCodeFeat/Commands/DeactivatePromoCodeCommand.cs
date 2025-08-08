using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Commands
{
    public record DeactivatePromoCodeCommand(int Id, int ModifiedByUserId) : IRequest<Result<bool>>;

    public class DeactivatePromoCodeCommandHandler : IRequestHandler<DeactivatePromoCodeCommand, Result<bool>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DeactivatePromoCodeCommandHandler> _logger;

        public DeactivatePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            IDistributedCache cache,
            ILogger<DeactivatePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(DeactivatePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var promoCode = await _promoCodeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (promoCode == null || promoCode.IsDeleted)
                {
                    return Result<bool>.Failure("Promo code not found");
                }

                promoCode.IsActive = false;
                promoCode.UpdatedAt = DateTime.UtcNow;
                promoCode.LastModifiedByUserId = request.ModifiedByUserId;

                await _promoCodeRepository.UpdateAsync(promoCode, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync($"promo_code_{promoCode.Code.ToLower()}");

                _logger.LogInformation("Deactivated promo code {Id} by user {UserId}", request.Id, request.ModifiedByUserId);
                return Result<bool>.Success(true, "Promo code deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating promo code {Id}", request.Id);
                return Result<bool>.Failure($"Error deactivating promo code: {ex.Message}");
            }
        }
    }
}