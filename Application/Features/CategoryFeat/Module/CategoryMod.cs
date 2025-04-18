using Application.Features.CategoryFeat.Commands;
using Application.Features.CategoryFeat.Queries;
using Carter;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.CategoryFeat.Module
{
    public class CategoryMod : CarterModule
    {
        public CategoryMod() : base("")
        {
            WithTags("Category");
            IncludeInOpenApi();

        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("category");

            app.MapPost("/create", async ([FromServices] ISender mediator, CreateCategoryCommand command) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/create-subCategory", async([FromQuery] int ParentCategoryId, [FromServices] ISender mediator, CreateSubCategoryCommand command) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/create-subSubCategory", async ([FromQuery] int subCategoryId, [FromServices] ISender mediator, CreateSubSubCategoryCommand command) => 
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllCategory", async ([FromServices] ISender mediator , int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllCategoryQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllSubCategory", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllSubCategoryQuery(PageNumber, PageSize));

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });
        }
    }
}
