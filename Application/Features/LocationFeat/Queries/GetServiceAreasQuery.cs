using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Interfaces.Services;
using MediatR;

namespace Application.Features.LocationFeat.Queries
{
    public record GetServiceAreasQuery(int pageNumber , int pageSize,bool ActiveOnly = true) : IRequest<Result<IEnumerable<ServiceAreaDTO>>>;

    public class GetServiceAreasQueryHandler : IRequestHandler<GetServiceAreasQuery, Result<IEnumerable<ServiceAreaDTO>>>
    {
        private readonly ILocationService _locationService;

        public GetServiceAreasQueryHandler(ILocationService locationService)
        {
            _locationService = locationService;
        }

        public async Task<Result<IEnumerable<ServiceAreaDTO>>> Handle(GetServiceAreasQuery request, CancellationToken cancellationToken)
        {
            return await _locationService.GetAllServiceAreasAsync(request.pageNumber,request.pageSize,request.ActiveOnly, cancellationToken);
        }
    }
}
