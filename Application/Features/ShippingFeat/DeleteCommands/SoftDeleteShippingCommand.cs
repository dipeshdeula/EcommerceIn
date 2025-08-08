using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.ShippingFeat.DeleteCommands
{
    public record SoftDeleteShippingCommand(int Id) : IRequest<Result<bool>>;

    public class SoftDeleteShippingCommandHandler : IRequestHandler<SoftDeleteShippingCommand, Result<bool>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public SoftDeleteShippingCommandHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<bool>> Handle(SoftDeleteShippingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var configuration = await _shippingRepository.GetByIdAsync(request.Id, cancellationToken);
                if (configuration == null)
                {
                    return Result<bool>.Failure("Shipping configuration not found");
                }

                // Check if it's the default configuration
                if (configuration.IsDefault)
                {
                    return Result<bool>.Failure("Cannot delete the default shipping configuration. Please set another configuration as default first.");
                }

                configuration.IsDeleted = true;
                configuration.IsActive = false;
                configuration.UpdatedAt = DateTime.UtcNow;

                await _shippingRepository.UpdateAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true, "Shipping configuration deleted successfully");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error deleting shipping configuration: {ex.Message}");
            }
        }
    }
}