using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Application.Extension;
using Application.Dto.CartItemDTOs;


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
            try
            {
                
                var user = await _userRepository.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return Result<IEnumerable<CartItemDTO>>.Failure("User not found");
                }

                var cartItems = await _cartItemRepository.GetAllAsync(
                    predicate: c => c.UserId == request.UserId && !c.IsDeleted,
                    includeProperties: "Product,Product.Images,User",
                    orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    cancellationToken: cancellationToken
                );

                if (!cartItems.Any())
                {
                    return Result<IEnumerable<CartItemDTO>>.Success(
                        new List<CartItemDTO>(),
                        "Cart is empty"
                    );
                }

                var cartItemDtos = cartItems.Select(c => c.ToDTO()).ToList();

                var activeCount = cartItems.Count(c => !c.IsExpired);
                var expiredCount = cartItems.Count(c => c.IsExpired);
                var totalValue = cartItems.Where(c => !c.IsExpired).Sum(c => (c.ReservedPrice) * c.Quantity);

                return Result<IEnumerable<CartItemDTO>>.Success(
                    cartItemDtos,
                    $"Cart items fetched successfully. Active: {activeCount}, Expired: {expiredCount}, Total Value: Rs. {totalValue:F2}"
                );
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<CartItemDTO>>.Failure($"Failed to fetch cart items: {ex.Message}");
            }


        }
    }

}
