using Application.Dto;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.SubSubCategoryFeat.Commands;
using Application.Features.SubSubCategoryFeat.DeleteCommands;
using Application.Features.SubSubCategoryFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.SubSubCategoryFeat.Module
{
    public class SubSubCategoryMod : CarterModule
    {
        public SubSubCategoryMod() : base("")
        {
            WithTags("SubSubCategory");
            IncludeInOpenApi();
            RequireAuthorization();

        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("subSubCategory");
            app.MapPost("/create-subSubCategory", async (
                [FromQuery] int subCategoryId,string Name,string Slug,string Description,IFormFile File, ISender mediator) =>
            {
                var command = new CreateSubSubCategoryCommand(subCategoryId, Name, Slug, Description, File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
              .RequireAuthorization("RequireAdminOrVendor")
             .DisableAntiforgery()
            .Accepts<CreateSubSubCategoryCommand>("multipart/form-data")
            .Produces<SubSubCategoryDTO>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapGet("/getAllSubSubCategory", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllSubSubCategory(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });


            app.MapGet("/getSubSubCategoryById", async ([FromQuery] int subSubCategoryId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetSubSubCategoryByIdQuery(subSubCategoryId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapPut("/updateSubSubCategory", async (
                int SubSubCategoryId,string? Name,string? Slug,string? Description,IFormFile? File, ISender mediator) =>
            {
                var command = new UpdateSubSubCategoryCommand(SubSubCategoryId, Name, Slug, Description, File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            })
            .RequireAuthorization("RequireAdminOrVendor")
            .DisableAntiforgery()
           .Accepts<UpdateSubSubCategoryCommand>("multipart/form-data")
           .Produces<SubSubCategoryDTO>(StatusCodes.Status200OK)
           .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapDelete("/softDeleteSubSubCategory", async (
                int SubSubCategoryId, ISender mediator
                ) =>
            {
                var command = new SoftDeleteSubSubCategoryCommand(SubSubCategoryId);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/hardDeleteSubSubCategory", async (
                int SubSubCategoryId, ISender mediator
                ) =>
            {
                var command = new HardDeleteSubSubCategoryCommand(SubSubCategoryId);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/unDeleteSubSubCategory", async (
               int SubSubCategoryId, ISender mediator
               ) =>
            {
                var command = new UnDeleteSubSubCategoryCommand(SubSubCategoryId);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");
        }
    }
}
