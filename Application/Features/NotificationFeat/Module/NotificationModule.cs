using Application.Features.NotificationFeat.Command;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.NotificationFeat.Module;

public class NotificationModule : CarterModule
{
    public NotificationModule() : base("")
    {
        WithTags("NotificationTest");
        IncludeInOpenApi();

    }

    public override async void AddRoutes(IEndpointRouteBuilder app)
    {
        app = app.MapGroup("notif");


        app.MapPost("/send-to-user", async (ISender mediator, SendNotificationToUser notif) =>
        {
            return mediator.Send(notif);
        });



    }
}
