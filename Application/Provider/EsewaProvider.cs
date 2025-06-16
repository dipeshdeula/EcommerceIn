using Application.Common;
using Application.Dto.PaymentDTOs.EsewaDTOs;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Provider
{
    public class EsewaProvider : IPaymentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EsewaProvider> _logger;
        private readonly EsewaConfig _config;

        public string ProviderName => "eSewa";

        public EsewaProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EsewaProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _config = new EsewaConfig(configuration);
        }

        public async Task<Result<PaymentInitiationResponse>> InitiateAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ Generate transaction ID (same as your working approach)
                var transactionUuid = $"ESW_{paymentRequest.Id}_{DateTime.UtcNow.Ticks}";

                _logger.LogInformation("🚀 Initiating eSewa payment: PaymentRequestId={PaymentRequestId}, TransactionId={TransactionId}, Amount={Amount}",
                    paymentRequest.Id, transactionUuid, paymentRequest.PaymentAmount);

                // ✅ Generate signature (using your working approach)
                var totalAmount = paymentRequest.PaymentAmount.ToString("F2");
                var signedFieldNames = "total_amount,transaction_uuid,product_code";
                var signatureString = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={_config.MerchantId}";

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.SecretKey));
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                var signature = Convert.ToBase64String(signatureBytes);

                // ✅ Prepare form data (exactly as your working approach)
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("amount", paymentRequest.PaymentAmount.ToString("F2")),
                    new("tax_amount", "0"),
                    new("total_amount", totalAmount),
                    new("transaction_uuid", transactionUuid),
                    new("product_code", _config.MerchantId),
                    new("product_service_charge", "0"),
                    new("product_delivery_charge", "0"),
                    new("success_url", _config.SuccessUrl),
                    new("failure_url", _config.FailureUrl),
                    new("signed_field_names", signedFieldNames),
                    new("signature", signature)
                };

                // ✅ Submit form to eSewa (your working approach)
                using var client = _httpClientFactory.CreateClient();
                var content = new FormUrlEncodedContent(formData);

                _logger.LogDebug("📤 Submitting eSewa form: {FormData}",
                    string.Join(", ", formData.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                // ✅ POST to /form endpoint (critical - this is what works!)
                var response = await client.PostAsync($"{_config.BaseUrl}/api/epay/main/v2/form", content, cancellationToken);

                _logger.LogInformation("📥 eSewa form response: StatusCode={StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ eSewa form submission failed: StatusCode={StatusCode}, Response={Response}",
                        response.StatusCode, errorContent);
                    return Result<PaymentInitiationResponse>.Failure($"eSewa form submission failed: {response.StatusCode}");
                }

                // ✅ Extract redirect URL (your working approach)
                var redirectUrl = response.RequestMessage?.RequestUri?.ToString();

                if (string.IsNullOrEmpty(redirectUrl))
                {
                    _logger.LogError("❌ eSewa failed to return a valid redirect URL");
                    return Result<PaymentInitiationResponse>.Failure("eSewa failed to return a valid payment URL");
                }

                _logger.LogInformation("✅ eSewa payment URL generated successfully: TransactionId={TransactionId}, PaymentUrl={PaymentUrl}",
                    transactionUuid, redirectUrl);

                return Result<PaymentInitiationResponse>.Success(new PaymentInitiationResponse
                {
                    Provider = ProviderName,
                    PaymentUrl = redirectUrl,  // ✅ Real eSewa-generated URL
                    PaymentFormHtml = null,    // ✅ Not needed since we have URL
                    ProviderTransactionId = transactionUuid,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    Status = "Initiated",
                    RequiresRedirect = true,
                    Instructions = "You will be redirected to eSewa for payment. Please complete within 15 minutes.",
                    Metadata = new Dictionary<string, string>
                    {
                        ["signature"] = signature,
                        ["merchantCode"] = _config.MerchantId,
                        ["environment"] = "test",
                        ["transactionId"] = transactionUuid
                    }
                }, "eSewa payment initiated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ eSewa payment initiation failed: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure($"eSewa initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔍 Verifying eSewa payment: TransactionId={TransactionId}", request.EsewaTransactionId);

                using var client = _httpClientFactory.CreateClient();

                var verificationPayload = new
                {
                    product_code = _config.MerchantId,
                    total_amount = request.CollectedAmount?.ToString("F2") ?? "0.00",
                    transaction_uuid = request.EsewaTransactionId
                };

                var jsonContent = JsonSerializer.Serialize(verificationPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(_config.VerifyUrl, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("📥 eSewa verification response: StatusCode={StatusCode}, Response={Response}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var verificationResult = JsonSerializer.Deserialize<EsewaVerificationApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var isSuccessful = verificationResult?.Status?.ToUpper() == "COMPLETE";

                    _logger.LogInformation("✅ eSewa verification completed: IsSuccessful={IsSuccessful}, Status={Status}",
                        isSuccessful, verificationResult?.Status);

                    return Result<PaymentVerificationResponse>.Success(new PaymentVerificationResponse
                    {
                        IsSuccessful = isSuccessful,
                        Status = isSuccessful ? "Succeeded" : "Failed",
                        Message = isSuccessful ? "Payment verified successfully" : "Payment verification failed",
                        Provider = ProviderName,
                        TransactionId = request.EsewaTransactionId ?? "",
                        CollectedAmount = request.CollectedAmount,
                        CollectedAt = isSuccessful ? DateTime.UtcNow : null,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["referenceId"] = verificationResult?.ReferenceId ?? "",
                            ["apiResponse"] = verificationResult ?? new object()
                        }
                    }, "eSewa verification completed");
                }

                _logger.LogWarning("❌ eSewa verification failed: StatusCode={StatusCode}", response.StatusCode);
                return Result<PaymentVerificationResponse>.Failure($"eSewa verification failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ eSewa verification error: TransactionId={TransactionId}", request.EsewaTransactionId);
                return Result<PaymentVerificationResponse>.Failure($"eSewa verification error: {ex.Message}");
            }
        }

        public async Task<Result<PaymentStatusResponse>> GetStatusAsync(string transactionId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📊 Getting eSewa payment status: TransactionId={TransactionId}", transactionId);

                // ✅ For now, return a basic implementation
                // You can enhance this later with actual eSewa status API calls
                return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse
                {
                    Status = "Pending",
                    TransactionId = transactionId,
                    Message = "Status check completed",
                    Provider = ProviderName
                }, "Status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting eSewa payment status: TransactionId={TransactionId}", transactionId);
                return Result<PaymentStatusResponse>.Failure($"Status check failed: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔔 Processing eSewa webhook");

                // ✅ Basic webhook processing
                // You can enhance this with actual signature verification
                return Result<bool>.Success(true, "Webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing eSewa webhook");
                return Result<bool>.Failure($"Webhook processing failed: {ex.Message}");
            }
        }
    }

    // ✅ Configuration class (enhanced)
    public class EsewaConfig
    {
        public string MerchantId { get; }
        public string SecretKey { get; }
        public string BaseUrl { get; }
        public string VerifyUrl { get; }
        public string SuccessUrl { get; }
        public string FailureUrl { get; }

        public EsewaConfig(IConfiguration configuration)
        {
            var section = configuration.GetSection("PaymentGateways:Esewa");

            MerchantId = section["MerchantId"] ?? throw new ArgumentNullException(nameof(MerchantId), "eSewa MerchantId is required");
            SecretKey = section["SecretKey"] ?? throw new ArgumentNullException(nameof(SecretKey), "eSewa SecretKey is required");
            BaseUrl = section["BaseUrl"] ?? "https://rc-epay.esewa.com.np";
            VerifyUrl = $"{BaseUrl}/api/epay/transaction/status";
            SuccessUrl = section["SuccessUrl"] ?? "http://localhost:5225/payment/callback/esewa/success";
            FailureUrl = section["FailureUrl"] ?? "http://localhost:5225/payment/callback/esewa/failure";

            // ✅ Validate configuration
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid eSewa BaseUrl format", nameof(BaseUrl));
        }
    }

    // ✅ API Response DTOs (fixed property names)
    public class EsewaVerificationApiResponse
    {
        [JsonPropertyName("product_code")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("transaction_uuid")]
        public string TransactionUuid { get; set; } = string.Empty;

        [JsonPropertyName("total_amount")]
        public string TotalAmount { get; set; } = string.Empty;

        [JsonPropertyName("reference_id")]
        public string ReferenceId { get; set; } = string.Empty;
    }
}