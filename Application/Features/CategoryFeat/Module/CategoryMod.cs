using Application.Dto;
using Application.Features.CategoryFeat.Commands;
using Application.Features.CategoryFeat.DeleteCategoryCommands;
using Application.Features.CategoryFeat.DeleteCommands;
using Application.Features.CategoryFeat.Queries;
using Application.Features.CategoryFeat.UpdateCommands;
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

            app.MapPost("/create-subCategory", async ([FromQuery] int ParentCategoryId, [FromForm] CreateSubCategoryCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            })
             .DisableAntiforgery()
             .Accepts<CreateSubCategoryCommand>("multipart/form-data")
             .Produces<SubCategoryDTO>(StatusCodes.Status200OK)
             .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);


            app.MapPost("/create-subSubCategory", async ([FromQuery] int subCategoryId, ISender mediator, [FromForm] CreateSubSubCategoryCommand command) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/create-product", async ([FromQuery] int subSubCategoryId, [FromServices] ISender mediator, CreateProductCommand command) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllCategory", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
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

            app.MapGet("/getAllSubSubCategory", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllSubSubCategory(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllProducts", async ([FromServices] ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllProductQuery(PageNumber, PageSize));
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

            app.MapGet("/getSubCategoryById", async ([FromQuery] int subCategoryId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetSubCategoryByIdQuery(subCategoryId, PageNumber, PageSize));
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

            app.MapGet("/getProductById", async ([FromQuery] int productId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetProductByIdQuery(productId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapPost("/UploadProductImages", async (ISender mediator, [FromForm] int productId, [FromForm] IFormFileCollection files) =>
            {
                var command = new UploadProductImagesCommand(productId, files);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .DisableAntiforgery()
            .Accepts<UploadProductImagesCommand>("multipart/form-data")
            .Produces<IEnumerable<ProductImageDTO>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

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

            app.MapPut("/updateSubCategory", async ([FromForm] UpdateSubCategoryCommand command, ISender mediator) => {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
                
            });

            app.MapPut("/updateSubSubCategory", async ([FromForm] UpdateSubSubCategoryCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapPut("/updateProduct", async ([FromForm] UpdateProductCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/softDeleteCategory", async ([FromQuery] int categoryId, ISender mediator) =>
            {
                var result = await mediator.Send(new SoftDeleteCategoryCommand(categoryId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });

                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("/softdDeleteProduct", async ([FromQuery] int productId, ISender mediator) =>
            {
                var result = await mediator.Send(new SoftDeleteProductCommand(productId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapDelete("hardDeleteProduct", async ([FromQuery] int productId, ISender mediator) =>
            {
                var result = await mediator.Send(new HardDeleteProductCommand(productId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            });

            app.MapDelete("/unDeleteProduct", async ([FromQuery] int productId, ISender mediator) =>
            {
                var result = await mediator.Send(new UnDeleteProductCommand(productId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            });



        }
    }
}
