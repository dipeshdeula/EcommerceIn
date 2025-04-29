using Application.Dto;
using Application.Features.CategoryFeat.Commands;
using Application.Features.CategoryFeat.DeleteCategoryCommands;
using Application.Features.CategoryFeat.DeleteCommands;
using Application.Features.CategoryFeat.Queries;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.ProductFeat.Commands;
using Application.Features.SubCategoryFeat.Commands;
using Application.Features.SubSubCategoryFeat.Commands;
using Carter;
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

        public override async void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("category");

            app.MapPost("/create", async (ISender mediator, [FromForm] CreateCategoryCommand command) =>
            {

                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).DisableAntiforgery()
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
              
           

            app.MapGet("/getCategoryById", async ([FromQuery] int categoryId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetCategoryByIdQuery(categoryId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllProdcutByCategoryById", async ([FromQuery] int categoryId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllProductsByCategoryId(categoryId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });


            app.MapPut("/updateCategory", async ([FromForm] UpdateCategoryCommand command, ISender mediator) =>
            {

                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
              .DisableAntiforgery()
              .Accepts<UpdateCategoryCommand>("multipart/form-data")
              .Produces<CategoryDTO>(StatusCodes.Status200OK)
              .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            

           

          
            app.MapDelete("/softDeleteCategory", async ([FromQuery] int categoryId, ISender mediator) =>
            {
                var result = await mediator.Send(new SoftDeleteCategoryCommand(categoryId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });

                }
                return Results.Ok(new { result.Message, result.Data });
            });

           


        }
    }
}
