using Application.Interfaces.Services;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.TrackEventsFeat
{
    public class TrackModule : CarterModule
    {
        public TrackModule() : base("")
        {
            WithTags("events");
            IncludeInOpenApi();
            
            
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app = app.MapGroup("events");

            app.MapGet("/event-usage-check/{userId:int}/{eventId:int}", async (
            int userId,
            int eventId,
            int productId,
            [FromServices] IEventUsageService eventUsageService) =>
            {
                var canUse = await eventUsageService.CanUserUseEventAsync(eventId, userId);
                var usageCount = await eventUsageService.GetUserEventUsageCountAsync(eventId, userId,productId);
                var hasReachedLimit = await eventUsageService.HasUserReachedEventLimitAsync(eventId, userId);

                return Results.Ok(new
                {
                    Message = canUse ? "User can use this event" : "User cannot use this event",
                    UserId = userId,
                    EventId = eventId,
                    CanUse = canUse,
                    CurrentUsage = usageCount,
                    HasReachedLimit = hasReachedLimit,
                    Status = canUse ? " Available" : " Limit Reached"
                });
            })
            .WithName("CheckEventUsageLimit")
            .WithTags("Products", "Events")
            .WithSummary("Check if user can use a specific event")
            .Produces(StatusCodes.Status200OK);

        }
    }
}
