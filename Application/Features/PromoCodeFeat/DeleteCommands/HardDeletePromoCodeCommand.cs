using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.DeleteCommands
{
    public record HardDeletePromoCodeCommand(int Id, int DeletedByUserId) : IRequest<Result<bool>>;

    public class HardDeletePromoCodeCommandHandler : IRequestHandler<HardDeletePromoCodeCommand, Result<bool>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<HardDeletePromoCodeCommandHandler> _logger;

        public HardDeletePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            IDistributedCache cache,
            ILogger<HardDeletePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(HardDeletePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var promoCode = await _promoCodeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (promoCode == null)
                {
                    return Result<bool>.Failure("Promo code not found");
                }

                // HARD DELETE
                await _promoCodeRepository.RemoveAsync(promoCode, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                // CLEAR CACHE
                await _cache.RemoveAsync($"promo_code_{promoCode.Code.ToLower()}");

                _logger.LogInformation("Hard deleted promo code {Id} by user {UserId}", request.Id, request.DeletedByUserId);
                return Result<bool>.Success(true, "Promo code permanently deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting promo code {Id}", request.Id);
                return Result<bool>.Failure($"Error permanently deleting promo code: {ex.Message}");
            }
        }
    }
}