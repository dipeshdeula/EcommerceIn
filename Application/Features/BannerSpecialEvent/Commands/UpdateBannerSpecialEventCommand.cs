using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.BannerSpecialEvent.Commands
{
    public record UpdateBannerSpecialEventCommand(
        int BannerId,
        string? Name,
        string? Description,
        double? Offers,
        DateTime? StartDate,
        DateTime? EndDate,
        bool? IsActive 

        ) : IRequest<Result<BannerEventSpecialDTO>>;

    public class UpdateBannerSpecialEventCommandHandler : IRequestHandler<UpdateBannerSpecialEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventSpecialRepository;
        public UpdateBannerSpecialEventCommandHandler(IBannerEventSpecialRepository bannerEventSpecialRepository)
        {
            _bannerEventSpecialRepository = bannerEventSpecialRepository;
        }
        public async Task<Result<BannerEventSpecialDTO>> Handle(UpdateBannerSpecialEventCommand request, CancellationToken cancellationToken)
        {
            var banner = await _bannerEventSpecialRepository.FindByIdAsync(request.BannerId);
            if (banner == null)
                return Result<BannerEventSpecialDTO>.Failure("Banner Id not found");

            banner.Name = request.Name ?? banner.Name;
            banner.Description = request.Description ?? banner.Description;
            banner.Offers = request.Offers ?? banner.Offers ;
            banner.StartDate = request.StartDate ?? banner.StartDate;
            banner.EndDate = request.EndDate ?? banner.EndDate;
            banner.IsActive = request.IsActive ?? banner.IsActive;

            await _bannerEventSpecialRepository.UpdateAsync(banner, cancellationToken);

            return Result<BannerEventSpecialDTO>.Success(banner.ToDTO(),"BannerEventSpecial is updated successfully");



        }
    }

}
