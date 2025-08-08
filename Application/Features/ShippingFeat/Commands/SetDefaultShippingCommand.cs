using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.ShippingFeat.Commands
{
    public record SetDefaultShippingCommand(
        int Id,
        int? ModifiedByUserId
    ) : IRequest<Result<bool>>;

    public class SetDefaultShippingCommandHandler : IRequestHandler<SetDefaultShippingCommand, Result<bool>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public SetDefaultShippingCommandHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<bool>> Handle(SetDefaultShippingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Remove default from all others
                var allConfigurations = await _shippingRepository.GetAllAsync(
                    predicate: c => c.IsActive && !c.IsDeleted,
                    includeDeleted: false
                );

                foreach (var config in allConfigurations)
                {
                    config.IsDefault = config.Id == request.Id;
                    config.LastModifiedByUserId = request.ModifiedByUserId;
                    config.UpdatedAt = DateTime.UtcNow;
                    await _shippingRepository.UpdateAsync(config, cancellationToken);
                }

                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true, "Configuration set as default successfully");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error setting default configuration: {ex.Message}");
            }
        }
    }
}