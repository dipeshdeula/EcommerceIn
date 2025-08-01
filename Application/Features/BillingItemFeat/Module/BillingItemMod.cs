using Application.Features.AddressFeat.Queries;
using Application.Features.BillingItemFeat.Commands;
using Application.Features.BillingItemFeat.DeleteCommands;
using Application.Features.BillingItemFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Forms;
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
            RequireAuthorization();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("Billing");


            app.MapPost("/createBill", async (
                ISender mediator,
                int UserId,
                int OrderId,
                int CompanyId
                ) =>
            {
                var command = new CreateBillingItemCommand(UserId, OrderId, CompanyId);
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
            }).RequireAuthorization("RequireAdminOrVendor");

            app.MapGet("/getAllBillItems", async (ISender mediator, int PageNumber = 1, int PageSize = 10) =>
            {
                var command = new GetAllBillingItemQuery(PageNumber, PageSize);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }
                return Results.Ok(new { result.Message, result.Data });
            }).RequireAuthorization("RequireAdminOrVendor");

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

            app.MapDelete("/softDeleteBill", async (int Id, ISender mediator) =>
            {
                var command = new SoftDeleteBillingItemCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapDelete("/unDeleteBill", async (int Id, ISender mediator) =>
            {
                var command = new UnDeleteBilingItemCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });

            });

            app.MapDelete("/hardDeleteBill", async (int Id, ISender mediator) =>
            {
                var command = new HardDeleteBillItemComand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });

            });



        }
    }
}
