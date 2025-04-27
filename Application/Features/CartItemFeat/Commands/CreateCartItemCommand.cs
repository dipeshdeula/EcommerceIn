using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CartItemFeat.Commands
{
    public record CreateCartItemCommand(
        int UserId,
        int ProductId,
        int Quantity

        ) : IRequest<Result<CartItemDTO>>;

    public class CreateCartItemCommandHandler : IRequestHandler<CreateCartItemCommand, Result<CartItemDTO>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;

        public CreateCartItemCommandHandler(
            ICartItemRepository cartItemRepository,
            IProductRepository productRepository,
            IUserRepository userRepository)
        {
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
        }

        public async Task<Result<CartItemDTO>> Handle(CreateCartItemCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<CartItemDTO>.Failure("User not found");
            }

            // Start a transaction or use a mutex for thread safety

            var product = await _productRepository.FindByIdAsync(request.ProductId);
            if (product == null)
            {
                return Result<CartItemDTO>.Failure("Product not found");
            }

            if (product.StockQuantity > request.Quantity && product.StockQuantity <= 0)
            {
                return Result<CartItemDTO>.Failure("Not enough stock available");
            }

            var cartItem = new CartItem
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                User = user,
                Product = product
            };

            var createdCartItem = await _cartItemRepository.AddAsync(cartItem);
            if (createdCartItem == null)
            {
                return Result<CartItemDTO>.Failure("Failed to create cart item");
            }

            return Result<CartItemDTO>.Success(createdCartItem.ToDTO(), "Items added to the Cart");
        }
    }


}
