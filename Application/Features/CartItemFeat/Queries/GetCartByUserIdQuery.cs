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
        private readonly ILogger<GetCartByUserIdQueryHandler> _logger; 

        public GetCartByUserIdQueryHandler(
            ICartItemRepository cartItemRepository,
            IUserRepository userRepository,
            IHybridCacheService cacheService,
            ILogger<GetCartByUserIdQueryHandler> logger 
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

                _logger.LogInformation(" CART LOOKUP: User {UserId}, Page {Page}/{Size}", 
                    request.UserId, request.PageNumber, request.PageSize);

                //  STEP 1: CHECK IF CACHE SHOULD BE SKIPPED (for recent deletions/modifications)
                var shouldSkipCache = await _cacheService.ShouldSkipCacheAsync(request.UserId, cancellationToken);
                
                if (!shouldSkipCache)
                {
                    // Try cache first using the improved extension method
                    var cachedCart = await _cacheService.GetCachedCartPageAsync(
                        request.UserId,
                        request.PageNumber,
                        request.PageSize,
                        cancellationToken
                    );

                    if (cachedCart.Any())
                    {
                        var cacheTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogInformation(" CART CACHE HIT: User {UserId} in {ElapsedMs}ms, {Count} items", 
                            request.UserId, cacheTime, cachedCart.Count);

                        return Result<IEnumerable<CartItemDTO>>.Success(cachedCart,
                            $"Cart retrieved from cache. {cachedCart.Count} items found.");
                    }
                }
                else
                {
                    _logger.LogInformation(" CACHE SKIPPED: Recent modifications detected for User {UserId}", request.UserId);
                }

                //  STEP 2: CACHE MISS - Verify user exists
                _logger.LogInformation(" CART CACHE MISS: Loading from database for user {UserId}", request.UserId);
                                
                var user = await _userRepository.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return Result<IEnumerable<CartItemDTO>>.Failure("User not found");
                }

                //  STEP 3: Get cart items from database with proper error handling
                var cartItems = await _cartItemRepository.GetAllAsync(
                    predicate: c => c.UserId == request.UserId && !c.IsDeleted,
                    includeProperties: "Product,Product.Images,User,User.Addresses,Shipping", //  Added missing includes
                    orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                    skip: (request.PageNumber - 1) * request.PageSize,
                    take: request.PageSize,
                    cancellationToken: cancellationToken
                );

                var dbTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (!cartItems.Any())
                {
                    _logger.LogInformation("📭 Empty cart: User {UserId} - caching empty result", request.UserId);

                    // Cache empty result to prevent repeated DB calls
                    await _cacheService.SetCartPageCacheAsync(
                        request.UserId,
                        request.PageNumber,
                        request.PageSize,
                        new List<CartItemDTO>(),
                        cancellationToken
                    );

                    return Result<IEnumerable<CartItemDTO>>.Success(
                        new List<CartItemDTO>(),
                        "Cart is empty"
                    );
                }

                //  STEP 4: Convert to DTOs with error handling
                var cartItemDtos = new List<CartItemDTO>();
                foreach (var cartItem in cartItems)
                {
                    try
                    {
                        var dto = cartItem.ToDTO();
                        cartItemDtos.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, " Failed to convert CartItem {CartItemId} to DTO for User {UserId}", 
                            cartItem.Id, request.UserId);
                        // Continue with other items
                    }
                }

                if (!cartItemDtos.Any())
                {
                    _logger.LogWarning(" No valid cart items could be processed for User {UserId}", request.UserId);
                    return Result<IEnumerable<CartItemDTO>>.Success(
                        new List<CartItemDTO>(),
                        "Cart items could not be processed"
                    );
                }

                //  STEP 5: Cache the result ONLY if not skipping cache
                if (!shouldSkipCache)
                {
                    try
                    {
                        await _cacheService.SetCartPageCacheAsync(
                            request.UserId,
                            request.PageNumber,
                            request.PageSize,
                            cartItemDtos,
                            cancellationToken
                        );
                        _logger.LogDebug(" Cart cached for User {UserId}, Page {Page}/{Size}", 
                            request.UserId, request.PageNumber, request.PageSize);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, " Failed to cache cart for User {UserId}: {Error}", 
                            request.UserId, ex.Message);
                        // Continue - caching failure shouldn't break the operation
                    }
                }

                //  STEP 6: Create and cache summary
                var summary = CartSummaryDTO.CreateFromCartItems(
                    request.UserId,
                    cartItemDtos,
                    dbTime,
                    isCacheData: false
                );

                // Cache summary separately (even if we're skipping main cache)
                try
                {
                    await _cacheService.SetCartSummaryCacheAsync(request.UserId, summary, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, " Failed to cache cart summary for User {UserId}", request.UserId);
                }

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
                _logger.LogError(ex, " Error fetching cart for user {UserId}: {Error}", request.UserId, ex.Message);

                return Result<IEnumerable<CartItemDTO>>.Failure($"Failed to fetch cart items: {ex.Message}");
            }
        }
    }
}