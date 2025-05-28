using Application.Enums;
using Application.Features.ImageFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.ImageFeat.Module
{
    public class ImageMod : CarterModule
    {
        public ImageMod() : base("")
        {
            WithTags("GetImage");
            IncludeInOpenApi();

        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("getImage");

            app.MapGet("/images/{folderName}/{fileName}", async (
                 [FromRoute] string folderName,
                 [FromRoute] string fileName,
                 [FromServices] ISender mediator) =>
            {
                var stream = await mediator.Send(new GetImageQuery(fileName, folderName));
                if (stream == null)
                    return Results.NotFound("Image not found");

                // You can use a more dynamic content type if needed
                return Results.File(stream, "image/jpeg");
            })
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);


        }
    }
}
