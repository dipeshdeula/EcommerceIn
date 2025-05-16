using Application.Common;
using Application.Dto;
using Application.Features.BannerSpecialEvent.Commands;
using Application.Features.BannerSpecialEvent.DeleteCommands;
using Application.Features.BannerSpecialEvent.Queries;
using Application.Features.CategoryFeat.Commands;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Xml.Linq;

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
            app = app.MapGroup("bannerEventSpecial");


            app.MapPost("/create", async (ISender mediator,
               string Name,              
               string Description,
               double Offers,
               DateTime StartDate,
               DateTime EndDate
              ) =>
            {
                var command = new CreateBannerSpecialEventCommand(Name, Description,Offers,StartDate,EndDate);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).DisableAntiforgery()
               .Accepts<CreateBannerSpecialEventCommand>("multipart/form-data")
               .Produces<BannerEventSpecialDTO>(StatusCodes.Status200OK)
               .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapGet("/getAllBannerEventSpecail", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllBannerEventSpecialQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/UploadBannerImages", async (ISender mediator, [FromForm] int bannerId, [FromForm] IFormFileCollection files) =>
            {
                var command = new UploadBannerImageCommand(bannerId, files);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
       .DisableAntiforgery()
       .Accepts<UploadBannerImageCommand>("multipart/form-data")
       .Produces<IEnumerable<BannerImageDTO>>(StatusCodes.Status200OK)
       .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapPut("/updateBannerEventSpecial", async (
                int BannerId,
                string? Name,
                string? Description,
                double? Offers,
                DateTime? StartDate,
                DateTime? EndDate,
                bool? IsActive,
                ISender mediator) =>
            {

                var command = new UpdateBannerSpecialEventCommand(
                    BannerId, Name, Description, Offers, StartDate, EndDate, IsActive);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapPut("/activeStatus", async (int BannerId, bool IsActive, ISender mediator) =>
            {
                var command = new UpdateBannerEventSpecialActiveStatus(BannerId, IsActive);
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

                if(!result.Succeeded)
                    return Results.BadRequest(new {result.Message,result.Data});
                return Results.Ok(new { result.Message,result.Data});
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
