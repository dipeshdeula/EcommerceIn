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
    public record SoftDeleteBannerEventCommand (
        int BannerId        
        ) : IRequest<Result<BannerEventSpecialDTO>>;
    public class SoftDeleteBannerEventCommandHandler : IRequestHandler<SoftDeleteBannerEventCommand, Result<BannerEventSpecialDTO>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventSpecialRepository;
        public SoftDeleteBannerEventCommandHandler(IBannerEventSpecialRepository bannerEventSpecialRepository)
        {
            _bannerEventSpecialRepository = bannerEventSpecialRepository;
            
        }

        public async Task<Result<BannerEventSpecialDTO>> Handle(SoftDeleteBannerEventCommand request, CancellationToken cancellationToken)
        {
            var banner = await _bannerEventSpecialRepository.FindByIdAsync( request.BannerId );
            if (banner == null)
                return Result<BannerEventSpecialDTO>.Failure("Banner Id is not found");

            await _bannerEventSpecialRepository.SoftDeleteAsync( banner );
            await _bannerEventSpecialRepository.SaveChangesAsync(cancellationToken);

            return Result<BannerEventSpecialDTO>.Success(banner.ToDTO(),"Banner Special Event is Deleted successfully");
        }
    }
}
