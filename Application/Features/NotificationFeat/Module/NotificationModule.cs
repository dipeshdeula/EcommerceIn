using Application.Features.NotificationFeat.Commands;
using Application.Features.NotificationFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.NotificationFeat.Module;

public class NotificationModule : CarterModule
{
    public NotificationModule() : base("")
    {
        WithTags("NotificationTest");
        IncludeInOpenApi();

    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app = app.MapGroup("notif");


        app.MapPost("/send-to-user", async (ISender mediator, SendNotificationToUser notif) =>
        {
            return await mediator.Send(notif);
        });
        app.MapPost("/acknowledge-notification", async (ISender mediator, int notificationId) =>
        {
            return await mediator.Send(new AcknowledgeNotificationCommand(notificationId));
        });
        app.MapGet("/getAllNotification", (
            [FromServices] ISender mediator,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string status = null) =>
        {
            return mediator.Send(new GetAllNotificationsQuery(pageNumber, pageSize, status));
        }).RequireAuthorization("RequireAdmin");

        app.MapGet("/getAllNotificationsByUserId", async (ISender mediator, int userId, int pageNumber = 1, int pageSize = 10) =>
        {
            return await mediator.Send(new GetAllNotificationsByUserIdQuery(userId, pageNumber, pageSize));
        });
        app.MapPost("/mark-as-read", async (ISender mediator,  ICollection<int> notificationIds) =>
        {
            return await mediator.Send(new MarkNotificationsAsReadCommand(notificationIds));
        }).RequireAuthorization();



    }
}
