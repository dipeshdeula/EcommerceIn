using Application.Common;
using Application.Dto.WhishListDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.whishlistFeat.Commands
{
    public record CreateWishlistCommand (
        int UserId, int ProductId) : IRequest<Result<WishlistDTO>>;

    public class CreateWishlistCommandHandler : IRequestHandler<CreateWishlistCommand, Result<WishlistDTO>>
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CreateWishlistCommandHandler> _logger;

        public CreateWishlistCommandHandler(
            IWishlistRepository wishlistRepository,
            IUserRepository userRepository,
            IProductRepository productRepository,
            ILogger<CreateWishlistCommandHandler> logger)
        {
            _wishlistRepository = wishlistRepository;
            _userRepository = userRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<Result<WishlistDTO>> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding product {ProductId} to wishlist for user {UserId}",
                request.ProductId, request.UserId);

            // Validate user exists
            var user = await _userRepository.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<WishlistDTO>.Failure("User not found");
            }

            // Validate product exists
            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<WishlistDTO>.Failure("Product not found");
            }

            if (product.IsDeleted)
            {
                return Result<WishlistDTO>.Failure("Product is no longer available");
            }

            // Check if already in wishlist
            var exists = await _wishlistRepository.ExistsAsync(request.UserId, request.ProductId, cancellationToken);
            if (exists)
            {
                return Result<WishlistDTO>.Failure("Product is already in your wishlist");
            }

            // Create wishlist item
            var wishlist = new Wishlist
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _wishlistRepository.AddAsync(wishlist, cancellationToken);
            await _wishlistRepository.SaveChangesAsync(cancellationToken);

            // Load navigation properties for DTO
            var savedWishlist = await _wishlistRepository.GetByUserAndProductAsync(
                request.UserId, request.ProductId, cancellationToken);

            _logger.LogInformation("Successfully added product to wishlist: {WishlistId}", savedWishlist?.Id);

            return Result<WishlistDTO>.Success(savedWishlist!.ToDTO(), "Product added to wishlist successfully");
        }
    }

}
