using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.LocationFeat.DeleCommands
{
    public record HardDeleteServiceAreaCommand (int id) : IRequest<Result<string>>;

    public class HardDeleteSoftAreaCommandHandler : IRequestHandler<HardDeleteServiceAreaCommand, Result<string>>
    {
        private readonly IServiceAreaRepository _serviceAreaRepository;
        public HardDeleteSoftAreaCommandHandler(IServiceAreaRepository serviceAreaRepository)
        {
            _serviceAreaRepository = serviceAreaRepository;
        }
        public async Task<Result<string>> Handle(HardDeleteServiceAreaCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var serviceArea = await _serviceAreaRepository.FindByIdAsync(request.id);
                if (serviceArea == null)
                {
                    return Result<string>.Failure("service area not found");
                }

                await _serviceAreaRepository.RemoveAsync(serviceArea, cancellationToken);
                await _serviceAreaRepository.SaveChangesAsync(cancellationToken);

                return Result<string>.Success("service area deleted successfully");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure("failed to delete service area", ex.Message);
            }


                
        }
    }

}
