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

namespace Application.Features.BannerSpecialEvent.DeleteCommands
{
    public record UnDeleteBannerEventCommand (int BannerId) : IRequest<Result<BannerEventSpecialDTO>>;

    public class UnDeleteBannerEventCommandHandler : IRequestHandler<UnDeleteBannerEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventSpecialRepository;
        public UnDeleteBannerEventCommandHandler(IBannerEventSpecialRepository bannerEventSpecialRepository)
        {
            _bannerEventSpecialRepository = bannerEventSpecialRepository;
            
        }
        public async Task<Result<BannerEventSpecialDTO>> Handle(UnDeleteBannerEventCommand request, CancellationToken cancellationToken)
        {
           var banner = await _bannerEventSpecialRepository.FindByIdAsync(request.BannerId);
            if (banner == null)
                return Result<BannerEventSpecialDTO>.Failure("Banner Id is not found");

            await _bannerEventSpecialRepository.UndeleteAsync(banner,cancellationToken);
            await _bannerEventSpecialRepository.SaveChangesAsync(cancellationToken);

            return Result<BannerEventSpecialDTO>.Success(banner.ToDTO(), "Banner is unDeleted successfully");
        }
    }
}
