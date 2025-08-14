using Application.Common;
using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Features.CategoryFeat.Queries;
using Application.Features.ProductFeat.Commands;
using Application.Features.ProductFeat.DeleteCommands;
using Application.Features.ProductFeat.Queries;
using Application.Interfaces.Services;
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
          
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("products");

            app.MapPost("/create-product", async (
                [FromQuery] int categoryId,
                [FromQuery] int? subCategoryId,
                [FromQuery] int? subSubCategoryId, 
                ISender mediator, CreateProductDTO productDTO) =>
            {
                var command = new CreateProductCommand(categoryId,subCategoryId,subSubCategoryId, productDTO);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapGet("/getAllProducts", async (
                [FromServices] ISender mediator,
                [FromServices] ICurrentUserService currentUserService,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] string? searchTerm = null,
                [FromQuery] bool includeDeleted = false,
                [FromQuery] bool? onSaleOnly = null,
                [FromQuery] bool? prioritizeEventProducts = null) =>
            {
                //  Security: Use CurrentUserService for permission checking
                var isAdmin = currentUserService.IsAdmin;
                var userId = currentUserService.GetUserIdAsInt();

                //  Security: Only admin can see deleted products
                var allowDeleted = isAdmin && includeDeleted;

                var query = new GetAllProductQuery(
                    PageNumber: pageNumber,
                    PageSize: pageSize,
                    UserId: userId,
                    OnSaleOnly: onSaleOnly,
                    PrioritizeEventProducts: prioritizeEventProducts,
                    SearchTerm: searchTerm,
                    IncludeDeleted: allowDeleted,
                    IsAdminRequest: isAdmin
                );

                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

               
                return Results.Ok(new
                {
                    result.Message,
                    Data = result.Data,
                    Pagination = new
                    {
                        result.TotalCount,
                        result.PageNumber,
                        result.PageSize,
                        result.TotalPages,
                        result.HasNextPage,
                        result.HasPreviousPage
                    },
                    Context = new
                    {
                        IsAdmin = isAdmin,
                        IncludesDeleted = allowDeleted,
                        UserId = userId,
                        CanManage = currentUserService.CanManageProducts,
                        Timestamp = DateTime.UtcNow
                    }
                });
            })
            .WithName("GetAllProductsUnified")
            .WithSummary("Get all products (unified endpoint)")
            .WithDescription("Unified endpoint that shows appropriate products based on user permissions. Admin can see deleted products with includeDeleted=true, regular users see only active products.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithTags("Products");

           

           //  FEATURED PRODUCTS - Always active products only
            app.MapGet("/featured", async (
                [FromServices] ISender mediator,
                [FromServices] ICurrentUserService currentUserService,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 20) =>
            {
                var query = new GetAllProductQuery(
                    PageNumber: pageNumber,
                    PageSize: pageSize,
                    UserId: currentUserService.GetUserIdAsInt(),
                    OnSaleOnly: true,
                    PrioritizeEventProducts: true,
                    SearchTerm: null,
                    IncludeDeleted: false, // Always false for featured
                    IsAdminRequest: false
                );

                var result = await mediator.Send(query);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new
                {
                    result.Message,
                    Data = result.Data,
                    Pagination = new
                    {
                        result.TotalCount,
                        result.PageNumber,
                        result.PageSize,
                        result.TotalPages,
                        result.HasNextPage,
                        result.HasPreviousPage
                    }
                });
            })
            .WithName("GetFeaturedProducts")
            .WithSummary("Get featured products on sale")
            .WithDescription("Retrieves only active products with discounts/events")
            .WithTags("Products");

            app.MapGet("/getProductById", async (
            [FromQuery] int productId, 
            [FromServices] ISender mediator,
            [FromServices] ICurrentUserService currentUserService,
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeDeleted = false) =>  
        {
            //  Use CurrentUserService for permission checking
            var isAdmin = currentUserService.IsAdmin;
            var userId = currentUserService.GetUserIdAsInt();
            
            // Only admin can see deleted products
            var allowDeleted = isAdmin && includeDeleted;

            var query = new GetProductByIdQuery(productId, pageNumber, pageSize)
            {
                UserId = userId,
                IsAdminRequest = isAdmin,
                IncludeDeleted = allowDeleted
            };

            var result = await mediator.Send(query);

            if (!result.Succeeded)
                return Results.NotFound(new { result.Message, result.Errors });

            return Results.Ok(new 
            { 
                result.Message, 
                result.Data,
                Context = new 
                { 
                    IsAdmin = isAdmin, 
                    UserId = userId,
                    CanManage = currentUserService.CanManageProducts,
                    IncludesDeleted = allowDeleted,
                    RequestedDeleted = includeDeleted,
                    Timestamp = DateTime.UtcNow
                }
            });
        })
        .WithName("GetProductById")
        .WithSummary("Get product by ID")
        .WithDescription("Get a specific product. Admin can see deleted products with includeDeleted=true.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("Products");

           app.MapGet("/getAllProductBySubSubCategoryId", async (
            [FromQuery] int SubSubCategoryId,
            [FromServices] ISender mediator,
            [FromServices] ICurrentUserService currentUserService,
            [FromQuery] int PageNumber = 1,
            [FromQuery] int PageSize = 10,
            [FromQuery] bool includeDeleted = false) =>
        {
            // Use CurrentUserService for permission checking
            var isAdmin = currentUserService.IsAdmin;
            var userId = currentUserService.GetUserIdAsInt();
            
            //  Security: Only admin can see deleted products
            var allowDeleted = isAdmin && includeDeleted;

            var query = new GetAllProductsBySubSubCategoryIdQuery(
                SubSubCategoryId, 
                PageNumber, 
                PageSize, 
                userId, 
                allowDeleted, 
                isAdmin);

            var result = await mediator.Send(query);
            
            if (!result.Succeeded)
                return Results.BadRequest(new { result.Message, result.Errors });

            return Results.Ok(new 
            { 
                result.Message, 
                result.Data,
                Context = new
                {
                    IsAdmin = isAdmin,
                    IncludesDeleted = allowDeleted,
                    UserId = userId,
                    CanManage = currentUserService.CanManageProducts,
                    Timestamp = DateTime.UtcNow
                }
            });
        })
        .WithName("GetProductsBySubSubCategory")
        .WithSummary("Get products by sub-sub category")
        .WithDescription("Get products in a specific sub-sub category. Admin can see deleted products with includeDeleted=true.")
        .Produces<object>(StatusCodes.Status200OK)
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
                [FromQuery] int? CategoryId,
                [FromQuery] int? SubCategoryId,
                [FromQuery] int? SubSubCategoryId,
                [FromBody] UpdateProductDTO udpateProductDto,
                ISender mediator) =>
            {
                var command = new UpdateProductCommand(ProductId,CategoryId,SubCategoryId,SubSubCategoryId, udpateProductDto);
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
