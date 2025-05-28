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

namespace Application.Features.BannerSpecialEvent.Queries
{
    public record GetAllBannerEventSpecialQuery (int PageNumber,int PageSize) : IRequest<Result<IEnumerable<BannerEventSpecialDTO>>>;

    public class GetAllBannerEventSpecialQueryHandler : IRequestHandler<GetAllBannerEventSpecialQuery, Result<IEnumerable<BannerEventSpecialDTO>>>
    {
        private readonly IBannerEventSpecialRepository _bannerEventSpecialRepository;
        public GetAllBannerEventSpecialQueryHandler(IBannerEventSpecialRepository bannerEventSpecialRepository)
        {
            _bannerEventSpecialRepository = bannerEventSpecialRepository;
        }
        public async Task<Result<IEnumerable<BannerEventSpecialDTO>>> Handle(GetAllBannerEventSpecialQuery request, CancellationToken cancellationToken)
        {
            var bannerEvents = await _bannerEventSpecialRepository.GetAllAsync(
                     orderBy: query => query.OrderByDescending(bannerEvents => bannerEvents.Id),
                     skip: (request.PageNumber - 1) * request.PageSize,
                     take: request.PageSize,
                     includeProperties:"Images");
                     

            var bannerEventsDTOs = bannerEvents.Select(be => be.ToDTO()).ToList();

            return Result<IEnumerable<BannerEventSpecialDTO>>.Success(bannerEventsDTOs, "Banner events fetched successfully");
                  
        }
    }

}
