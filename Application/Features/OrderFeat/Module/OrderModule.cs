﻿using Application.Dto.OrderDTOs;
using Application.Features.OrderFeat.Commands;
using Application.Features.OrderFeat.DeleteCommands;
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
            RequireAuthorization();
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

            }).RequireAuthorization("RequireAdmin");

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
                .RequireAuthorization("RequireAdmin")
                .WithName("ConfirmOrderStatus")
                .WithSummary("Confirm or update order status")
                .WithDescription("Updates order confirmation status and sends notifications to customer")
                .Produces<OrderConfirmationResponseDTO>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);

            app.MapPut("/cancelOrder", async (ISender mediator, int OrderId, string ReasonToCancel, bool IsConfirmed) =>
            {
                var command = new UpdateOrderCancelledCommand(OrderId, ReasonToCancel,IsConfirmed);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors, orderId = OrderId });
                }

                return Results.Ok(new { result.Message, result.Data, notificationSummary = result.Data.NotificationResult });
            })
               .RequireAuthorization()
               .WithName("CancelOrder")
               .WithSummary("Cancel your order")
               .WithDescription("Updates order cancellation status and sends notifications to customer")
               .Produces<OrderCancellationRepsonseDTO>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status400BadRequest)
               .Produces(StatusCodes.Status401Unauthorized);

            app.MapDelete("/softDeleteOrder", async (int Id, ISender mediator) =>
            {
                var command = new SoftDeleteOrderCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/unDeleteOrder", async (int Id, ISender mediator) =>
            {
                var command = new UnDeleteOrderCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/hardDeleteOrder", async (int Id, ISender mediator) =>
            {
                var command = new HardDeleteOrderCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });
        }
    }
}
