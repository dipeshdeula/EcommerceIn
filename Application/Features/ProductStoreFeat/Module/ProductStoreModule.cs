using Application.Dto;
using Application.Features.CategoryFeat.Commands;
using Application.Features.ProductStoreFeat.Commands;
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
            });
        }
    }
}
