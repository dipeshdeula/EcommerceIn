using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Application.Features.PromoCodeFeat.DeleteCommands
{
    public record SoftDeletePromoCodeCommand(int Id, int DeletedByUserId) : IRequest<Result<bool>>;

    public class SoftDeletePromoCodeCommandHandler : IRequestHandler<SoftDeletePromoCodeCommand, Result<bool>>
    {
        private readonly IPromoCodeRepository _promoCodeRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<SoftDeletePromoCodeCommandHandler> _logger;

        public SoftDeletePromoCodeCommandHandler(
            IPromoCodeRepository promoCodeRepository,
            IDistributedCache cache,
            ILogger<SoftDeletePromoCodeCommandHandler> logger)
        {
            _promoCodeRepository = promoCodeRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(SoftDeletePromoCodeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var promoCode = await _promoCodeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (promoCode == null || promoCode.IsDeleted)
                {
                    return Result<bool>.Failure("Promo code not found");
                }

                //  SOFT DELETE
                promoCode.IsDeleted = true;
                promoCode.IsActive = false;
                promoCode.LastModifiedByUserId = request.DeletedByUserId;
                promoCode.UpdatedAt = DateTime.UtcNow;

                await _promoCodeRepository.UpdateAsync(promoCode, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);

                //  CLEAR CACHE
                await _cache.RemoveAsync($"promo_code_{promoCode.Code.ToLower()}");

                _logger.LogInformation("Soft deleted promo code {Id} by user {UserId}", request.Id, request.DeletedByUserId);
                return Result<bool>.Success(true, "Promo code deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting promo code {Id}", request.Id);
                return Result<bool>.Failure($"Error deleting promo code: {ex.Message}");
            }
        }
    }
}