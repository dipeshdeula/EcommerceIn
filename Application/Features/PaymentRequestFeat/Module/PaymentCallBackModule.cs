using Application.Dto.PaymentDTOs;
using Application.Interfaces.Services;
using Carter;
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
                    logger.LogInformation("✅ eSewa success callback received: HasData={HasData}", !string.IsNullOrEmpty(data));

                    if (string.IsNullOrEmpty(data))
                    {
                        logger.LogError("❌ No data received in eSewa success callback");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=no_data");
                    }

                    // ✅ Decode Base64 data
                    var decodedBytes = Convert.FromBase64String(data);
                    var decodedJson = Encoding.UTF8.GetString(decodedBytes);

                    logger.LogInformation("📥 Decoded eSewa data: {DecodedData}", decodedJson);

                    var responseData = JsonSerializer.Deserialize<EsewaCallbackResponse>(decodedJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (responseData == null)
                    {
                        logger.LogError("❌ Failed to deserialize eSewa response");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_response");
                    }

                    logger.LogInformation("📥 eSewa Callback: Status={Status}, Amount={Amount}, TransactionUuid={TransactionUuid}",
                        responseData.Status, responseData.TotalAmount, responseData.TransactionUuid);

                    using var scope = serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // ✅ Extract payment request ID from transaction UUID
                    var transactionParts = responseData.TransactionUuid.Split('_');
                    if (transactionParts.Length >= 2 && int.TryParse(transactionParts[1], out var paymentRequestId))
                    {
                        var paymentRequest = await unitOfWork.PaymentRequests.GetByIdAsync(paymentRequestId, cancellationToken: default);
                        if (paymentRequest != null)
                        {
                            // ✅ Update payment status based on eSewa response
                            var isSuccess = responseData.Status?.ToUpper() == "COMPLETE";
                            var oldStatus = paymentRequest.PaymentStatus;

                            paymentRequest.PaymentStatus = isSuccess ? "Succeeded" : "Failed";
                            paymentRequest.UpdatedAt = DateTime.UtcNow;

                            await unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, default);

                            // ✅ Update order status if payment successful
                            if (isSuccess)
                            {
                                var order = await unitOfWork.Orders.GetByIdAsync(paymentRequest.OrderId, default);
                                if (order != null)
                                {
                                    order.Status = "Paid";
                                    order.UpdatedAt = DateTime.UtcNow;
                                    await unitOfWork.Orders.UpdateAsync(order, default);
                                }
                            }

                            await unitOfWork.SaveChangesAsync(default);

                            logger.LogInformation("✅ Payment status updated: PaymentRequestId={PaymentRequestId}, OldStatus={OldStatus}, NewStatus={NewStatus}, OrderId={OrderId}",
                                paymentRequestId, oldStatus, paymentRequest.PaymentStatus, paymentRequest.OrderId);

                            // ✅ Redirect to frontend with success
                            var redirectUrl = isSuccess
                                ? $"http://localhost:5173/payment/success?paymentId={paymentRequestId}&transactionId={responseData.TransactionUuid}&amount={responseData.TotalAmount}&status=verified"
                                : $"http://localhost:5173/payment/failure?paymentId={paymentRequestId}&reason=payment_failed&transactionCode={responseData.TransactionCode}";

                            return Results.Redirect(redirectUrl);
                        }
                        else
                        {
                            logger.LogError("❌ Payment request not found: PaymentRequestId={PaymentRequestId}", paymentRequestId);
                            return Results.Redirect("http://localhost:5173/payment/failure?error=payment_not_found");
                        }
                    }
                    else
                    {
                        logger.LogError("❌ Invalid transaction UUID format: {TransactionUuid}", responseData.TransactionUuid);
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_transaction_format");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error processing eSewa success callback");
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
                logger.LogWarning("❌ eSewa failure callback received");

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

                        logger.LogInformation("📥 eSewa Failure Data: TransactionId={TransactionId}, TransactionCode={TransactionCode}",
                            transactionId, transactionCode);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ Error decoding failure data");
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
                    logger.LogInformation("✅ Khalti success callback received: Pidx={Pidx}, Status={Status}", pidx, status);

                    if (string.IsNullOrEmpty(pidx))
                    {
                        logger.LogError("❌ No pidx received in Khalti success callback");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=no_pidx");
                    }

                    using var scope = serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var paymentGatewayService = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();

                    // ✅ Find payment request by Khalti Pidx
                    var paymentRequest = await unitOfWork.PaymentRequests.GetAsync(
                        predicate: pr => pr.KhaltiPidx == pidx,
                        cancellationToken: default);

                    if (paymentRequest == null)
                    {
                        logger.LogError("❌ Payment request not found for Pidx: {Pidx}", pidx);
                        return Results.Redirect("http://localhost:5173/payment/failure?error=payment_not_found");
                    }

                    // ✅ Verify payment with Khalti
                    var verificationRequest = new PaymentVerificationRequest
                    {
                        PaymentRequestId = paymentRequest.Id,
                        KhaltiPidx = pidx,
                        Status = status?.ToUpper() == "COMPLETED" ? "SUCCESS" : "FAILED",
                        CollectedAmount = amount,
                        AdditionalData = new Dictionary<string, string>
                        {
                            ["transactionId"] = transaction_id ?? "",
                            ["purchaseOrderId"] = purchase_order_id ?? "",
                            ["callbackType"] = "success"
                        }
                    };

                    logger.LogInformation("🔍 Verifying Khalti payment: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);

                    var verificationResult = await paymentGatewayService.VerifyPaymentAsync(verificationRequest, default);

                    if (verificationResult.Succeeded && verificationResult.Data.IsSuccessful)
                    {
                        logger.LogInformation(" Khalti payment verification successful: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);

                        var successUrl = $"http://localhost:5173/payment/success?paymentId={paymentRequest.Id}&transactionId={pidx}&amount={amount}&status=verified&provider=khalti";
                        return Results.Redirect(successUrl);
                    }
                    else
                    {
                        logger.LogError("❌ Khalti payment verification failed: {Error}", verificationResult.Message);

                        var failureUrl = $"http://localhost:5173/payment/failure?paymentId={paymentRequest.Id}&reason=verification_failed&provider=khalti&error={Uri.EscapeDataString(verificationResult.Message ?? "")}";
                        return Results.Redirect(failureUrl);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error processing Khalti success callback");
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
