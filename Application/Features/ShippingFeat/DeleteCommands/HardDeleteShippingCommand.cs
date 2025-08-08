using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.ShippingFeat.DeleteCommands
{
    public record HardDeleteShippingCommand(int Id) : IRequest<Result<bool>>;

    public class HardDeleteShippingCommandHandler : IRequestHandler<HardDeleteShippingCommand, Result<bool>>
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IDistributedCache _cache;

        public HardDeleteShippingCommandHandler(
            IShippingRepository shippingRepository,
            IDistributedCache cache)
        {
            _shippingRepository = shippingRepository;
            _cache = cache;
        }

        public async Task<Result<bool>> Handle(HardDeleteShippingCommand request, CancellationToken cancellationToken)
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
                    return Result<bool>.Failure("Cannot permanently delete the default shipping configuration.");
                }

                await _shippingRepository.RemoveAsync(configuration, cancellationToken);
                await _shippingRepository.SaveChangesAsync(cancellationToken);

                // Clear cache
                await _cache.RemoveAsync("shipping_config_active");

                return Result<bool>.Success(true, "Shipping configuration permanently deleted");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error permanently deleting shipping configuration: {ex.Message}");
            }
        }
    }
}