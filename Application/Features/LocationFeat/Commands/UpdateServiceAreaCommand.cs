using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Extension;
using Application.Features.LocationFeat.Validators;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.LocationFeat.Commands
{
    public record UpdateServiceAreaCommand(int id, UpdateServiceAreaDTO updateServiceAreaDTO) : IRequest<Result<ServiceAreaDTO>>;

    public class UpdateServiceAreaCommandHandler : IRequestHandler<UpdateServiceAreaCommand, Result<ServiceAreaDTO>>
    {
        private readonly IServiceAreaRepository _serviceAreaRepository;
        private readonly ILogger<UpdateServiceAreaCommandHandler> _logger;
        ICurrentUserService _currentUserService;

        public UpdateServiceAreaCommandHandler(IServiceAreaRepository serviceAreaRepository
            , ILogger<UpdateServiceAreaCommandHandler> logger,
            ICurrentUserService currentUserService)
        {
            _serviceAreaRepository = serviceAreaRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<ServiceAreaDTO>> Handle(UpdateServiceAreaCommand request, CancellationToken cancellationToken)
        {
            try
            {            
                var serviceArea = await _serviceAreaRepository.FindByIdAsync(request.id);
                if (serviceArea == null)
                {
                    return Result<ServiceAreaDTO>.Failure("service area id not found");

                }

                serviceArea.CityName = request.updateServiceAreaDTO.CityName ?? serviceArea.CityName;
                serviceArea.Province = request.updateServiceAreaDTO.Province ?? serviceArea.Province;
                serviceArea.Country = request.updateServiceAreaDTO.Country ?? serviceArea.Country;
                serviceArea.CenterLatitude = request.updateServiceAreaDTO.CenterLatitude ?? serviceArea.CenterLatitude;
                serviceArea.CenterLongitude = request.updateServiceAreaDTO.CenterLongitude ?? serviceArea.CenterLongitude;
                serviceArea.RadiusKm = request.updateServiceAreaDTO.RadiusKm ?? serviceArea.RadiusKm;
                serviceArea.IsActive = request.updateServiceAreaDTO?.IsActive ?? serviceArea.IsActive;
                serviceArea.IsComingSoon = request.updateServiceAreaDTO?.IsComingSoon ?? serviceArea.IsComingSoon;
                serviceArea.MaxDeliveryDistanceKm = request.updateServiceAreaDTO?.MaxDeliveryDistancekm ?? serviceArea.MaxDeliveryDistanceKm;
                serviceArea.MinOrderAmount = request.updateServiceAreaDTO?.MinOrderAmount ?? serviceArea.MinOrderAmount;
                serviceArea.DeliveryStartTime = request.updateServiceAreaDTO?.DeliveryStartTime ?? serviceArea.DeliveryStartTime;
                serviceArea.EstimatedDeliveryDays = request.updateServiceAreaDTO?.EstimatedDeliveryDays ?? serviceArea.EstimatedDeliveryDays;
                serviceArea.DisplayName = request.updateServiceAreaDTO?.DisplayName ?? serviceArea.DisplayName;
                serviceArea.Description = request.updateServiceAreaDTO?.Description ?? serviceArea.Description;
                serviceArea.NotAvailableMessage = request.updateServiceAreaDTO?.NotAvailableMessage ?? serviceArea.NotAvailableMessage;
                serviceArea.UpdatedAt = DateTime.UtcNow;
                serviceArea.CreatedBy = _currentUserService.UserName ?? serviceArea.CreatedBy;
                serviceArea.UpdatedBy = _currentUserService.UserName ?? serviceArea.UpdatedBy;

                await _serviceAreaRepository.UpdateAsync(serviceArea, cancellationToken);
                await _serviceAreaRepository.SaveChangesAsync(cancellationToken);

                return Result<ServiceAreaDTO>.Success(serviceArea.ToDTO(), "service area is updated successfully");

            }
            catch (Exception ex)
            {
                return Result<ServiceAreaDTO>.Failure("service area failed to update", ex.Message);
            }
        }
    }
    
    
}
