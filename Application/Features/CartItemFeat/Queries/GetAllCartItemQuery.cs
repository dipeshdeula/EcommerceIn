using Application.Common;
using Application.Dto;
using Application.Extension;
using Application.Interfaces.Repositories;
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
            var cartItem = await _cartItemRepository.GetAllAsync(
                orderBy: query => query.OrderByDescending(cartItem => cartItem.Id),
                skip: (request.PageNumber - 1) * request.PageSize,
                take: request.PageSize,
                includeProperties:"User,Product"
               
                );
            // Map categories to DTOs
            var cartItemDtos = cartItem.Select(cd => cd.ToDTO()).ToList();

            // Return the result wrapped in a Task
            return Result<IEnumerable<CartItemDTO>>.Success(cartItemDtos, "Cart Item fetched successfully");
        }
    }
}
