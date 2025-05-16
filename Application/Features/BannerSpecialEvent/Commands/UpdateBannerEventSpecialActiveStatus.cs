using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record UpdateBannerEventSpecialActiveStatus(
        int BannerId,
        bool IsActive
        ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class UpdateBannerEventSpecialActiveStatusHandler : IRequestHandler<UpdateBannerEventSpecialActiveStatus, Result<BannerEventSpecialDTO>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventSpecialRepository;
        public UpdateBannerEventSpecialActiveStatusHandler(IBannerEventSpecialRepository bannerEventSpecialRepository)
        {
            _bannerEventSpecialRepository = bannerEventSpecialRepository;

        }
        public async Task<Result<BannerEventSpecialDTO>> Handle(UpdateBannerEventSpecialActiveStatus request, CancellationToken cancellationToken)
        {
            var banner = await _bannerEventSpecialRepository.FindByIdAsync(request.BannerId);
            if (banner == null)
                return Result<BannerEventSpecialDTO>.Failure("Banner Id is not found");

            banner.IsActive = request.IsActive;

            await _bannerEventSpecialRepository.UpdateAsync(banner, cancellationToken);

            if (banner.IsActive == true)
            {
                return Result<BannerEventSpecialDTO>.Success(banner.ToDTO(), "Banner Special Event is Activate now");

            }
            else
            {
                return Result<BannerEventSpecialDTO>.Success(banner.ToDTO(), "Banner Special Event is Deactivate now");

            }

        }
    }

}
