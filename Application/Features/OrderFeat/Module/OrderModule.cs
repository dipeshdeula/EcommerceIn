using Application.Features.CategoryFeat.Queries;
using Application.Features.OrderFeat.Commands;
using Application.Features.OrderFeat.Queries;
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

            app.MapGet("/getAllOrder", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var result = await mediator.Send(new GetAllOrderQuery(PageNumber, PageSize));
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });

            });
        }
    }
}
