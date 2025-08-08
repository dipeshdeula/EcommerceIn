using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.Commands
{
    public record ActivatePromoCodeCommand(int Id, int ModifiedByUserId) : IRequest<Result<bool>>;

    public class ActivatePromoCodeCommandHandler : IRequestHandler<ActivatePromoCodeCommand, Result<bool>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ActivatePromoCodeCommandHandler> _logger;

        public ActivatePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            IDistributedCache cache,
            ILogger<ActivatePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(ActivatePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var promoCode = await _promoCodeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (promoCode == null || promoCode.IsDeleted)
                {
                    return Result<bool>.Failure("Promo code not found");
                }

                promoCode.IsActive = true;
                promoCode.CreatedAt = DateTime.UtcNow;
                promoCode.LastModifiedByUserId = request.ModifiedByUserId;
                promoCode.UpdatedAt = DateTime.UtcNow;

                await _promoCodeRepository.UpdateAsync(promoCode, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync($"promo_code_{promoCode.Code.ToLower()}");

                _logger.LogInformation("Activated promo code {Id} by user {UserId}", request.Id, request.ModifiedByUserId);
                return Result<bool>.Success(true, "Promo code activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating promo code {Id}", request.Id);
                return Result<bool>.Failure($"Error activating promo code: {ex.Message}");
            }
        }
    }
}