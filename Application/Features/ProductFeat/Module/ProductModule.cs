using Application.Dto;
using Application.Features.ProductFeat.DeleteCommands;
using Application.Features.CategoryFeat.UpdateCommands;
using Application.Features.ProductFeat.Commands;
using Application.Features.ProductFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.ProductFeat.Module
{
    public class ProductModule : CarterModule
    {
        public ProductModule() : base("") {
            WithTags("Product");
            IncludeInOpenApi();
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("products");

            app.MapPost("/create-product", async ([FromQuery] int subSubCategoryId, ISender mediator, CreateProductCommand command) =>
            {
                var result = await mediator.Send(command);
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
            app.MapGet("/nearby", async (ISender mediator,double lat, double lon, double radius,int skip = 0,int take = 10) =>
            {
                var query = new GetNearbyProductsQuery(lat, lon, radius,skip,take);
                var result = await mediator.Send(query);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            }).WithName("GetNearbyProducts")
            .Produces<IEnumerable<NearbyProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");

            app.MapPut("/updateProduct", async ([FromForm] UpdateProductCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
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
