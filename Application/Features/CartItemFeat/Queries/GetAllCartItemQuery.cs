using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Extension;
using Application.Interfaces.Repositories;
using Application.Utilities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CartItemFeat.Queries
{
    public record GetAllCartItemQuery(int PageNumber, int PageSize) : IRequest<Result<IEnumerable<CartItemDTO>>>;

    public class GetAllCartItemQueryHandler : IRequestHandler<GetAllCartItemQuery, Result<IEnumerable<CartItemDTO>>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        public GetAllCartItemQueryHandler(ICartItemRepository cartItemRepository)
        {
            _cartItemRepository = cartItemRepository;

        }
        public async Task<Result<IEnumerable<CartItemDTO>>> Handle(GetAllCartItemQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cartItems = await _cartItemRepository.GetAllAsync(
                    predicate: c => !c.IsDeleted && c.ExpiresAt > DateTime.UtcNow,
                    orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    includeProperties: "User,User.Addresses,Product,Product.Images,Product.Category",
                    cancellationToken: cancellationToken
                );
                

                var cartItemDtos = cartItems.Select(c => c.ToDTO()).ToList();

                var activeCount = cartItems.Count(c => !c.IsExpired);
                var expiredCount = cartItems.Count(c => c.IsExpired);
                var totalUsers = cartItems.Select(c => c.UserId).Distinct().Count();

                return Result<IEnumerable<CartItemDTO>>.Success(
                    cartItemDtos,
                    $"Cart items fetched successfully. Total: {cartItems.Count()}, Active: {activeCount}, Expired: {expiredCount}, Users: {totalUsers}"
                );
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<CartItemDTO>>.Failure($"Failed to fetch cart items: {ex.Message}");
            }
        }
    }
}
