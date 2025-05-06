using Application.Features.OrderFeat.Commands;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.OrderFeat.Module
{
    public class OrderModule : CarterModule
    {
        public OrderModule() : base("")
        {
            WithTags("Order");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("Order");

            app.MapPost("/place-order", async (CreatePlaceOrderCommand command, ISender mediator) =>
            {
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { message = result.Message });
                }
                return Results.Ok(result.Data);
            });

        }
    }
}
