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
                string? Status,
                int? DeliveryPersonId,
                string? DeliveryNotes,
                decimal? CollectedAmount,
                ISender mediator) =>
            {
                var command = new VerifyPaymentCommand(
                    PaymentRequestId,
                    EsewaTransactionId,
                    KhaltiPidx,
                    Status);

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
                    Status = "COMPLETED",
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
using Application.Features.PaymentRequestFeat.Queries;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

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
            // ✅ Create payment intent
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

            // ✅ FIXED: eSewa success callback with proper verification
            app.MapGet("/callback/esewa/success", async (
                string? oid,
                string? amt,
                string? refId,
                ISender mediator,
                ILogger<PaymentRequestModule> logger) =>
            {
                logger.LogInformation("✅ eSewa success callback: oid={oid}, amt={amt}, refId={refId}", oid, amt, refId);

                if (string.IsNullOrEmpty(oid))
                {
                    logger.LogWarning("❌ Missing transaction ID (oid) in success callback");
                    return Results.Redirect("http://localhost:5173/payment/failure?error=missing_transaction_id");
                }

                try
                {
                    // Extract PaymentRequest ID from transaction ID (TXN_4_638854726371600160)
                    var transactionParts = oid.Split('_');
                    if (transactionParts.Length >= 2 && int.TryParse(transactionParts[1], out var paymentRequestId))
                    {
                        // ✅ Verify payment with eSewa before confirming
                        var verificationRequest = new PaymentVerificationRequest
                        {
                            PaymentRequestId = paymentRequestId,
                            Status = "SUCCESS",
                            EsewaTransactionId = oid,
                            AdditionalData = new Dictionary<string, string>
                            {
                                ["refId"] = refId ?? "",
                                ["amount"] = amt ?? "",
                                ["callbackType"] = "success"
                            }
                        };

                        // Use the enhanced verification command
                        var command = new VerifyPaymentCommand(paymentRequestId, oid, null, "SUCCESS");
                        var result = await mediator.Send(command);

                        if (result.Succeeded)
                        {
                            logger.LogInformation("✅ Payment verification successful for PaymentRequestId={PaymentRequestId}", paymentRequestId);
                            return Results.Redirect($"http://localhost:5173/payment/success?paymentId={paymentRequestId}&transactionId={oid}&refId={refId}");
                        }
                        else
                        {
                            logger.LogError("❌ Payment verification failed: {Error}", result.Message);
                            return Results.Redirect($"http://localhost:5173/payment/failure?error=verification_failed&reason={Uri.EscapeDataString(result.Message ?? "")}");
                        }
                    }
                    else
                    {
                        logger.LogError("❌ Invalid transaction ID format: {TransactionId}", oid);
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_transaction_format");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error processing eSewa success callback");
                    return Results.Redirect("http://localhost:5173/payment/failure?error=processing_error");
                }
            })
            .WithName("EsewaSuccessCallback")
            .WithSummary("Handle eSewa payment success callback");

            // ✅ eSewa failure callback
            app.MapGet("/callback/esewa/failure", async (
                string? oid,
                string? amt,
                ILogger<PaymentRequestModule> logger) =>
            {
                logger.LogWarning("❌ eSewa failure callback: oid={oid}, amt={amt}", oid, amt);

                return Results.Redirect($"http://localhost:5173/payment/failure?transactionId={oid}&provider=esewa&reason=payment_cancelled");
            })
            .WithName("EsewaFailureCallback")
            .WithSummary("Handle eSewa payment failure callback");

            // ✅ Manual payment verification endpoint
            app.MapPost("/verify", async (
                int PaymentRequestId,
                string? EsewaTransactionId,
                string? KhaltiPidx,
                string? Status,
                int? DeliveryPersonId,
                string? DeliveryNotes,
                decimal? CollectedAmount,
                ISender mediator) =>
            {
                var command = new VerifyPaymentCommand(
                    PaymentRequestId,
                    EsewaTransactionId,
                    KhaltiPidx,
                    Status);

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

            // ✅ Get payment requests
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

            // ✅ Get payments by user
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
        }
    }
}