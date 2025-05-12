using Application.Dto;
using Application.Features.BannerSpecialEvent.Commands;
using Application.Features.BannerSpecialEvent.Queries;
using Application.Features.CategoryFeat.Commands;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
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
               DateTime EndDate,
               IFormFile File) =>
            {
                var command = new CreateBannerSpecialEventCommand(Name, Description,Offers,StartDate,EndDate, File);
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





        }
    }
}
