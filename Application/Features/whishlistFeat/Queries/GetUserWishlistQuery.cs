using Application.Common;
using Application.Dto.WhishListDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.whishlistFeat.Queries
{
    public record GetUserWishlistQuery(int UserId) : IRequest<Result<WishlistSummaryDTO>>;

    public class GetUserWishlistQueryHandler : IRequestHandler<GetUserWishlistQuery, Result<WishlistSummaryDTO>>
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ILogger<GetUserWishlistQueryHandler> _logger;

        public GetUserWishlistQueryHandler(
            IWishlistRepository wishlistRepository,
            ILogger<GetUserWishlistQueryHandler> logger)
        {
            _wishlistRepository = wishlistRepository;
            _logger = logger;
        }

        public async Task<Result<WishlistSummaryDTO>> Handle(GetUserWishlistQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching wishlist for user {UserId}", request.UserId);

            var wishlistItems = await _wishlistRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            var wishlistDTOs = wishlistItems.Select(w => w.ToDTO()).ToList();

            var summary = new WishlistSummaryDTO
            {
                UserId = request.UserId,
                TotalItems = wishlistDTOs.Count,
                TotalValue = wishlistDTOs.Sum(w => w.ProductDto?.CostPrice ?? 0),
                Items = wishlistDTOs,
                LastUpdated = wishlistDTOs.Any() ? wishlistDTOs.Max(w => w.UpdatedAt) : DateTime.UtcNow
            };

            return Result<WishlistSummaryDTO>.Success(summary, "Wishlist fetched successfully");
        }
    }
}
