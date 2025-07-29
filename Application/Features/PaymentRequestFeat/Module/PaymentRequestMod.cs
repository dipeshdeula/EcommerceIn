/*using Application.Dto.PaymentDTOs;
using Application.Features.PaymentRequestFeat.Commands;
using Application.Features.PaymentRequestFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PaymentRequestFeat.Module
{
    public class PaymentRequestModule : CarterModule
    {
        public PaymentRequestModule() : base("/payment")
        {
            WithTags("Payment");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            // ✅ FIXED: Using Carter's MapPost correctly
            app.MapPost("/create-payment-intent", async (
                AddPamentRequestDTO addPaymentRequest,
                ISender mediator) =>
            {
                var command = new CreatePaymentRequestCommand(addPaymentRequest);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        result.Message,
                        result.Errors,
                        success = false,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Results.Ok(new
                {
                    result.Message,
                    result.Data,
                    success = true,
                    timestamp = DateTime.UtcNow
                });
            })
            .WithName("CreatePaymentIntent")
            .WithSummary("Create payment intent for order")
            .WithDescription("Initiates payment process for the specified order")
            .Produces<PaymentInitiationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            app.MapGet("/requests", async (
                ISender mediator,
                int pageNumber = 1,
                int pageSize = 10) =>
            {
                var query = new GetAllPaymentQuery(pageNumber, pageSize);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("GetAllPaymentRequests")
            .WithSummary("Get all payment requests")
            .WithDescription("Retrieve paginated list of payment requests")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            app.MapGet("/user/{userId}", async (
                int userId,
                ISender mediator,
                int pageNumber = 1,
                int pageSize = 10) =>
            {
                var query = new GetPaymentByUserIdQuery(userId, pageNumber, pageSize);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("GetPaymentsByUserId")
            .WithSummary("Get payments by user ID")
            .WithDescription("Retrieve paginated list of payments for specific user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            app.MapPost("/verify", async (
                int PaymentRequestId,
                string? EsewaTransactionId,
                string? KhaltiPidx,
                string? PaymentStatus,
                int? DeliveryPersonId,
                string? DeliveryNotes,
                decimal? CollectedAmount,
                ISender mediator) =>
            {
                var command = new VerifyPaymentCommand(
                    PaymentRequestId,
                    EsewaTransactionId,
                    KhaltiPidx,
                    PaymentStatus);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("VerifyPayment")
            .WithSummary("Verify payment status")
            .WithDescription("Verify and update payment status from payment gateway")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // ✅ NEW: COD specific endpoints
            app.MapPost("/cod/collect", async (
                int PaymentRequestId,
                int DeliveryPersonId,
                decimal CollectedAmount,
                string? Notes,
                ISender mediator) =>
            {
                var verificationRequest = new PaymentVerificationRequest
                {
                    PaymentRequestId = PaymentRequestId,
                    PaymentStatus = "COMPLETED",
                    DeliveryPersonId = DeliveryPersonId,
                    CollectedAmount = CollectedAmount,
                    DeliveryNotes = Notes
                };

                // Use existing verification logic for COD
                var command = new VerifyPaymentCommand(PaymentRequestId, null, null, "COMPLETED");
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new
                {
                    message = "COD payment collected successfully",
                    data = result.Data,
                    success = true
                });
            })
            .WithName("CollectCODPayment")
            .WithSummary("Collect COD payment")
            .WithDescription("Mark COD payment as collected by delivery person")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
        }
    }
}*/


using Application.Dto.PaymentDTOs;
using Application.Features.PaymentRequestFeat.Commands;
using Application.Features.PaymentRequestFeat.DeleteCommands;
using Application.Features.PaymentRequestFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Application.Features.PaymentRequestFeat.Module
{
    public class PaymentRequestModule : CarterModule
    {
        public PaymentRequestModule() : base("/payment")
        {
            WithTags("Payment");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-payment-intent", async (
                AddPamentRequestDTO addPaymentRequest,
                ISender mediator) =>
            {
                var command = new CreatePaymentRequestCommand(addPaymentRequest);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        result.Message,
                        result.Errors,
                        success = false,
                        timestamp = DateTime.UtcNow
                    });
                }

                return Results.Ok(new
                {
                    result.Message,
                    result.Data,
                    //success = true,
                    //timestamp = DateTime.UtcNow
                });
            })
            .WithName("CreatePaymentIntent")
            .WithSummary("Create payment intent for order")
            .WithDescription("Initiates payment process for the specified order")
            .Produces<PaymentInitiationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);


           /* app.MapPost("/verify", async (
                int PaymentRequestId,
                string? EsewaTransactionId,
                string? KhaltiPidx,
                string? PaymentStatus,
                int? DeliveryPersonId,
                string? DeliveryNotes,
                decimal? CollectedAmount,
                ISender mediator) =>
            {
                var command = new VerifyPaymentCommand(
                    PaymentRequestId,
                    EsewaTransactionId,
                    KhaltiPidx,
                    PaymentStatus);

                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { 
                        result.Message, result.Errors, success = false,timeStamp = DateTime.UtcNow });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("VerifyPayment")
            .WithSummary("Verify payment status")
            .WithDescription("Verify and update payment status from payment gateway")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);*/

            // Get all payment requests

            app.MapGet("/requests", async (
                ISender mediator,
                int pageNumber = 1,
                int pageSize = 10,
                string? status = null,
                int? PaymentMethodId = null,
                DateTime? FromDate = null,
                DateTime? ToDate = null,
                string? SearchTerm = null,
                string? OrderBy = "CreatedAt"


                ) =>
            {
                var query = new GetAllPaymentQuery(
                    pageNumber, pageSize, status, PaymentMethodId, FromDate, ToDate, SearchTerm, OrderBy);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { 
                    result.Message, result.Data, result.TotalCount,result.TotalPages,result.PageSize,result.HasNextPage,result.HasPreviousPage });
            })
            .WithName("GetAllPaymentRequests")
            .WithSummary("Get all payment requests")
            .WithDescription("Retrieve paginated list of payment requests")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // get payment by userId
            app.MapGet("/user/{userId}", async (
                int userId,
                ISender mediator,
                int pageNumber = 1,
                int pageSize = 10,
                string? Status = null,
                string? OrderBy = "CreatedAt"
                ) =>
            {
                var query = new GetPaymentByUserIdQuery(userId, pageNumber, pageSize, Status,OrderBy);
                var result = await mediator.Send(query);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            })
            .WithName("GetPaymentsByUserId")
            .WithSummary("Get payments by user ID")
            .WithDescription("Retrieve paginated list of payments for specific user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

            // soft delete
            app.MapDelete("/softDeletPaymentRequest", async (int Id, ISender mediator) =>
            {
                var command = new SoftDeletePaymentRequestCommand(Id);
                var result = await mediator.Send(command);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });
                }

                return Results.Ok(new { result.Message, result.Data });
            });

            // unDelete Payment Request
            app.MapDelete("/unDeletePaymentRequest", async (int Id, ISender mediator) =>
            {
                var command = new UnDeletePaymentRequestCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Errors });

                }

                return Results.Ok(new { result.Message, result.Data });
            });

            // Hard Delete Payment Request
            app.MapDelete("/hardDeletePaymentRequest", async (int Id, ISender mediator) =>
            {
                var command = new HardDeletePaymentRequestCommand(Id);
                var result = await mediator.Send(command);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new { result.Message, result.Data });
                }
                return Results.Ok(new { result.Message, result.Data });
            });
        }
    }
}