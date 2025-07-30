using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.whishlistFeat.DeleteCommands
{
    public record RemoveWishlistCommand(int UserId, int ProductId) : IRequest<Result<string>>;

    public class RemoveWishlistCommandHandler : IRequestHandler<RemoveWishlistCommand, Result<string>>
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ILogger<RemoveWishlistCommandHandler> _logger;

        public RemoveWishlistCommandHandler(
            IWishlistRepository wishlistRepository,
            ILogger<RemoveWishlistCommandHandler> logger)
        {
            _wishlistRepository = wishlistRepository;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(RemoveWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Removing product {ProductId} from wishlist for user {UserId}",
                request.ProductId, request.UserId);

            var exists = await _wishlistRepository.ExistsAsync(request.UserId, request.ProductId, cancellationToken);
            if (!exists)
            {
                return Result<string>.Failure("Product is not in your wishlist");
            }

            await _wishlistRepository.RemoveByUserAndProductAsync(request.UserId, request.ProductId, cancellationToken);

            _logger.LogInformation("Successfully removed product from wishlist");
            return Result<string>.Success("", "Product removed from wishlist successfully");
        }
    }
}
