using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.whishlistFeat.Commands
{
    public record MoveToCartCommand(int UserId, int ProductId, int Quantity = 1) : IRequest<Result<CartItemDTO>>;

    public class MoveToCartCommandHandler : IRequestHandler<MoveToCartCommand, Result<CartItemDTO>>
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ICartService _cartService;
        private readonly ILogger<MoveToCartCommandHandler> _logger;

        public MoveToCartCommandHandler(
            IWishlistRepository wishlistRepository,
            ICartService cartService,
            ILogger<MoveToCartCommandHandler> logger)
        {
            _wishlistRepository = wishlistRepository;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<Result<CartItemDTO>> Handle(MoveToCartCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Moving product {ProductId} from wishlist to cart for user {UserId}",
                request.ProductId, request.UserId);

            // Check if product is in wishlist
            var exists = await _wishlistRepository.ExistsAsync(request.UserId, request.ProductId, cancellationToken);
            if (!exists)
            {
                return Result<CartItemDTO>.Failure("Product is not in your wishlist");
            }

            // Add to cart
            var addToCartRequest = new AddToCartItemDTO
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            var cartResult = await _cartService.AddItemToCartAsync(request.UserId, addToCartRequest);
            if (!cartResult.Succeeded)
            {
                return Result<CartItemDTO>.Failure($"Failed to add to cart: {cartResult.Message}");
            }

            // Remove from wishlist
            await _wishlistRepository.RemoveByUserAndProductAsync(request.UserId, request.ProductId, cancellationToken);

            _logger.LogInformation("Successfully moved product from wishlist to cart");
            return Result<CartItemDTO>.Success(cartResult.Data, "Product moved to cart successfully");
        }
    }
}
