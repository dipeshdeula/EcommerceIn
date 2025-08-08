using Application.Common;
using Application.Dto.ShippingDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.ShippingFeat.Commands
{
    public record UpdateShippingCommand(
        int Id,
        string Name,
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
        int? ModifiedByUserId
    ) : IRequest<Result<ShippingDTO>>;

    public class UpdateShippingCommandHandler : IRequestHandler<UpdateShippingCommand, Result<ShippingDTO>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public UpdateShippingCommandHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<ShippingDTO>> Handle(UpdateShippingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(request.Id, cancellationToken);
                if (configuration == null || configuration.IsDeleted)
                {
                    return Result<ShippingDTO>.Failure("Shipping configuration not found");
                }

                // If setting as default, deactivate current default
                if (request.SetAsDefault && !configuration.IsDefault)
                {
                    var currentDefaults = await _shippingRepository.GetAllAsync(
                        predicate: c => c.IsDefault && c.IsActive && !c.IsDeleted && c.Id != request.Id,
                        includeDeleted: false
                    );

                    foreach (var defaultConfig in currentDefaults)
                    {
                        defaultConfig.IsDefault = false;
                        await _shippingRepository.UpdateAsync(defaultConfig, cancellationToken);
                    }
                }

                // Update configuration
                configuration.Name = request.Name;
                configuration.IsDefault = request.SetAsDefault;
                configuration.LowOrderThreshold = request.LowOrderThreshold;
                configuration.LowOrderShippingCost = request.LowOrderShippingCost;
                configuration.HighOrderShippingCost = request.HighOrderShippingCost;
                configuration.FreeShippingThreshold = request.FreeShippingThreshold;
                configuration.EstimatedDeliveryDays = request.EstimatedDeliveryDays;
                configuration.MaxDeliveryDistanceKm = request.MaxDeliveryDistanceKm;
                configuration.EnableFreeShippingEvents = request.EnableFreeShippingEvents;
                configuration.IsFreeShippingActive = request.IsFreeShippingActive;
                configuration.FreeShippingStartDate = request.FreeShippingStartDate;
                configuration.FreeShippingEndDate = request.FreeShippingEndDate;
                configuration.FreeShippingDescription = request.FreeShippingDescription;
                configuration.WeekendSurcharge = request.WeekendSurcharge;
                configuration.HolidaySurcharge = request.HolidaySurcharge;
                configuration.RushDeliverySurcharge = request.RushDeliverySurcharge;
                configuration.CustomerMessage = request.CustomerMessage;
                configuration.AdminNotes = request.AdminNotes;
                configuration.LastModifiedByUserId = request.ModifiedByUserId;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                var result = configuration.ToShippingDTO();
                return Result<ShippingDTO>.Success(result, "Shipping configuration updated successfully");
            }
            catch (Exception ex)
            {
                return Result<ShippingDTO>.Failure($"Error updating shipping configuration: {ex.Message}");
            }
        }
    }
}