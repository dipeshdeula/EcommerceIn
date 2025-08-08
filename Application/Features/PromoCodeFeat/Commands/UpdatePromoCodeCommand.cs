using Application.Common;
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
                var promoCode = await _promoCodeRepository.GetByIdAsync(request.Id, cancellationToken);
                if (promoCode == null || promoCode.IsDeleted)
                {
                    return Result<PromoCodeDTO>.Failure("Promo code not found");
                }
                
                // UPDATE PROPERTIES
                promoCode.Name = request.PromoCodeData.Name ?? promoCode.Name;
                promoCode.Description = request.PromoCodeData.Description ?? promoCode.Description;
                promoCode.DiscountValue = request.PromoCodeData.DiscountValue ?? 0;
                promoCode.MaxDiscountAmount = request.PromoCodeData.MaxDiscountAmount ?? promoCode.MaxDiscountAmount;
                promoCode.MinOrderAmount = request.PromoCodeData.MinOrderAmount ?? promoCode.MinOrderAmount;
                promoCode.MaxTotalUsage = request.PromoCodeData.MaxTotalUsage ?? promoCode.MaxTotalUsage;
                promoCode.MaxUsagePerUser = request.PromoCodeData.MaxUsagePerUser ?? promoCode.MaxUsagePerUser;
                
                // ENSURE UTC DATES
                promoCode.StartDate = DateTime.SpecifyKind(request.PromoCodeData.StartDate ?? promoCode.StartDate, DateTimeKind.Utc);
                promoCode.EndDate = DateTime.SpecifyKind(request.PromoCodeData.EndDate ?? promoCode.EndDate, DateTimeKind.Utc);
                
                promoCode.IsActive = request.PromoCodeData.IsActive ?? promoCode.IsActive;
                promoCode.ApplyToShipping = request.PromoCodeData.ApplyToShipping ?? promoCode.ApplyToShipping;
                promoCode.StackableWithEvents = request.PromoCodeData.StackableWithEvents ?? promoCode.StackableWithEvents;
                promoCode.AdminNotes = request.PromoCodeData.AdminNotes ?? promoCode.AdminNotes;
                promoCode.LastModifiedByUserId = request.ModifiedByUserId ;
                promoCode.UpdatedAt = DateTime.UtcNow;
                
                await _promoCodeRepository.UpdateAsync(promoCode, cancellationToken);
                await _promoCodeRepository.SaveChangesAsync(cancellationToken);
                
                // CLEAR CACHE
                await _cache.RemoveAsync($"promo_code_{promoCode.Code.ToLower()}");
                
                // CONVERT TO DTO WITH NEPAL TIMEZONE SERVICE
                var result = promoCode.ToPromoCodeDTO(_nepalTimeZoneService);
                _logger.LogInformation("Updated promo code {Id} by user {UserId}", request.Id, request.ModifiedByUserId);
                
                return Result<PromoCodeDTO>.Success(result, "Promo code updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promo code {Id}", request.Id);
                return Result<PromoCodeDTO>.Failure($"Error updating promo code: {ex.Message}");
            }
        }
    }
}