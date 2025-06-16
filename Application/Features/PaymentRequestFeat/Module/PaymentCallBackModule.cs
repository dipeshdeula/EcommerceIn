using Application.Interfaces.Services;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Features.PaymentRequestFeat.Modules
{
    public class PaymentCallbackModule : CarterModule
    {
        public PaymentCallbackModule() : base("/payment/callbacktest")
        {
            WithTags("Payment call back");
            IncludeInOpenApi();
        }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/payment/callback")
                .WithTags("Payment Callbacks");
           

            // ✅ eSewa Success Callback
            group.MapGet("/esewa/success", async (
                string? data,
                IServiceProvider serviceProvider,
                ILogger<PaymentCallbackModule> logger) =>
            {
                try
                {
                    logger.LogInformation("✅ eSewa success callback received");

                    if (string.IsNullOrEmpty(data))
                    {
                        logger.LogError("❌ No data received in eSewa success callback");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=no_data");
                    }

                    // ✅ Decode Base64 data
                    var decodedBytes = Convert.FromBase64String(data);
                    var decodedJson = Encoding.UTF8.GetString(decodedBytes);
                    var responseData = JsonSerializer.Deserialize<EsewaCallbackResponse>(decodedJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (responseData == null)
                    {
                        logger.LogError("❌ Failed to deserialize eSewa response");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_response");
                    }

                    logger.LogInformation("📥 eSewa Success: Status={Status}, Amount={Amount}, TransactionUuid={TransactionUuid}",
                        responseData.Status, responseData.TotalAmount, responseData.TransactionUuid);

                    using var scope = serviceProvider.CreateScope();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // ✅ Verify signature
                    var secretKey = configuration["PaymentGateways:Esewa:SecretKey"];
                    if (!VerifyEsewaSignature(responseData, secretKey))
                    {
                        logger.LogError("❌ eSewa signature verification failed");
                        return Results.Redirect("http://localhost:5173/payment/failure?error=signature_failed");
                    }

                    // ✅ Extract payment request ID from transaction UUID
                    var transactionParts = responseData.TransactionUuid.Split('_');
                    if (transactionParts.Length >= 2 && int.TryParse(transactionParts[1], out var paymentRequestId))
                    {
                        // ✅ Update payment status in database
                        var paymentRequest = await unitOfWork.PaymentRequests.GetByIdAsync(paymentRequestId);
                        if (paymentRequest != null)
                        {
                            paymentRequest.PaymentStatus = responseData.Status?.ToUpper() == "COMPLETE" ? "Succeeded" : "Failed";
                            paymentRequest.UpdatedAt = DateTime.UtcNow;

                            await unitOfWork.PaymentRequests.UpdateAsync(paymentRequest);

                            // ✅ Update order status if payment successful
                            if (paymentRequest.PaymentStatus == "Succeeded")
                            {
                                var order = await unitOfWork.Orders.GetByIdAsync(paymentRequest.OrderId);
                                if (order != null)
                                {
                                    order.Status = "Paid";
                                    order.UpdatedAt = DateTime.UtcNow;
                                    await unitOfWork.Orders.UpdateAsync(order);
                                }
                            }

                            await unitOfWork.SaveChangesAsync();

                            logger.LogInformation("✅ Payment updated successfully: PaymentRequestId={PaymentRequestId}, Status={Status}",
                                paymentRequestId, paymentRequest.PaymentStatus);

                            // ✅ Redirect to frontend success page
                            var redirectUrl = paymentRequest.PaymentStatus == "Succeeded"
                                ? $"http://localhost:5173/payment/success?paymentId={paymentRequestId}&transactionId={responseData.TransactionUuid}&amount={responseData.TotalAmount}"
                                : $"http://localhost:5173/payment/failure?paymentId={paymentRequestId}&reason=payment_failed";

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
                        return Results.Redirect("http://localhost:5173/payment/failure?error=invalid_transaction");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error processing eSewa success callback");
                    return Results.Redirect("http://localhost:5173/payment/failure?error=processing_error");
                }
            })
            .WithName("EsewaSuccessCallback")
            .WithSummary("Handle eSewa payment success callback")
            .Produces(302);

            // ✅ eSewa Failure Callback
            group.MapGet("/esewa/failure", async (
                string? data,
                ILogger<PaymentCallbackModule> logger) =>
            {
                logger.LogWarning("❌ eSewa failure callback received");

                string? transactionId = null;
                if (!string.IsNullOrEmpty(data))
                {
                    try
                    {
                        var decodedBytes = Convert.FromBase64String(data);
                        var decodedJson = Encoding.UTF8.GetString(decodedBytes);
                        var responseData = JsonSerializer.Deserialize<EsewaCallbackResponse>(decodedJson);
                        transactionId = responseData?.TransactionUuid;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ Error decoding failure data");
                    }
                }

                return Results.Redirect($"http://localhost:5173/payment/failure?transactionId={transactionId}&provider=esewa&reason=payment_cancelled");
            })
            .WithName("EsewaFailureCallback")
            .WithSummary("Handle eSewa payment failure callback")
            .Produces(302);
        }

        private static bool VerifyEsewaSignature(EsewaCallbackResponse response, string secretKey)
        {
            try
            {
                var signatureMessage = $"transaction_code={response.TransactionCode},status={response.Status},total_amount={response.TotalAmount},transaction_uuid={response.TransactionUuid},product_code={response.ProductCode},signed_field_names={response.SignedFieldNames}";

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
                var computedSignatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureMessage));
                var computedSignature = Convert.ToBase64String(computedSignatureBytes);

                return string.Equals(computedSignature, response.Signature, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // ✅ eSewa Response DTOs
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
}