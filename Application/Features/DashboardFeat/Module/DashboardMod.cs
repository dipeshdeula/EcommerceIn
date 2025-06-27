using Application.Features.DashboardFeat.Queries;
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

namespace Application.Features.DashboardFeat.Module
{
    public class DashboardMod : CarterModule
    {
        public DashboardMod() : base()
        {
            WithTags("Dashboard");
            IncludeInOpenApi();
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("");

            app.MapGet("/admin/dashboard", async (ISender mediator) =>
            {
                var command = new GetAdminDashboardQuery();
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });

            })
                /*.RequireAuthorization()*/
            .WithName("GetAdminDashboard")
            .WithSummary("Get admin dashboard statistics");
        }
    }
}
