using Application.Dto.PaymentDTOs;
using Application.Features.PaymentRequestFeat.Commands;
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
            // ✅ COD Payment Collection Endpoint
            app.MapPost("/cod/collect-payment", async (
                int PaymentRequestId,
                int DeliveryPersonId,
                decimal CollectedAmount,
                string DeliveryStatus,
                string? Notes,
                ISender mediator) =>
            {
                var command = new UpdateCODPaymentCommand(
                    PaymentRequestId,
                    DeliveryPersonId,
                    CollectedAmount,
                    DeliveryStatus,
                    Notes);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                    return Results.BadRequest(new { result.Message, result.Errors });

                return Results.Ok(new
                {
                    result.Message,
                    result.Data,
                    Success = true,
                    Timestamp = DateTime.UtcNow
                });
            })
            .WithName("CollectCODPaymentDelivery")
            .WithSummary("Update COD payment status after delivery")
            .WithDescription("Called by delivery personnel to update payment status when cash is collected")
            .Produces<PaymentVerificationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // ✅ Get COD Deliveries for a delivery person
            app.MapGet("/cod/pending-deliveries/{deliveryPersonId}", async (
                int deliveryPersonId,
                ISender mediator) =>
            {
                // Implementation to get pending COD deliveries
                // You'll need to create this query
                return Results.Ok(new { message = "Pending COD deliveries endpoint" });
            })
            .WithName("GetPendingCODDeliveries")
            .WithSummary("Get pending COD deliveries for a delivery person");
        }
    }
}
