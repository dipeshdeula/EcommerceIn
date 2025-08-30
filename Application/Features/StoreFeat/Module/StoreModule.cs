using Application.Dto.StoreDTOs;
using Application.Features.StoreFeat.Commands;
using Application.Features.StoreFeat.DeleteCommands;
using Application.Features.StoreFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.StoreFeat.Module
{
    public class StoreModule : CarterModule
    {
        public StoreModule() : base("")
        {
            WithTags("Store");
            IncludeInOpenApi();
            //RequireAuthorization();

        }

        public override async void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("store");
            app.MapPost("/create", async (ISender mediator,
                [FromForm] string Name,
                [FromForm] string OwnerName,
                [FromForm] IFormFile File
                ) =>
            {
                var command = new CreateStoreCommand(Name, OwnerName, File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
             .RequireAuthorization("RequireAdminOrVendor")
            .DisableAntiforgery()
            .Accepts<CreateStoreCommand>("multipart/form-data")
            .Produces<StoreDTO>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapGet("/getAllStores", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllStoreQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPut("/updateStore", async (
                [FromQuery] int Id,
                [FromForm] string? Name, 
                [FromForm] string? OwnerName, 
                [FromForm] IFormFile? File,
                ISender mediator) =>
            {
                var command = new UpdateStoreCommand(Id, Name, OwnerName, File);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            })
              .RequireAuthorization("RequireAdminOrVendor")
             .DisableAntiforgery()
            .Accepts<UpdateStoreCommand>("multipart/form-data")
            .Produces<StoreDTO>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapDelete("/softDeleteStore", async (int Id, ISender mediator) =>
            {
                var command = new SoftDeleteStoreCommand(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new {result.Message, result.Data});

            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/unDeleteStore", async (int Id, ISender mediator) =>
            {
                var command = new UnDeleteStoreCommand(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });

            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/hardDeleteStore", async (int Id, ISender mediator) =>
            {
                var command = new HardDeleteStoreCommand(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });

            }).RequireAuthorization("RequireAdminOrVendor");
        }

    }

}
