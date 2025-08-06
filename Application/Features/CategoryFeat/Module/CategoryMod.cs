using Application.Features.CategoryFeat.Commands;
using Application.Features.CategoryFeat.Queries;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.CategoryFeat.DeleteCommands;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Application.Dto.CategoryDTOs;

namespace Application.Features.CategoryFeat.Module
{
    public class CategoryMod : CarterModule
    {
        public CategoryMod() : base("")
        {
            WithTags("Category");
            IncludeInOpenApi();
            RequireAuthorization();

        }

        public override async void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("category");
           

            app.MapPost("/create", async (ISender mediator,
                [FromForm] string Name,
                [FromForm] string Slug,
                [FromForm] string Description,
                [FromForm] IFormFile File) =>
            {
                var command = new CreateCategoryCommand(Name,Slug,Description,File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
                .RequireAuthorization("RequireAdminOrVendor")
                .DisableAntiforgery()
                .Accepts<CreateCategoryCommand>("multipart/form-data")
                .Produces<CategoryDTO>(StatusCodes.Status200OK)
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);



            app.MapGet("/getAllCategory", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllCategoryQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });         
              
           

            app.MapGet("/getCategoryById", async (
            int categoryId,
            ISender mediator,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? userId = null,
            [FromQuery] bool includeProducts = true,
            [FromQuery] bool includeDeleted = false) =>
        {
            var query = new GetCategoryByIdQuery(
                CategoryId: categoryId,
                PageNumber: pageNumber,
                PageSize: pageSize,
                UserId: userId,
                IncludeProducts: includeProducts,
                IncludeDeleted: includeDeleted
            );

            var result = await mediator.Send(query);

            if (!result.Succeeded)
                return Results.NotFound(new { result.Message, result.Errors });

            return Results.Ok(new { result.Message, result.Data });
        })
        .WithName("GetCategoryById")
        .WithSummary("Get category by ID with products and statistics")
        .WithDescription("Fetches a specific category with its products, subcategories, and calculated statistics like product count, price range, etc.");
            app.MapGet("/getAllProdcutByCategoryById", async ([FromQuery] int categoryId, ISender mediator, int PageNumber = 1, int PageSize = 10, string? sortBy = null) =>
            {
                var result = await mediator.Send(new GetAllProductsByCategoryId(categoryId, PageNumber, PageSize,SortBy:sortBy));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).WithName("Category")
            .Produces<IEnumerable<CategoryWithProductsDTO>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");


            app.MapPut("/updateCategory", async (
                [FromQuery] int CategoryId, 
                [FromForm] string? Name,
                [FromForm] string? Slug,
                [FromForm] string? Description,
                [FromForm] IFormFile? File,
                ISender mediator) =>
            {
                var command = new UpdateCategoryCommand(CategoryId, Name, Slug, Description, File);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
               .RequireAuthorization("RequireAdminOrVendor")
              .DisableAntiforgery()
              .Accepts<UpdateCategoryCommand>("multipart/form-data")
              .Produces<CategoryDTO>(StatusCodes.Status200OK)
              .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);


            app.MapDelete("/softDeleteCategory", async ([FromQuery] int categoryId, ISender mediator) =>
            {
                var command = new SoftDeleteCategoryCommand(categoryId);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });

                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/unDeleteCategory", async ([FromQuery] int categoryId, ISender mediator) =>
            {
                var command = new UnDeleteCategoryCommand(categoryId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/hardDeleteCategory", async ([FromQuery] int categoryId, ISender mediator) =>
            {
                var command = new HardDeleteCategoryCommand(categoryId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdmin","RequireVendo");


        }
    }
}
