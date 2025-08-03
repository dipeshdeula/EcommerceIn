using Application.Features.PaymentRequestFeat.Commands;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PaymentRequestFeat.Module
{
    public class DeliveryModule : CarterModule
    {
        public DeliveryModule() : base("delivery")
        {
            WithTags("Delivery Management");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            // Managing order status while payment made through online

            app.MapPut("/Delivered", async (
                ISender mediator,
                ICurrentUserService currentUserService,
                int PaymentRequestId,
                bool IsDelivered = false
                ) =>
            {
                
                var deliveryPersonId = int.TryParse(currentUserService.UserId, out var id) ? id : 0;
                if (deliveryPersonId == 0)
                    return Results.BadRequest(new { message = "Invalid delivery person" });

                var command = new UpdateDeliveryStatusCommand(PaymentRequestId, IsDelivered);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new { result.Message, result.Data });



            })  .RequireAuthorization("RequireAdminOrDeliveryBoy")
                .WithName("Update Delivery Status")
                .WithSummary("Update OrderStatus after online payment");

        }
    }
}
