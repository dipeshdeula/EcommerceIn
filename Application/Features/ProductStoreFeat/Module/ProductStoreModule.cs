using Application.Dto;
using Application.Features.CategoryFeat.Commands;
using Application.Features.ProductStoreFeat.Commands;
using Application.Features.ProductStoreFeat.Queries;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.ProductStoreFeat.Module
{
    public class ProductStoreModule : CarterModule
    {
        public ProductStoreModule() : base("")
        {
            WithTags("ProductStore");
            IncludeInOpenApi();
            RequireAuthorization();

        }
        public override async void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("ProductStore");

            app.MapPost("/create", async (ISender mediator, CreateProductStoreCommand command) =>
            {

                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapGet("/getAllProductStore", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllProductStoreQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllProductByStoreId", async (
                ISender mediator, int StoreId, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllProductByStoreIdQuery(StoreId, PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });
                return Results.Ok(new { result.Message, result.Data });

            });
            
            app.MapGet("/getTransactionFromStoreId", async (
            [FromQuery] int storeId,
            [FromServices] ISender mediator,
            [FromServices] ICurrentUserService currentUserService,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? orderStatus = null,
            [FromQuery] bool includeDeleted = false) =>
        {
            // Use CurrentUserService for permission checking
            var isAdmin = currentUserService.IsAdmin;
            var canManage = currentUserService.CanManageProducts;
            
            // Security: Only admin/vendor can see detailed transactions
            if (!isAdmin && !canManage)
            {
                return Results.Forbid();
            }
            
            // Security: Only admin can see deleted records
            var allowDeleted = isAdmin && includeDeleted;

            var query = new GetStoreTransactionsQuery(
                StoreId: storeId,
                FromDate: fromDate,
                ToDate: toDate,
                PageNumber: pageNumber,
                PageSize: pageSize,
                OrderStatus: orderStatus,
                IncludeDeleted: allowDeleted,
                IsAdminRequest: isAdmin
            );

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
                    CanManage = canManage,
                    IncludesDeleted = allowDeleted,
                    UserId = currentUserService.GetUserIdAsInt(),
                    Timestamp = DateTime.UtcNow,
                    FilterApplied = new
                    {
                        FromDate = fromDate,
                        ToDate = toDate,
                        OrderStatus = orderStatus,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    }
                }
            });
        })
        .RequireAuthorization("RequireAdminOrVendor") 
        .WithName("GetStoreTransactionDetails")
        .WithSummary("Get detailed transaction history for a specific store")
        .WithDescription("Retrieve comprehensive transaction details including products sold, quantities, prices, and revenue for a specific store within a date range.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .WithTags("ProductStore", "Transactions");
        }
    }
}
