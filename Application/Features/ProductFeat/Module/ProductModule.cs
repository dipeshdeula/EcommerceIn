using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Features.CategoryFeat.Queries;
using Application.Features.ProductFeat.Commands;
using Application.Features.ProductFeat.DeleteCommands;
using Application.Features.ProductFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.ProductFeat.Module
{
    public class ProductModule : CarterModule
    {
        public ProductModule() : base("")
        {
            WithTags("Product");
            IncludeInOpenApi();
           /* RequireAuthorization();*/
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("products");

            app.MapPost("/create-product", async ([FromQuery] int subSubCategoryId, ISender mediator, CreateProductDTO productDTO) =>
            {
                var command = new CreateProductCommand(subSubCategoryId, productDTO);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapGet("/admin/all", async (               
            [FromServices] ISender mediator,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool includeDeleted = true 
            ) =>
        {
            var query = new GetAllProductQuery(
                PageNumber: pageNumber,
                PageSize: pageSize,
                UserId: null, 
                OnSaleOnly: null,
                PrioritizeEventProducts: false, 
                SearchTerm: searchTerm,
                IncludeDeleted: includeDeleted,
                IsAdminRequest: true 
            );

            var result = await mediator.Send(query);

            return result.Succeeded
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .RequireAuthorization("RequireAdminOrVendor") 
        .WithName("GetAllProductsForAdmin")
        .WithSummary("Admin: Get all products including deleted ones")
        .WithDescription("Retrieve all products with admin privileges, including deleted products for management purposes")
        .Produces<Result<IEnumerable<ProductDTO>>>(StatusCodes.Status200OK);


            //  ENHANCED: GetAllProducts with event prioritization
            app.MapGet("/getAllProducts", async ([FromServices] ISender mediator,
                [FromQuery] int PageNumber =1 ,
                [FromQuery] int PageSize =10,
                [FromQuery] int? UserId = null,
                [FromQuery] bool? OnSaleOnly = null ,
                [FromQuery] bool? PrioritizeEventProducts = null, 
                [FromQuery] string? SearchTerm =null) =>
            {
                var result = await mediator.Send(new GetAllProductQuery(
                    PageNumber,
                    PageSize,
                    UserId,
                    OnSaleOnly,
                    PrioritizeEventProducts,
                    SearchTerm,
                    IncludeDeleted: false, 
                    IsAdminRequest: false  
                    ));

                

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            }).WithName("GetAllProductsWithDynamicPricing")
            .WithSummary("Get all products with real-time event-based pricing")
            .WithDescription("Retrieves products with dynamic pricing. Event products are shown first by default.")
            .Produces<IEnumerable<ProductDTO>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");

            // NEW: Get products currently on sale
            app.MapGet("/onSale", async ([FromServices] ISender mediator,
                int PageNumber = 1,
                int PageSize = 20) =>
            {
                var result = await mediator.Send(new GetAllProductQuery(
                    PageNumber,
                    PageSize,
                    UserId:null,
                    OnSaleOnly: true,
                    PrioritizeEventProducts: true,
                    SearchTerm:null));

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            }).WithName("GetProductsOnSale")
            .WithSummary("Get all products currently on sale")
            .WithDescription("Retrieves only products with active discounts/events")
            .Produces<IEnumerable<ProductDTO>>(StatusCodes.Status200OK)
            .WithTags("Products");

            app.MapGet("/getProductById", async ([FromQuery] int productId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetProductByIdQuery(productId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllProductBySubSubCategoryId", async ([FromQuery] int SubSubCategoryId, ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllProductsBySubSubCategoryIdQuery(SubSubCategoryId, PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).WithName("Brand")
            .Produces<IEnumerable<CategoryWithProductsDTO>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");

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
            .RequireAuthorization("RequireAdminOrVendor")
            .DisableAntiforgery()
            .Accepts<UploadProductImagesCommand>("multipart/form-data")
            .Produces<IEnumerable<ProductImageDTO>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

            app.MapGet("/nearby", async (
                ISender mediator,
                double? lat = null,
                double? lon = null,
                double radius = 5.0,
                int skip = 0,
                int take = 10,
                int? addressId = null,
                bool useUserLocation = true

                ) =>
            {
                var query = new GetNearbyProductsQuery(
                    lat, lon, radius, skip, take,addressId,useUserLocation);
                var result = await mediator.Send(query);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            }).WithName("GetNearbyProducts")
            .WithSummary("Get nearby products based on user location or manual coordinates")
            .WithDescription("Retrieves products from stores within specified radius. Uses user's address by default or manual coordinates.")
            .Produces<IEnumerable<NearbyProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");

            app.MapPut("/updateProduct", async (
                [FromQuery] int ProductId,
                [FromBody] UpdateProductDTO udpateProductDto,
                ISender mediator) =>
            {
                var command = new UpdateProductCommand(ProductId, udpateProductDto);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/softdDeleteProduct", async ([FromQuery] int productId, ISender mediator) =>
            {
                var result = await mediator.Send(new SoftDeleteProductCommand(productId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("hardDeleteProduct", async ([FromQuery] int productId, ISender mediator) =>
            {
                var result = await mediator.Send(new HardDeleteProductCommand(productId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapDelete("/unDeleteProduct", async ([FromQuery] int productId, ISender mediator) =>
            {
                var result = await mediator.Send(new UnDeleteProductCommand(productId));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            }).RequireAuthorization("RequireAdminOrVendor");

            


        }
    }
}
