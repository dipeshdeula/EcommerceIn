using Application.Common;
using Application.Dto.CartItemDTOs;
using Application.Dto.PromoCodeDTOs;
using Application.Extension.Cache;
using Application.Features.CartItemFeat.Commands;
using Application.Features.CartItemFeat.Queries;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.CartItemFeat.Module
{
    public class CartModule : CarterModule
    {
        public CartModule() : base("")
        {
            WithTags("CartItem");
            IncludeInOpenApi();
            RequireAuthorization();

        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("CartItem");

            app.MapPost("/create-cart", async (ISender mediator, CreateCartItemCommand command) =>
            {

                var result = await mediator.Send(command);

                /*  if (result == null)
                  {
                      return Results.BadRequest(new { message = "No response from the consumer." });
                  }*/

                if (result == null || !result.Succeeded)
                {
                    return Results.BadRequest(new { message = result?.Message ?? "An error occurred." });
                }

                return Results.Ok(result.Data);
            });

            app.MapGet("/getAllCartItem", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllCartItemQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getCartItemByUserId", async (ISender mediator, [FromQuery] int userId, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetCartByUserIdQuery(userId, PageNumber, PageSize));
                if (result == null || !result.Succeeded)
                {
                    return Results.BadRequest(new { message = result?.Message ?? "An error occurred.", data = result?.Data });
                }

                return Results.Ok(new { message = result.Message, data = result.Data });
            });

            app.MapPut("/updateCartItem", async (
                int Id, int UserId, int? ProductId, int? Quantity, ISender mediator) =>
            {
                var command = new UpdateCartItemCommand(Id, UserId, ProductId, Quantity);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            var promoGroup = app.MapGroup("/promo")
                .WithTags("Cart - Promo Codes");

            // Apply promo code to cart - COMMAND
            promoGroup.MapPost("/apply", async (
                [FromBody] ApplyPromoCodeToCartRequest request,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IUserRepository userRepository,
                [FromServices] ISender mediator) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId == 0) return Results.Unauthorized();
                var userInfo = await userRepository.FindByIdAsync(userId);

                
                var command = new ApplyPromoCodeToCartCommand(
                    Code: request.Code,
                    UserId: userInfo.Id,
                    CartItemIds: request.CartItemIds,
                    OrderTotal: request.OrderTotal,
                    ShippingCost: request.ShippingCost,
                    CustomerTier: request.CustomerTier,
                    DeliveryAddress: request.DeliveryAddress,
                    IsCheckout: request.IsCheckout,
                    UpdateCartPrices: true // Always true for apply
                );
                
                var result = await mediator.Send(command);
                
                return result.Succeeded 
                    ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .RequireAuthorization()
            .WithName("ApplyPromoCodeToCart")
            .WithSummary("Apply promo code to user's cart and update prices")
            .WithDescription("Applies a promo code to the user's cart items and updates the cart item prices accordingly")
            .Produces<Result<PromoCodeDiscountResultDTO>>();
            
            // Validate promo code without applying - COMMAND (with UpdateCartPrices = false)
            promoGroup.MapPost("/validate", async (
                [FromBody] ApplyPromoCodeToCartRequest request,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IUserRepository userRepository,
                [FromServices] ISender mediator) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId == 0) return Results.Unauthorized();

                var userInfo = await userRepository.FindByIdAsync(userId);
                
                var command = new ApplyPromoCodeToCartCommand(
                    Code: request.Code,
                    UserId: userInfo.Id,
                    CartItemIds: request.CartItemIds,
                    OrderTotal: request.OrderTotal,
                    ShippingCost: request.ShippingCost,
                    CustomerTier: request.CustomerTier,
                    DeliveryAddress: request.DeliveryAddress,
                    IsCheckout: request.IsCheckout,
                    UpdateCartPrices: false // Only validate, don't update
                );
                
                var result = await mediator.Send(command);
                
                return result.Succeeded 
                    ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .RequireAuthorization()
            .WithName("ValidatePromoCodeForCart")
            .WithSummary("Validate promo code against cart without applying")
            .Produces<Result<PromoCodeDiscountResultDTO>>();
            
            // Remove promo code from cart - COMMAND
            promoGroup.MapDelete("/remove/{promoCode}", async (
                string promoCode,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IUserRepository userRepository,
                [FromServices] ISender mediator) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId == 0) return Results.Unauthorized();

                var userInfo = await userRepository.FindByIdAsync(userId);
                if (userInfo == null) return Results.NotFound();

                var command = new RemovePromoCodeFromCartCommand(userInfo.Id, promoCode);
                var result = await mediator.Send(command);
                
                return result.Succeeded 
                    ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .RequireAuthorization()
            .WithName("RemovePromoCodeFromCart")
            .WithSummary("Remove promo code from cart and restore original prices")
            .Produces<Result<string>>();
            
            // Get cart summary with promo codes - QUERY
            promoGroup.MapGet("/summary", async (
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IUserRepository userRepository,
                [FromServices] ISender mediator) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId == 0) return Results.Unauthorized();
                var userInfo = await userRepository.FindByIdAsync(userId);
                
                var query = new GetCartSummaryWithPromoQuery(userInfo.Id);
                var result = await mediator.Send(query);
                
                return result.Succeeded 
                    ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .RequireAuthorization()
            .WithName("GetCartSummaryWithPromo")
            .WithSummary("Get cart summary including applied promo codes")
            .Produces<Result<CartSummaryWithPromoDTO>>();

            // Calculate checkout pricing - QUERY
            promoGroup.MapPost("/checkout-pricing", async (
                [FromBody] CheckoutPricingRequest request,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] IUserRepository userRepository,
                [FromServices] ISender mediator) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId == 0) return Results.Unauthorized();

                var userInfo = await userRepository.FindByIdAsync(userId);
                
                var query = new CalculateCheckoutPricingQuery(userInfo.Id, request.ShippingCost, request.TaxRate ?? 0.13m);
                var result = await mediator.Send(query);
                
                return result.Succeeded 
                    ? Results.Ok(result) 
                    : Results.BadRequest(result);
            })
            .RequireAuthorization()
            .WithName("CalculateCheckoutPricing")
            .WithSummary("Calculate final order pricing with all discounts for checkout")
            .Produces<Result<OrderPricingSummaryDTO>>();

            app.MapPost("/add-multiple-cart-items", async (
                int userId,
                List<AddToCartItemDTO> items,
                ISender mediator
                ) =>
            {
                var command = new CreateCartItemsCommand(userId, items);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization()
            .WithName("AddMultipleCartItems")
            .WithSummary("Add multiple product items to the cart in one request");


            app.MapDelete("/remove-cart-item", async (
                int cartItemId,
                ICurrentUserService currentUserService,
                ISender mediator
                ) =>
            {
                if (string.IsNullOrEmpty(currentUserService.UserId))
                    return Results.Unauthorized();

                var command = new HardDeleteCartItemCommand(cartItemId, Convert.ToInt32(currentUserService.UserId));
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization()
            .WithName("RemoveCartItem")
            .WithSummary("Remove a particular product item from the cart");

            // Performance analysis endpoint

            app.MapGet("/cart-performance/{userId:int}", async (
                int userId,
                ISender mediator,
                IHybridCacheService cacheService) =>
            {
                try
                {
                    var stats = new List<object>();

                    // Test multiple page loads
                    for (int page = 1; page <= 3; page++)
                    {
                        var start = DateTime.UtcNow;

                        var result = await mediator.Send(new GetCartByUserIdQuery(userId, page, 10));

                        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                        var isCacheHit = elapsed < 30; // Under 30ms is likely cache hit

                        if (result.Succeeded)
                        {
                            stats.Add(new
                            {
                                Page = page,
                                ElapsedMs = elapsed,
                                ItemCount = result.Data?.Count() ?? 0,
                                CacheHit = isCacheHit,
                                Message = result.Message
                            });
                        }
                    }

                    // Get cache summary
                    var summary = await cacheService.GetCachedCartSummaryAsync(userId);

                    return Results.Ok(new
                    {
                        UserId = userId,
                        PageStats = stats,
                        CacheSummary = summary,
                        AverageResponseTime = stats.Count > 0 ? stats.Average(s => (double)s.GetType().GetProperty("ElapsedMs").GetValue(s)) : 0,
                        CacheHitRate = stats.Count(s => (bool)s.GetType().GetProperty("CacheHit").GetValue(s)) * 100.0 / Math.Max(stats.Count, 1)
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Error = ex.Message });
                }
            })
            .WithName("GetCartPerformanceStats")
            .WithSummary("Analyze cart performance and cache efficiency for a user")
            .WithTags("CartItem", "Performance");
        }
    }
    
    public record ApplyPromoCodeToCartRequest(
        string Code,
        List<int>? CartItemIds = null,
        decimal? OrderTotal = null,
        decimal? ShippingCost = null,
        string? CustomerTier = null,
        string? DeliveryAddress = null,
        bool IsCheckout = false
    );
    
    public record CheckoutPricingRequest(
        decimal ShippingCost,
        decimal? TaxRate = 0.13m
    );
}
