using Application.Dto.OrderDTOs;
using Application.Features.OrderFeat.Commands;
using Application.Features.OrderFeat.Queries;
using Application.Features.OrderFeat.UpdateCommands;
using Carter;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.OrderFeat.Module
{
    public class OrderModule : CarterModule
    {
        public OrderModule() : base("")
        {
            WithTags("Order");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("Order");

            app.MapPost("/place-order", async (CreatePlaceOrderCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { message = result.Message });
                }
                return Results.Ok(result.Data);
            });

            app.MapGet("/getAllOrder", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllOrderQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapGet("/getOrderById", async (ISender mediator, int Id) =>
            {
                var command = new GetOrderByIdQuery(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllOrderByUserId", async (ISender mediator, int UserId, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetOrderByUserIdQuery(UserId, PageNumber, PageSize);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPut("/confirmOrderStatus", async (ISender mediator, int OrderId, bool IsConfirmed) =>
            {
                var command = new UpdateOrderConfirmedCommand(OrderId, IsConfirmed);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors, orderId = OrderId });
                }

                return Results.Ok(new { result.Message, result.Data, notificationSummary = result.Data.NotificationResult });
            })
               /* .RequireAuthorization("RequireAdmin")*/
                .WithName("ConfirmOrderStatus")
                .WithSummary("Confirm or update order status")
                .WithDescription("Updates order confirmation status and sends notifications to customer")
                .Produces<OrderConfirmationResponseDTO>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }
}
