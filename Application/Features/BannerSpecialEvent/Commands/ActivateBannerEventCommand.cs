using Application.Common;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums.BannerEventSpecial;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record ActivateBannerEventCommand(
       int BannerEventId,
       bool IsActive
   ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class ActivateBannerEventCommandHandler : IRequestHandler<ActivateBannerEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventRepository;
        private readonly IEventValidationService _validationService;
        private readonly ICacheService _cacheService;

        public ActivateBannerEventCommandHandler(
            IBannerEventSpecialRepository bannerEventRepository,
            IEventValidationService validationService,
            ICacheService cacheService)
        {
            _bannerEventRepository = bannerEventRepository;
            _validationService = validationService;
            _cacheService = cacheService;
        }

        public async Task<Result<BannerEventSpecialDTO>> Handle(ActivateBannerEventCommand request, CancellationToken cancellationToken)
        {
            var bannerEvent = await _bannerEventRepository.FindByIdAsync(request.BannerEventId);
            if (bannerEvent == null)
                return Result<BannerEventSpecialDTO>.Failure("Banner event not found");

            if (request.IsActive)
            {
                // Validate if event can be activated
                var canActivate = await _validationService.CanActivateEventAsync(request.BannerEventId);
                if (!canActivate)
                    return Result<BannerEventSpecialDTO>.Failure("Event cannot be activated. Please check event configuration and dates.");

                bannerEvent.IsActive = true;
                bannerEvent.Status = EventStatus.Active;

                // Clear promotion cache when activating
                await _cacheService.RemovePatternAsync("promotions:*");

                await _bannerEventRepository.UpdateAsync(bannerEvent, cancellationToken);

                return Result<BannerEventSpecialDTO>.Success(
                    bannerEvent.ToDTO(),
                    "Banner event activated successfully");
            }
            else
            {
                bannerEvent.IsActive = false;
                bannerEvent.Status = EventStatus.Paused;

                // Clear promotion cache when deactivating
                await _cacheService.RemovePatternAsync("promotions:*");

                await _bannerEventRepository.UpdateAsync(bannerEvent, cancellationToken);

                return Result<BannerEventSpecialDTO>.Success(
                    bannerEvent.ToDTO(),
                    "Banner event deactivated successfully");
            }
        }
    }
}
