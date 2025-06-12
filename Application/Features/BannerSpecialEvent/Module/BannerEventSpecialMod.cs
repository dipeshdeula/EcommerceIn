using Application.Common.Models;
using Application.Dto;
using Application.Dto.BannerEventSpecialDTOs;
using Application.Features.BannerSpecialEvent.Commands;
using Application.Features.BannerSpecialEvent.DeleteCommands;
using Application.Features.BannerSpecialEvent.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.BannerSpecialEvent.Module
{
    public class BannerEventSpecialMod : CarterModule
    {
        public BannerEventSpecialMod() : base("")
        {
            WithTags("BannerEventSpecial");
            IncludeInOpenApi();
        }
        public override async void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("api/banner-events")
                .WithTags("Banner Event Special");


            app.MapPost("/create", async (ISender mediator,
               CreateBannerSpecialEventDTO bannerSpecialEventDTO
              ) =>
            {
                var command = new CreateBannerSpecialEventCommand(bannerSpecialEventDTO);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Created($"/api/banner-events/{result.Data?.Id}", new { result.Message, result.Data });
            })
                /* .RequireAuthorization("RequireAdmin", "RequireVendor")*/
                .DisableAntiforgery()
               .Accepts<CreateBannerSpecialEventCommand>("application/json")
               .Produces<BannerEventSpecialDTO>(StatusCodes.Status200OK)
               .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
               .WithName("CreateBannerEvent")
                .WithSummary("Create a new banner event")
                .WithDescription("Creates a new banner event with optional rules and product associations");

            // Get all banner events with filtering and pagination
            app.MapGet("/", async (
                ISender mediator,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] bool includeDeleted = false,
                [FromQuery] string? status = null,
                [FromQuery] bool? isActive = null) =>
            {
                var query = new GetAllBannerEventSpecialQuery(
                    PageNumber: pageNumber,
                    PageSize: pageSize,
                    IncludeDeleted: includeDeleted,
                    Status: status,
                    IsActive: isActive
                );

                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        message = result.Message,
                        errors = result.Errors
                    });
                }

                return Results.Ok(new
                {
                    message = result.Message,
                    data = result.Data
                });
            })
            /*  .RequireAuthorization("RequireAdmin")*/
            .Produces<PagedResult<BannerEventSpecialDTO>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .WithName("GetAllBannerEvents")
            .WithSummary("Get all banner events")
            .WithDescription("Retrieves paginated list of banner events with optional filtering");

            app.MapGet("/getBannerEventById", async (ISender mediator, [FromQuery] int bannerId) =>
            {
                var command = new GetBannerEventByIdQuery(bannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/UploadBannerImage", async (ISender mediator, [FromForm] int bannerId, [FromForm] IFormFileCollection files) =>
            {
                var command = new UploadBannerImageCommand(bannerId, files);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
               /* .RequireAuthorization("RequireAdmin")*/
               .DisableAntiforgery()
               .Accepts<UploadBannerImageCommand>("multipart/form-data")
               .Produces<IEnumerable<BannerImageDTO>>(StatusCodes.Status200OK)
               .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
            ;

            app.MapPut("/ActivateBannerEventSpecial", async (
                int BannerEventId,
                bool IsActive,
                ISender mediator) =>
            {
                var command = new ActivateBannerEventCommand(
                    BannerEventId, IsActive);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });

            });



            app.MapDelete("softDeleteBannerEvent", async (int BannerId, ISender mediator) =>
            {
                var command = new SoftDeleteBannerEventCommand(BannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Data });

                return Results.Ok(new { result.Message, result.Data });
            });
            app.MapDelete("UnDeleteBannerEvent", async (int BannerId, ISender mediator) =>
            {
                var command = new UnDeleteBannerEventCommand(BannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Data });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("HardDeleteBannerEvent", async (int BannerId, ISender mediator) =>
            {
                var command = new HardDeleteBannerEventCommand(BannerId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Data });
                return Results.Ok(new { result.Message, result.Data });

            });


        }
    }
}
