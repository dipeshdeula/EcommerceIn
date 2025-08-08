using Application.Common;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.ShippingFeat.Commands
{
    public record CreateShippingCommand(
        string ConfigurationName,
        decimal LowOrderThreshold,
        decimal LowOrderShippingCost,
        decimal HighOrderShippingCost,
        decimal FreeShippingThreshold,
        int EstimatedDeliveryDays,
        double MaxDeliveryDistanceKm,
        bool EnableFreeShippingEvents,
        bool IsFreeShippingActive,
        DateTime? FreeShippingStartDate,
        DateTime? FreeShippingEndDate,
        string FreeShippingDescription,
        decimal WeekendSurcharge,
        decimal HolidaySurcharge,
        decimal RushDeliverySurcharge,
        string CustomerMessage,
        string AdminNotes,
        bool SetAsDefault,
        int? CreatedByUserId
    ) : IRequest<Result<ShippingDTO>>;

    public class CreateShippingCommandHandler : IRequestHandler<CreateShippingCommand, Result<ShippingDTO>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public CreateShippingCommandHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<ShippingDTO>> Handle(CreateShippingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // If setting as default, deactivate current default
                if (request.SetAsDefault)
                {
                    var currentDefaults = await _shippingRepository.GetAllAsync(
                        predicate: c => c.IsDefault && c.IsActive && !c.IsDeleted,
                        includeDeleted: false
                    );

                    foreach (var defaultConfig in currentDefaults)
                    {
                        defaultConfig.IsDefault = false;
                        await _shippingRepository.UpdateAsync(defaultConfig, cancellationToken);
                    }
                }

                var configuration = new Shipping
                {
                    Name = request.ConfigurationName,
                    IsActive = true,
                    IsDefault = request.SetAsDefault,
                    LowOrderThreshold = request.LowOrderThreshold,
                    LowOrderShippingCost = request.LowOrderShippingCost,
                    HighOrderShippingCost = request.HighOrderShippingCost,
                    FreeShippingThreshold = request.FreeShippingThreshold,
                    EstimatedDeliveryDays = request.EstimatedDeliveryDays,
                    MaxDeliveryDistanceKm = request.MaxDeliveryDistanceKm,
                    EnableFreeShippingEvents = request.EnableFreeShippingEvents,
                    IsFreeShippingActive = request.IsFreeShippingActive,
                    FreeShippingStartDate = request.FreeShippingStartDate,
                    FreeShippingEndDate = request.FreeShippingEndDate,
                    FreeShippingDescription = request.FreeShippingDescription,
                    WeekendSurcharge = request.WeekendSurcharge,
                    HolidaySurcharge = request.HolidaySurcharge,
                    RushDeliverySurcharge = request.RushDeliverySurcharge,
                    CustomerMessage = request.CustomerMessage,
                    AdminNotes = request.AdminNotes,
                    CreatedByUserId = request.CreatedByUserId ?? 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RequireLocationValidation = true
                };

                await _shippingRepository.AddAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                var result = configuration.ToShippingDTO();
                return Result<ShippingDTO>.Success(result, "Shipping configuration created successfully");
            }
            catch (Exception ex)
            {
                return Result<ShippingDTO>.Failure($"Error creating shipping configuration: {ex.Message}");
            }
        }
    }
}