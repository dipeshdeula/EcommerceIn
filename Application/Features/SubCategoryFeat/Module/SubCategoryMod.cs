using Application.Dto;
using Application.Features.CategoryFeat.Queries;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.SubCategoryFeat.Commands;
using Application.Features.SubCategoryFeat.DeleteCommands;
using Application.Features.SubCategoryFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.SubCategoryFeat.Module
{
    public class SubCategoryMod : CarterModule
    {
        public SubCategoryMod() : base("")
        {
            WithTags("SubCategory");
            IncludeInOpenApi();
            RequireAuthorization();

        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("subCategory");          

            app.MapPost("/create-subCategory", async (
                [FromQuery] int CategoryId, string Name,string Slug,string Description,IFormFile File, ISender mediator) =>
            {
                var command = new CreateSubCategoryCommand(CategoryId, Name, Slug, Description, File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
            .RequireAuthorization("RequireAdminOrVendor")
            .DisableAntiforgery()
            .Accepts<CreateSubCategoryCommand>("multipart/form-data")
            .Produces<SubCategoryDTO>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapGet("/getAllSubCategory", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllSubCategoryQuery(PageNumber, PageSize));

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getSubCategoryById", async ([FromQuery] int subCategoryId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetSubCategoryByIdQuery(subCategoryId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPut("/updateSubCategory", async (
                int SubCategoryId,string? Name,string? Slug,string? Description, IFormFile? File, ISender mediator) =>
            {
                var command = new UpdateSubCategoryCommand(SubCategoryId, Name, Slug, Description, File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            })
               .RequireAuthorization("RequireAdminOrVendor")
               .DisableAntiforgery()
              .Accepts<UpdateSubCategoryCommand>("multipart/form-data")
              .Produces<SubCategoryDTO>(StatusCodes.Status200OK)
              .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapDelete("/softDeleteSubCategory", async (
                int SubCategoryId, ISender mediator
                ) =>
            {
                var command = new SoftDeleteSubCategoryCommand(SubCategoryId);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/unDeleteSubCategory", async (
               int SubCategoryId, ISender mediator
               ) =>
            {
                var command = new UnDeleteSubCategoryCommand(SubCategoryId);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/hardDeleteSubCategory", async (
               int SubCategoryId, ISender mediator
               ) =>
            {
                var command = new HardDeleteSubCategoryCommand(SubCategoryId);
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
