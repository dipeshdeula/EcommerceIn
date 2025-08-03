using Application.Common;
using Application.Interfaces.Repositories;
using MediatR;
using Application.Extension;
using Application.Dto.CartItemDTOs;
using Application.Utilities;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Application.Extension.Cache;


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
        private readonly IHybridCacheService _cacheService;
        private ILogger<GetCartByUserIdQuery> _logger;


        public GetCartByUserIdQueryHandler(
            ICartItemRepository cartItemRepository,
            IUserRepository userRepository,
            IHybridCacheService cacheService,
            ILogger<GetCartByUserIdQuery> logger
            )
        {
            _cartItemRepository = cartItemRepository;
            _userRepository = userRepository;
            _cacheService = cacheService;
            _logger = logger;

        }
        public async Task<Result<IEnumerable<CartItemDTO>>> Handle(GetCartByUserIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // step 1 : Try Cache first
                _logger.LogInformation("CART CACHE LOOKUP: User {UserId}, Page {Page}/{Size}", 
                    request.UserId, request.PageNumber, request.PageSize);

                var cachedCart = await _cacheService.GetCachedCartPageAsync(
                    request.UserId,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken
                );

                if(cachedCart.Any())
                {
                    var cacheTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("CART CACHE HIT: User {UserId} in {ElapsedMs}ms, {Count} items", 
                        request.UserId, cacheTime, cachedCart.Count);

                    return Result<IEnumerable<CartItemDTO>>.Success(cachedCart,
                        $"Cart retrieved from cache. {cachedCart.Count} items found.");
                }

                // step 2 : Cache miss - verfiy user exists
                _logger.LogInformation("CART CACHE MISS : Loading from database for user {UserId}",request.UserId);
                                
                var user = await _userRepository.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return Result<IEnumerable<CartItemDTO>>.Failure("User not found");
                }

                // Get cart items from database

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
                    // Cache empty result to prevent repeated DB calls
                    await _cacheService.SetCartPageCacheAsync(
                        request.UserId,
                        request.PageNumber,
                        request.PageSize,
                        new List<CartItemDTO>(),
                        cancellationToken
                    );

                    _logger.LogInformation("Empty cart: User {UserId} - cached empty result", request.UserId);

                    return Result<IEnumerable<CartItemDTO>>.Success(
                        new List<CartItemDTO>(),
                        "Cart is empty"
                    );
                }

                var cartItemDtos = cartItems.Select(c => c.ToDTO()).ToList();

                // Cache the result 
                await _cacheService.SetCartPageCacheAsync(
                    request.UserId,
                    request.PageNumber,
                    request.PageSize,
                    cartItemDtos,
                    cancellationToken
                );

                var dbTime = (DateTime.UtcNow-startTime).TotalMilliseconds;

                var summary = CartSummaryDTO.CreateFromCartItems(
                    request.UserId,
                    cartItemDtos,
                    dbTime,
                    isCacheData: false
                );

                
                await _cacheService.SetCartSummaryCacheAsync(request.UserId, summary, cancellationToken);

                 _logger.LogInformation(
                "CART DB LOAD: User {UserId} in {ElapsedMs}ms | Active: {Active}, Expired: {Expired}, Value: Rs.{Value:F2}",
                request.UserId, dbTime, summary.ActiveItemsCount, summary.ExpiredItemsCount, summary.EstimatedTotal);
                return Result<IEnumerable<CartItemDTO>>.Success(
                    cartItemDtos,
                    $"Cart items fetched successfully. Active: {summary.ActiveItemsCount}, Expired: {summary.ExpiredItemsCount}, Total Value: Rs. {summary.EstimatedTotal:F2}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cart for user {UserId}", request.UserId);

                return Result<IEnumerable<CartItemDTO>>.Failure($"Failed to fetch cart items: {ex.Message}");
            }


        }
    }

}
