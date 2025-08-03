using Application.Dto.CartItemDTOs;
using Application.Extension.Cache;
using Application.Features.CartItemFeat.Commands;
using Application.Features.CartItemFeat.Queries;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
}
