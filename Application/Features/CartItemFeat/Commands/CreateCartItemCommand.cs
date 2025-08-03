using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Extension.Cache;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.CartItemFeat.Commands
{
    public record CreateCartItemCommand(
        int UserId,
        int ProductId,
        int Quantity
    ) : IRequest<Result<CartItemDTO>>;

    public class CreateCartItemCommandHandler : IRequestHandler<CreateCartItemCommand, Result<CartItemDTO>>
    {
        private readonly ICartService _cartService;
        private readonly IHybridCacheService _cacheService;
        private readonly ILogger<CreateCartItemCommandHandler> _logger;
        private readonly IEventUsageService _eventUsageService;
        private readonly IProductPricingService _productPricingService;
        private readonly IUnitOfWork _unitOfWork;
        public CreateCartItemCommandHandler(
            ICartService cartService,
            IHybridCacheService cacheService,
            IEventUsageService eventUsageService,
            IProductPricingService productPricingservice,
            ILogger<CreateCartItemCommandHandler> logger,
            IUnitOfWork unitOfWork
            )
        {
            _cartService = cartService;
            _cacheService = cacheService;
            _productPricingService = productPricingservice;
            _eventUsageService = eventUsageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<CartItemDTO>> Handle(CreateCartItemCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing add to cart: UserId={UserId}, ProductId={ProductId}, Quantity={Quantity}",
                    request.UserId, request.ProductId, request.Quantity);

                var pricing = await _productPricingService.GetEffectivePriceAsync(request.ProductId, request.UserId, cancellationToken);

                // validate event usage if event is applied 
                if (pricing.HasActiveEvent && pricing.ActiveEventId.HasValue)
                {
                    var validationResult = await _eventUsageService.CanUserAddQuantityToCartForProductAsync(
                        pricing.ActiveEventId.Value,
                        request.UserId,
                         request.ProductId,
                        request.Quantity
                       
                    );

                    if (!validationResult.Succeeded)
                    {
                        _logger.LogWarning("User {UserId} cannot add {Quantity}x product {ProductId}: {Reason}",
                            request.UserId, request.Quantity, request.ProductId, validationResult.Message);

                        return Result<CartItemDTO>.Failure(validationResult.Message);
                    }
                    
                    _logger.LogInformation("User {UserId} can add {Quantity}x product {ProductId} with event {EventId}",
                        request.UserId, request.Quantity, request.ProductId, pricing.ActiveEventId.Value);
                }

                

                // DIRECT SERVICE CALL (No RabbitMQ blocking)
                    var addToCartRequest = new AddToCartItemDTO
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                };

                var result = await _cartService.AddItemToCartAsync(request.UserId, addToCartRequest);

                if (result.Succeeded)
                {
                    // Invalidate user's cart cache 
                    await _cacheService.InvalidateUserCartCacheAsync(request.UserId, cancellationToken);

                    _logger.LogInformation("CART CACHE INVALIDATED: User {UserId} after adding ProductId {ProductId} ",
                        request.UserId, request.ProductId);
                        
                    // BACKGROUND EVENTS (Fire-and-forget, non-blocking)
                    _ = Task.Run(async () => await PublishBackgroundEvents(request, result.Data), cancellationToken);

                    _logger.LogInformation(" Cart item added successfully: CartItemId={CartItemId}",
                        result.Data.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item to cart: UserId={UserId}, ProductId={ProductId}",
                    request.UserId, request.ProductId);
                return Result<CartItemDTO>.Failure($"Failed to add item to cart: {ex.Message}");
            }
        }

        /// <summary>
        ///  BACKGROUND EVENTS (Non-blocking analytics, notifications)
        /// </summary>
        private async Task PublishBackgroundEvents(CreateCartItemCommand request, CartItemDTO cartItem)
        {
            try
            {
                // TODO: Implement proper background events
                // For now, just log analytics
                _logger.LogInformation("Cart analytics: UserId={UserId}, ProductId={ProductId}, Price={Price}",
                    request.UserId, request.ProductId, cartItem.ReservedPrice);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Don't fail main operation if background events fail
                _logger.LogWarning(ex, " Failed to publish background events for cart item: {CartItemId}", cartItem.Id);
            }
        }
    }
}