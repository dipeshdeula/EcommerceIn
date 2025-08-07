using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.LocationFeat.Commands
{
    public record SaveUserLocationCommand(int UserId, LocationRequestDTO Location) : IRequest<Result<Address>>;

    public class SaveUserLocationCommandHandler : IRequestHandler<SaveUserLocationCommand, Result<Address>>
    {
        private readonly ILocationService _locationService;

        public SaveUserLocationCommandHandler(ILocationService locationService)
        {
            _locationService = locationService;
        }

        public async Task<Result<Address>> Handle(SaveUserLocationCommand request, CancellationToken cancellationToken)
        {
            return await _locationService.SaveUserLocationAsync(request.UserId, request.Location, cancellationToken);
        }
    }
}
