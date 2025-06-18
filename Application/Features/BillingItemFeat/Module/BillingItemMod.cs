using Application.Features.AddressFeat.Queries;
using Application.Features.BillingItemFeat.Commands;
using Application.Features.BillingItemFeat.Queries;
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

namespace Application.Features.BillingItemFeat.Module
{
    public class BillingItemMod : CarterModule
    {
        public BillingItemMod() : base("")
        {
            WithTags("Billing");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("Billing");


            app.MapPost("/createBill", async (int UserId, int OrderId, ISender mediator) =>
            {
                var command = new CreateBillingItemCommand(UserId, OrderId);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllBills", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllBillingQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getAllBillItems", async(ISender mediator,int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllBillingItemQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            });

            app.MapGet("/getBillByUserId", async (ISender mediator, int UserId) =>
            {
                var command = new GetBillByUserIdQuery(UserId);
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
