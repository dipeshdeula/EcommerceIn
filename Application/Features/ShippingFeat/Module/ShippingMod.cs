using Application.Dto.ShippingDTOs;
using Application.Features.ShippingFeat.Commands;
using Application.Features.ShippingFeat.DeleteCommands;
using Application.Features.ShippingFeat.Queries;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.ShippingFeat.Module
{
    public class ShippingMod : CarterModule
    {
        public ShippingMod() : base("shipping")
        {
            WithTags("Shipping");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            // Customer endpoints
            /*app.MapPost("/calculate", async (
                [FromBody] CalculateShippingCommand command,
                [FromServices] ISender mediator) =>
            {
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("CalculateShipping")
            .WithSummary("Calculate shipping cost for order")
            .WithDescription("Calculate shipping cost based on order total and delivery location")
            .Produces<ShippingCalculationDetailDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);*/

            app.MapGet("/info", async ([FromServices] ISender mediator) =>
            {
                var query = new GetShippingInfoQuery();
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(result.Data);
            })
            .WithName("GetShippingInfo")
            .WithSummary("Get shipping information and rates")
            .Produces<object>(StatusCodes.Status200OK);

            // Admin endpoints
            app.MapGet("/admin/all", async (
                [FromServices] ISender mediator,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10) =>
            {
                var query = new GetAllShippingQuery(pageNumber, pageSize);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("GetAllShippingConfigurations")
            .WithSummary("Get all shipping configurations (Admin)")
            .Produces<List<ShippingDTO>>(StatusCodes.Status200OK);

            app.MapGet("/admin/{id:int}", async (
                int id,
                [FromServices] ISender mediator) =>
            {
                var query = new GetShippingByIdQuery(id);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.NotFound(new { result.Message });

                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("GetShippingConfigurationById")
            .WithSummary("Get shipping configuration by ID (Admin)")
            .Produces<ShippingDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPost("/admin/create", async (
                [FromBody] CreateShippingDTO request,
                [FromServices] ISender mediator,
                [FromServices] ICurrentUserService currentUserService) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId <= 0)
                    return Results.Unauthorized();

                var command = new CreateShippingCommand(
                    request.Name,
                    request.LowOrderThreshold,
                    request.LowOrderShippingCost,
                    request.HighOrderShippingCost,
                    request.FreeShippingThreshold,
                    request.EstimatedDeliveryDays,
                    request.MaxDeliveryDistanceKm,
                    request.EnableFreeShippingEvents,
                    request.IsFreeShippingActive,
                    request.FreeShippingStartDate,
                    request.FreeShippingEndDate,
                    request.FreeShippingDescription,
                    request.WeekendSurcharge,
                    request.HolidaySurcharge,
                    request.RushDeliverySurcharge,
                    request.CustomerMessage,
                    request.AdminNotes,
                    request.SetAsDefault,
                    userId
                );

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Created($"/shipping/admin/{result.Data.Id}", new { result.Message, result.Data });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("CreateShippingConfiguration")
            .WithSummary("Create new shipping configuration (Admin)")
            .Produces<ShippingDTO>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

            app.MapPut("/admin/{id:int}", async (
                int id,
                [FromBody] CreateShippingDTO request,
                [FromServices] ISender mediator,
                [FromServices] ICurrentUserService currentUserService) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId <= 0)
                    return Results.Unauthorized();

                var command = new UpdateShippingCommand(
                    id,
                    request.Name,
                    request.LowOrderThreshold,
                    request.LowOrderShippingCost,
                    request.HighOrderShippingCost,
                    request.FreeShippingThreshold,
                    request.EstimatedDeliveryDays,
                    request.MaxDeliveryDistanceKm,
                    request.EnableFreeShippingEvents,
                    request.IsFreeShippingActive,
                    request.FreeShippingStartDate,
                    request.FreeShippingEndDate,
                    request.FreeShippingDescription,
                    request.WeekendSurcharge,
                    request.HolidaySurcharge,
                    request.RushDeliverySurcharge,
                    request.CustomerMessage,
                    request.AdminNotes,
                    request.SetAsDefault,
                    userId
                );

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.NotFound(new { result.Message });

                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("UpdateShippingConfiguration")
            .WithSummary("Update shipping configuration (Admin)")
            .Produces<ShippingDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPut("/admin/{id:int}/set-default", async (
                int id,
                [FromServices] ISender mediator,
                [FromServices] ICurrentUserService currentUserService) =>
            {
                var userId = currentUserService.GetUserIdAsInt();
                if (userId <= 0)
                    return Results.Unauthorized();

                var command = new SetDefaultShippingCommand(id, userId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("SetDefaultShippingConfiguration")
            .WithSummary("Set configuration as default (Admin)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            app.MapDelete("/admin/{id:int}/soft-delete", async (
                int id,
                [FromServices] ISender mediator) =>
            {
                var command = new SoftDeleteShippingCommand(id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("SoftDeleteShippingConfiguration")
            .WithSummary("Soft delete shipping configuration (Admin)")
            .Produces(StatusCodes.Status200OK);

            app.MapDelete("/admin/{id:int}/hard-delete", async (
                int id,
                [FromServices] ISender mediator) =>
            {
                var command = new HardDeleteShippingCommand(id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message });

                return Results.Ok(new { result.Message });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("HardDeleteShippingConfiguration")
            .WithSummary("Permanently delete shipping configuration (Admin)")
            .Produces(StatusCodes.Status200OK);

            app.MapGet("/admin/active", async ([FromServices] ISender mediator) =>
            {
                var query = new GetActiveShippingQuery();
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.NotFound(new { result.Message });

                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization("RequireAdmin")
            .WithName("GetActiveShippingConfiguration")
            .WithSummary("Get currently active shipping configuration (Admin)")
            .Produces<ShippingDTO>(StatusCodes.Status200OK);
        }
    }
}