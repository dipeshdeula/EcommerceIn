using Application.Common;
using Application.Dto.LocationDTOs;
using Application.Extension;
using Application.Features.LocationFeat.Validators;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.LocationFeat.Commands
{
    public record CreateServiceAreaCommand(AddServiceAreaDTO serviceArea) : IRequest<Result<ServiceAreaDTO>>;

    public class CreateServiceAreaCommandHandler : IRequestHandler<CreateServiceAreaCommand, Result<ServiceAreaDTO>>
    {
        private readonly IServiceAreaRepository _serviceAreaRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateServiceAreaCommandHandler> _logger;

        public CreateServiceAreaCommandHandler(
            IServiceAreaRepository serviceAreaRepository,
            ICurrentUserService currentUserService,
            ILogger<CreateServiceAreaCommandHandler> logger)
        {
            _serviceAreaRepository = serviceAreaRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }


        public async Task<Result<ServiceAreaDTO>> Handle(CreateServiceAreaCommand request, CancellationToken cancellationToken)
        {
            try
            {
               
                var createServiceArea = new ServiceArea
                {
                    CityName = request.serviceArea.CityName,
                    Province = request.serviceArea.Province,
                    Country = request.serviceArea.Country,
                    CenterLatitude = request.serviceArea.CenterLatitude,
                    CenterLongitude = request.serviceArea.CenterLongitude,
                    RadiusKm = request.serviceArea.RadiusKm,
                    IsActive = request.serviceArea.IsActive,
                    IsComingSoon = request.serviceArea.IsComingSoon,
                    MaxDeliveryDistanceKm = request.serviceArea.MaxDeliveryDistancekm,
                    MinOrderAmount = request.serviceArea.MinOrderAmount,
                    DeliveryStartTime = request.serviceArea.DeliveryStartTime,
                    DeliveryEndTime = request.serviceArea.DeliveryEndTime,
                    EstimatedDeliveryDays = request.serviceArea.EstimatedDeliveryDays,
                    DisplayName = request.serviceArea.DisplayName,
                    Description = request.serviceArea.Description,
                    NotAvailableMessage = request.serviceArea.NotAvailableMessage,                    
                    CreatedBy = _currentUserService.UserName ,
                    

                };

                await _serviceAreaRepository.AddAsync(createServiceArea, cancellationToken);
                await _serviceAreaRepository.SaveChangesAsync(cancellationToken);

                return Result<ServiceAreaDTO>.Success(createServiceArea.ToDTO(),"Service area created succesfully");

            }
            catch (Exception ex)
            {
                return Result<ServiceAreaDTO>.Failure("Failed to create a new service area", ex.Message);
            }
        }
    }

}
