using Application.Common;
using Application.Dto.PaymentDTOs.EsewaDTOs;
using Infrastructure.Persistence.Configurations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services
{
    public class EsewaService : IEsewaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EsewaService> _logger;
        private readonly EsewaConfig _config;

        public EsewaService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EsewaService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _config = new EsewaConfig(configuration);
        }

        public async Task<Result<EsewaPaymentResponse>> InitiatePaymentAsync(EsewaPaymentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Initiating eSewa payment: Amount={Amount}, TransactionId={TransactionId}",
                    request.Amount, request.TransactionUuid);

                //  Generate signature according to eSewa documentation
                var signatureMessage = $"total_amount={request.TotalAmount:F2},transaction_uuid={request.TransactionUuid},product_code={_config.MerchantId}";
                var signature = GenerateSignature(signatureMessage, _config.SecretKey);

                // Build payment URL according to eSewa documentation
                var paymentUrl = BuildEsewaPaymentUrl(request, signature);

                _logger.LogInformation(" eSewa payment URL generated: {PaymentUrl}", paymentUrl);

                return Result<EsewaPaymentResponse>.Success(new EsewaPaymentResponse
                {
                    PaymentUrl = paymentUrl,
                    TransactionUuid = request.TransactionUuid,
                    Signature = signature,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    Status = "Initiated"
                }, "eSewa payment initiated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate eSewa payment");
                return Result<EsewaPaymentResponse>.Failure($"eSewa payment initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<EsewaVerificationResponse>> VerifyPaymentAsync(EsewaVerificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Verifying eSewa payment: TransactionId={TransactionId}", request.TransactionUuid);

                using var client = _httpClientFactory.CreateClient("EsewaClient");
                client.Timeout = TimeSpan.FromSeconds(30);

                //  eSewa verification endpoint according to documentation
                var verificationPayload = new
                {
                    product_code = _config.MerchantId,
                    total_amount = request.TotalAmount.ToString("F2"),
                    transaction_uuid = request.TransactionUuid
                };

                var jsonContent = JsonSerializer.Serialize(verificationPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(_config.VerifyUrl, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("eSewa verification response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var verificationResult = JsonSerializer.Deserialize<EsewaVerificationApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var isSuccessful = verificationResult?.Status?.ToUpper() == "COMPLETE";

                    return Result<EsewaVerificationResponse>.Success(new EsewaVerificationResponse
                    {
                        IsSuccessful = isSuccessful,
                        Status = isSuccessful ? "Succeeded" : "Failed",
                        TransactionId = request.TransactionUuid,
                        ReferenceId = verificationResult?.ReferenceId,
                        Message = isSuccessful ? "Payment verified successfully" : "Payment verification failed",
                        VerifiedAt = DateTime.UtcNow,
                        ApiResponse = verificationResult
                    }, "Payment verification completed");
                }
                else
                {
                    _logger.LogError("eSewa verification failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    return Result<EsewaVerificationResponse>.Failure($"eSewa verification failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during eSewa payment verification");
                return Result<EsewaVerificationResponse>.Failure($"Payment verification error: {ex.Message}");
            }
        }

        public string GenerateSignature(string message, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(signatureBytes);
        }

        public bool ValidateSignature(string message, string signature, string secretKey)
        {
            var expectedSignature = GenerateSignature(message, secretKey);
            return expectedSignature == signature;
        }

        private string BuildEsewaPaymentUrl(EsewaPaymentRequest request, string signature)
        {
            var parameters = new Dictionary<string, string>
            {
                ["tAmt"] = request.TotalAmount.ToString("F2"),
                ["amt"] = request.Amount.ToString("F2"),
                ["txAmt"] = request.TaxAmount.ToString("F2"),
                ["psc"] = request.ServiceCharge.ToString("F2"),
                ["pdc"] = request.DeliveryCharge.ToString("F2"),
                ["scd"] = _config.MerchantId,
                ["pid"] = request.TransactionUuid,
                ["su"] = _config.SuccessUrl,
                ["fu"] = _config.FailureUrl,
                ["signed_field_names"] = "total_amount,transaction_uuid,product_code",
                ["signature"] = signature
            };

            var queryString = string.Join("&", parameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return $"{_config.BaseUrl}/epay/main?{queryString}";
        }
    }
}
