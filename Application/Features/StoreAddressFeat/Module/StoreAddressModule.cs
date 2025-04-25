using Application.Features.StoreAddressFeat.Commands;
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

namespace Application.Features.StoreAddressFeat.Module
{
    public class StoreAddressModule : CarterModule
    {
        public StoreAddressModule() : base("")
        {
            WithTags("StoreAddress");
            IncludeInOpenApi();
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("StoreAddress");
            app.MapPost("/add", async ([FromServices] ISender mediator, [FromQuery] int storeId, [FromBody] CreateStoreAddressCommand command) =>
            {
                var result = await mediator.Send(command with { StoreId = storeId });
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

        }
    }
    
}
