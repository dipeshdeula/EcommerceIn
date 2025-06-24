using Application.Dto.PaymentDTOs;
using Application.Features.PaymentRequestFeat.Commands;
using Application.Interfaces.Services;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Features.PaymentRequestFeat.Modules
{
    public class PaymentCallbackModule : CarterModule
    {
        public PaymentCallbackModule() : base("/payment/callback")
        {
            WithTags("Payment Callbacks");
            IncludeInOpenApi();
        }

        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("");


            group.MapGet("/esewa/success", async (
                string? data,
                IServiceProvider serviceProvider,
                ILogger<PaymentCallbackModule> logger) =>
            {
                try
                {
                    logger.LogInformation("eSewa success callback received: HasData={HasData}", !string.IsNullOrEmpty(data));

                    if (string.IsNullOrEmpty(data))
                    {
                        logger.LogError("No data received in eSewa success callback");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=no_data");
                    }

                    // Decode Base64 data
                    var decodedBytes = Convert.FromBase64String(data);
                    var decodedJson = Encoding.UTF8.GetString(decodedBytes);

                    logger.LogInformation("Decoded eSewa data: {DecodedData}", decodedJson);

                    var responseData = JsonSerializer.Deserialize<EsewaCallbackResponse>(decodedJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (responseData == null)
                    {
                        logger.LogError("Failed to deserialize eSewa response");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_response");
                    }

                    logger.LogInformation("eSewa Callback: Status={Status}, Amount={Amount}, TransactionUuid={TransactionUuid}",
                        responseData.Status, responseData.TotalAmount, responseData.TransactionUuid);

                    using var scope = serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // Extract payment request ID from transaction UUID
                    var transactionParts = responseData.TransactionUuid.Split('_');
                    if (transactionParts.Length >= 2 && int.TryParse(transactionParts[1], out var paymentRequestId))
                    {
                        var paymentRequest = await unitOfWork.PaymentRequests.GetByIdAsync(paymentRequestId, cancellationToken: default);
                        if (paymentRequest != null)
                        {
                            // Update payment status based on eSewa response
                            var isSuccess = responseData.Status?.ToUpper() == "COMPLETE";
                            var oldStatus = paymentRequest.PaymentStatus;

                            paymentRequest.PaymentStatus = isSuccess ? "Succeeded" : "Failed";
                            paymentRequest.UpdatedAt = DateTime.UtcNow;

                            await unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, default);

                            // Update order status if payment successful
                            if (isSuccess)
                            {
                                var order = await unitOfWork.Orders.GetByIdAsync(paymentRequest.OrderId, default);
                                if (order != null)
                                {
                                    order.PaymentStatus = "Paid";
                                    order.UpdatedAt = DateTime.UtcNow;
                                    await unitOfWork.Orders.UpdateAsync(order, default);
                                }
                            }

                            await unitOfWork.SaveChangesAsync(default);

                            logger.LogInformation("Payment status updated: PaymentRequestId={PaymentRequestId}, OldStatus={OldStatus}, NewStatus={NewStatus}, OrderId={OrderId}",
                                paymentRequestId, oldStatus, paymentRequest.PaymentStatus, paymentRequest.OrderId);

                            //  Redirect to frontend with success
                            var redirectUrl = isSuccess
                                ? $"http://localhost:5173/payment/success?paymentId={paymentRequestId}&transactionId={responseData.TransactionUuid}&amount={responseData.TotalAmount}&status=verified"
                                : $"http://localhost:5173/payment/failure?paymentId={paymentRequestId}&reason=payment_failed&transactionCode={responseData.TransactionCode}";

                            return Results.Redirect(redirectUrl);
                        }
                        else
                        {
                            logger.LogError("Payment request not found: PaymentRequestId={PaymentRequestId}", paymentRequestId);
                            return Results.Redirect("http://localhost:5173/payment/failure?error=payment_not_found");
                        }
                    }
                    else
                    {
                        logger.LogError("Invalid transaction UUID format: {TransactionUuid}", responseData.TransactionUuid);
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_transaction_format");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing eSewa success callback");
                    return Results.Redirect("http://localhost:5173/payment/failure?error=callback_processing_error");
                }
            })
            .WithName("EsewaSuccessCallback")
            .WithSummary("Handle eSewa payment success callback")
            .Produces(302);


            group.MapGet("/esewa/failure", async (
                string? data,
                ILogger<PaymentCallbackModule> logger) =>
            {
                logger.LogWarning("eSewa failure callback received");

                string? transactionId = null;
                string? transactionCode = null;

                if (!string.IsNullOrEmpty(data))
                {
                    try
                    {
                        var decodedBytes = Convert.FromBase64String(data);
                        var decodedJson = Encoding.UTF8.GetString(decodedBytes);
                        var responseData = JsonSerializer.Deserialize<EsewaCallbackResponse>(decodedJson);
                        transactionId = responseData?.TransactionUuid;
                        transactionCode = responseData?.TransactionCode;

                        logger.LogInformation("eSewa Failure Data: TransactionId={TransactionId}, TransactionCode={TransactionCode}",
                            transactionId, transactionCode);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error decoding failure data");
                    }
                }

                var failureUrl = $"http://localhost:5173/payment/failure?transactionId={transactionId}&transactionCode={transactionCode}&provider=esewa&reason=payment_cancelled";
                return Results.Redirect(failureUrl);
            })
            .WithName("EsewaFailureCallback")
            .WithSummary("Handle eSewa payment failure callback")
            .Produces(302);

            // khalti success callback
            group.MapGet("/khalti/success", async (
                string? pidx,
                string? status,
                decimal? amount,
                string? purchase_order_id,
                string? transaction_id,
                IServiceProvider serviceProvider,
                ILogger<PaymentCallbackModule> logger) =>
            {
                try
                {
                    logger.LogInformation("Khalti success callback received: Pidx={Pidx}, Status={Status}", pidx, status);

                    if (string.IsNullOrEmpty(pidx))
                    {
                        logger.LogError("No pidx received in Khalti success callback");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=no_pidx");
                    }

                    using var scope = serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var paymentGatewayService = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();

                    // Find payment request by Khalti Pidx
                    var paymentRequest = await unitOfWork.PaymentRequests.GetAsync(
                        predicate: pr => pr.KhaltiPidx == pidx,
                        cancellationToken: default);

                    if (paymentRequest == null)
                    {
                        logger.LogError("Payment request not found for Pidx: {Pidx}", pidx);
                        return Results.Redirect("http://localhost:5173/payment/failure?error=payment_not_found");
                    }

                    // Verify payment with Khalti
                    var verificationRequest = new PaymentVerificationRequest
                    {
                        PaymentRequestId = paymentRequest.Id,
                        KhaltiPidx = pidx,
                        PaymentStatus = status?.ToUpper() == "COMPLETED" ? "SUCCESS" : "FAILED",
                        CollectedAmount = amount,
                        AdditionalData = new Dictionary<string, string>
                        {
                            ["transactionId"] = transaction_id ?? "",
                            ["purchaseOrderId"] = purchase_order_id ?? "",
                            ["callbackType"] = "success"
                        }
                    };

                    logger.LogInformation("Verifying Khalti payment: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);

                    var verificationResult = await paymentGatewayService.VerifyPaymentAsync(verificationRequest, default);

                    if (verificationResult.Succeeded && verificationResult.Data.IsSuccessful)
                    {
                        logger.LogInformation(" Khalti payment verification successful: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);

                        var successUrl = $"http://localhost:5173/payment/success?paymentId={paymentRequest.Id}&transactionId={pidx}&amount={amount}&status=verified&provider=khalti";
                        return Results.Redirect(successUrl);
                    }
                    else
                    {
                        logger.LogError("Khalti payment verification failed: {Error}", verificationResult.Message);

                        var failureUrl = $"http://localhost:5173/payment/failure?paymentId={paymentRequest.Id}&reason=verification_failed&provider=khalti&error={Uri.EscapeDataString(verificationResult.Message ?? "")}";
                        return Results.Redirect(failureUrl);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing Khalti success callback");
                    return Results.Redirect("http://localhost:5173/payment/failure?error=callback_processing_error&provider=khalti");
                }
            })
            .WithName("KhaltiSuccessCallback")
            .WithSummary("Handle Khalti payment success callback")
            .Produces(302);

            // Khalti Failure Callback
            group.MapGet("/khalti/failure", async (
                string? pidx,
                string? status,
                string? message,
                ILogger<PaymentCallbackModule> logger) =>
            {
                logger.LogWarning("❌ Khalti failure callback received: Pidx={Pidx}, Status={Status}, Message={Message}",
                    pidx, status, message);

                var failureUrl = $"http://localhost:5173/payment/failure?pidx={pidx}&provider=khalti&reason=payment_cancelled_or_failed&message={Uri.EscapeDataString(message ?? "")}";
                return Results.Redirect(failureUrl);
            })
            .WithName("KhaltiFailureCallback")
            .WithSummary("Handle Khalti payment failure callback")
            .Produces(302);

            group.MapPost("/khalti/webhook", async (
             HttpRequest request,
             IServiceProvider serviceProvider,
             ILogger<PaymentCallbackModule> logger) =>
                 {
                     try
                     {
                         // Read payload
                         string payload;
                         using (var reader = new StreamReader(request.Body))
                         {
                             payload = await reader.ReadToEndAsync();
                         }

                         if (string.IsNullOrWhiteSpace(payload))
                         {
                             logger.LogError("No payload received in Khalti webhook");
                             return Results.BadRequest(new { message = "Empty payload" });
                         }


                         // Optionally, verify signature/header if Khalti provides one

                         // Parse payload (assume JSON with pidx)
                         var json = JsonDocument.Parse(payload);
                         var pidx = json.RootElement.GetProperty("pidx").GetString();

                         if (string.IsNullOrEmpty(pidx))
                         {
                             logger.LogError("No pidx in Khalti webhook");
                             return Results.BadRequest(new { message = "Missing pidx" });
                         }

                         using var scope = serviceProvider.CreateScope();
                         var paymentGatewayService = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();
                         var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                         // Find payment request by pidx
                         var paymentRequest = await unitOfWork.PaymentRequests.GetAsync(pr => pr.KhaltiPidx == pidx, cancellationToken: default);
                         if (paymentRequest == null)
                         {
                             logger.LogError("Payment request not found for webhook pidx: {Pidx}", pidx);
                             return Results.NotFound(new { message = "Payment request not found" });
                         }

                         // Always verify with Khalti lookup
                         var verificationRequest = new PaymentVerificationRequest
                         {
                             PaymentRequestId = paymentRequest.Id,
                             KhaltiPidx = pidx
                         };

                         var verificationResult = await paymentGatewayService.VerifyPaymentAsync(verificationRequest, default);

                         if (verificationResult.Succeeded && verificationResult.Data.IsSuccessful)
                         {
                             logger.LogInformation("Khalti webhook: payment verified and updated for PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                             return Results.Ok(new { message = "Payment verified and updated" });
                         }
                         else
                         {
                             logger.LogWarning("Khalti webhook: payment not completed for PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                             return Results.Ok(new { message = "Payment not completed", status = verificationResult.Data?.Status });
                         }
                     }
                     catch (Exception ex)
                     {
                         logger.LogError(ex, "Error processing Khalti webhook");
                         return Results.StatusCode(500);
                     }
                 })
                 .WithName("KhaltiWebhook")
                 .WithSummary("Handle Khalti payment webhook")
                 .Produces(200)
                 .Produces(400)
                 .Produces(404)
                 .Produces(500);

            group.MapPost("/cod/collect-payment", async (
                     int PaymentRequestId,                    
                     string DeliveryStatus, // "Delivered", "PartialPayment", "PaymentRefused"
                     string? Notes,
                     ICurrentUserService currentUserService,
                     ISender mediator
                     ) =>
                 {
                     // Ensure only delivery personnel can access
                     if (currentUserService.Role?.ToLower() != "deliveryboy")
                         return Results.Unauthorized();

                     var deliveryPersonId = int.TryParse(currentUserService.UserId, out var id) ? id : 0;
                     if (deliveryPersonId == 0)
                         return Results.BadRequest(new { message = "Invalid delivery person" });

                     var command = new UpdateCODPaymentCommand(
                         PaymentRequestId,
                         deliveryPersonId,                        
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
                 .RequireAuthorization("RequireDeliveryBoy")
                 .WithName("CollectCODPaymentDelivery")
                 .WithSummary("Update COD payment status after delivery");
        }
    }
}


public class EsewaCallbackResponse
{
    [JsonPropertyName("transaction_code")]
    public string TransactionCode { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("total_amount")]
    public string TotalAmount { get; set; } = string.Empty;

    [JsonPropertyName("transaction_uuid")]
    public string TransactionUuid { get; set; } = string.Empty;

    [JsonPropertyName("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("signed_field_names")]
    public string SignedFieldNames { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}
