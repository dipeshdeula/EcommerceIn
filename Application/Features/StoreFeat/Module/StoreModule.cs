using Application.Features.StoreFeat.Commands;
using Application.Features.StoreFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.StoreFeat.Module
{
    public class StoreModule : CarterModule
    {
        public StoreModule() : base("")
        {
            WithTags("Store");
            IncludeInOpenApi();

        }

        public override async void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("store");
            app.MapPost("/create", async (CreateStoreCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllStores", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllStoreQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });
        }

    }

}
