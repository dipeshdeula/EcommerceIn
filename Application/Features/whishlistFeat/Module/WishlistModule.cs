using Application.Dto.WhishListDTOs;
using Application.Features.whishlistFeat.Commands;
using Application.Features.whishlistFeat.DeleteCommands;
using Application.Features.whishlistFeat.Queries;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.whishlistFeat.Module
{
    public class WishlistModule : CarterModule
    {
        public WishlistModule() : base("")
        {
            WithTags("wishlist");
            IncludeInOpenApi();
           // RequireAuthorization();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("wishlist");
            // Get user's wishlist
            app.MapGet("/{userId:int}", async (
                int userId,
                ISender mediator,
                ICurrentUserService currentUserService) =>
            {
                // Ensure user can only access their own wishlist
                if (currentUserService.UserId != userId.ToString() && currentUserService.Role?.ToLower() != "admin")
                {
                    return Results.Forbid();
                }

                var query = new GetUserWishlistQuery(userId);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
             .RequireAuthorization()
            .WithName("GetUserWishlist")
            .WithSummary("Get user's wishlist with product details");

            // Add product to wishlist
            app.MapPost("/add", async (
                [FromBody] AddWishlistDTO request,
                ISender mediator,
                ICurrentUserService currentUserService) =>
            {
                var userId = Convert.ToInt32(currentUserService.UserId);
                var command = new CreateWishlistCommand(userId, request.ProductId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message });
            })
            .WithName("AddToWishlist")
            .WithSummary("Add a product to user's wishlist");

            // Remove product from wishlist
            app.MapDelete("/{productId:int}", async (
                int productId,
                ISender mediator,
                ICurrentUserService currentUserService) =>
            {
                var userId = Convert.ToInt32(currentUserService.UserId);
                var command = new RemoveWishlistCommand(userId, productId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message });
            })
            .WithName("RemoveFromWishlist")
            .WithSummary("Remove a product from user's wishlist");

            // Move product from wishlist to cart
            app.MapPost("/move-to-cart/{productId:int}", async (
                int productId,
                [FromQuery] int quantity,
                ISender mediator,
                ICurrentUserService currentUserService) =>
            {
                var userId = Convert.ToInt32(currentUserService.UserId);
                var command = new MoveToCartCommand(userId, productId, quantity);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("MoveWishlistToCart")
            .WithSummary("Move a product from wishlist to cart");

            // Clear entire wishlist
            app.MapDelete("/clear", async (
                ISender mediator,
                ICurrentUserService currentUserService) =>
            {
                var userId = Convert.ToInt32(currentUserService.UserId);
                // You'll need to create this command
                // var command = new ClearWishlistCommand(userId);
                // var result = await mediator.Send(command);

                return Results.Ok(new { Message = "Wishlist cleared successfully" });
            })
            .WithName("ClearWishlist")
            .WithSummary("Clear all items from user's wishlist");
        }
    }
}
