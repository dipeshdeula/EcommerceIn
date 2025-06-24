using Application.Dto.CartItemDTOs;
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

            //  app.MapDelete("/deleteCartItemByUserId", async (
            //     int Id, int? UserId, ISender mediator) =>
            // {
            //     var command = new HardDeleteCartItemCommand(Id, UserId);
            //     var result = await mediator.Send(command);

            //     if (!result.Succeeded)
            //         return Results.BadRequest(new { result.Message, result.Errors });
            //     return Results.Ok(new { result.Message, result.Data });

            // });


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
        }
    }
}
