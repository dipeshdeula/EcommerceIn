using Application.Common;
using Application.Dto;
using Application.Interfaces.Repositories;
using MediatR;
using Application.Extension;


namespace Application.Features.CartItemFeat.Queries
{
    public record GetCartByUserIdQuery(
        int UserId,
        int PageNumber,
        int PageSize
        ) : IRequest<Result<IEnumerable<CartItemDTO>>>;

    public class GetCartByUserIdQueryHandler : IRequestHandler<GetCartByUserIdQuery, Result<IEnumerable<CartItemDTO>>>
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IUserRepository _userRepository;

        public GetCartByUserIdQueryHandler(ICartItemRepository cartItemRepository,IUserRepository userRepository)
        {
            _cartItemRepository = cartItemRepository;
            _userRepository = userRepository;
            
        }
        public async Task<Result<IEnumerable<CartItemDTO>>> Handle(GetCartByUserIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync( request.UserId );
            if (user == null)
            {
                return Result<IEnumerable<CartItemDTO>>.Failure("User not found");
            }

            var cart = await _cartItemRepository.GetByUserIdAsync( request.UserId );
            if (cart == null)
            {
                return Result<IEnumerable<CartItemDTO>>.Failure("User not placed the item to the cart");
            }

            return Result<IEnumerable<CartItemDTO>>.Success(cart.ToDTO(), "Fetch Cart item by User Id");
        }
    }

}
