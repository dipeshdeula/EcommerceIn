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
    public record ValidateLocationQuery(LocationRequestDTO Location, int? UserId = null) : IRequest<Result<LocationValidationResponseDTO>>;

    public class ValidateLocationQueryHandler : IRequestHandler<ValidateLocationQuery, Result<LocationValidationResponseDTO>>
    {
        private readonly ILocationService _locationService;

        public ValidateLocationQueryHandler(ILocationService locationService)
        {
            _locationService = locationService;
        }

        public async Task<Result<LocationValidationResponseDTO>> Handle(ValidateLocationQuery request, CancellationToken cancellationToken)
        {
            return await _locationService.ValidateLocationAsync(request.Location, request.UserId, cancellationToken);
        }
    }
}
