using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Provider
{
    public class KhaltiProvider : IPaymentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KhaltiProvider> _logger;
        private readonly KhaltiConfig _config;

        public string ProviderName => "Khalti";

        public KhaltiProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<KhaltiProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _config = new KhaltiConfig(configuration);
        }

        public async Task<Result<PaymentInitiationResponse>> InitiateAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("KhaltiClient");

                var khaltiRequest = new KhaltiPaymentRequest
                {
                    Amount = (int)(paymentRequest.PaymentAmount * 100), // Convert to paisa
                    ReturnUrl = _config.ReturnUrl,
                    WebsiteUrl = _config.WebsiteUrl,
                    PurchaseOrderId = $"Order_{paymentRequest.OrderId}",
                    PurchaseOrderName = paymentRequest.Description,
                    CustomerInfo = new KhaltiCustomerInfo
                    {
                        Name = "Customer", // You'll need to get this from user
                        Email = "customer@example.com", // You'll need to get this from user
                        Phone = "9800000000" // You'll need to get this from user
                    }
                };

                client.DefaultRequestHeaders.Add("Authorization", $"Key {_config.SecretKey}");

                var response = await client.PostAsJsonAsync($"{_config.BaseUrl}/epayment/initiate/", khaltiRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<KhaltiInitiationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null || string.IsNullOrEmpty(result.Pidx))
                {
                    return Result<PaymentInitiationResponse>.Failure("Khalti failed to return valid response");
                }

                _logger.LogInformation("✅ Khalti payment initiated: Pidx={Pidx}, Amount={Amount}",
                    result.Pidx, paymentRequest.PaymentAmount);

                return Result<PaymentInitiationResponse>.Success(new PaymentInitiationResponse
                {
                    Provider = ProviderName,
                    PaymentUrl = result.PaymentUrl,
                    ProviderTransactionId = result.Pidx,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    Status = "Initiated",
                    RequiresRedirect = true,
                    Instructions = "You will be redirected to Khalti for payment.",
                    Metadata = new Dictionary<string, string>
                    {
                        ["pidx"] = result.Pidx,
                        ["environment"] = "test"
                    }
                }, "Khalti payment initiated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Khalti payment initiation failed");
                return Result<PaymentInitiationResponse>.Failure($"Khalti initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("KhaltiClient");

                var verificationPayload = new { pidx = request.KhaltiPidx };

                client.DefaultRequestHeaders.Add("Authorization", $"Key {_config.SecretKey}");
                var response = await client.PostAsJsonAsync($"{_config.BaseUrl}/epayment/lookup/", verificationPayload, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<KhaltiVerificationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var isSuccessful = result?.Status?.ToUpper() == "COMPLETED";

                return Result<PaymentVerificationResponse>.Success(new PaymentVerificationResponse
                {
                    IsSuccessful = isSuccessful,
                    Status = isSuccessful ? "Succeeded" : "Failed",
                    Message = isSuccessful ? "Payment verified successfully" : "Payment verification failed",
                    Provider = ProviderName,
                    TransactionId = request.KhaltiPidx ?? "",
                    CollectedAmount = isSuccessful ? (result?.TotalAmount ?? 0) / 100 : null, // Convert from paisa
                    CollectedAt = isSuccessful ? DateTime.UtcNow : null,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["transactionId"] = result?.TransactionId ?? "",
                        ["apiResponse"] = result
                    }
                }, "Khalti verification completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Khalti verification error");
                return Result<PaymentVerificationResponse>.Failure($"Khalti verification error: {ex.Message}");
            }
        }

        public async Task<Result<PaymentStatusResponse>> GetStatusAsync(string transactionId, CancellationToken cancellationToken)
        {
            return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse
            {
                Status = "Pending",
                TransactionId = transactionId,
                Message = "Status check not implemented yet"
            }, "Status retrieved");
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
        {
            return Result<bool>.Success(true, "Webhook processed");
        }
        public class KhaltiConfig
        {
            public string SecretKey { get; }
            public string BaseUrl { get; }
            public string ReturnUrl { get; }
            public string WebsiteUrl { get; }

            public KhaltiConfig(IConfiguration configuration)
            {
                var section = configuration.GetSection("PaymentGateways:Khalti");
                SecretKey = section["SecretKey"] ?? "";
                BaseUrl = section["BaseUrl"] ?? "https://khalti.com/api/v2";
                ReturnUrl = section["ReturnUrl"] ?? "http://localhost:5173/payment/success";
                WebsiteUrl = section["WebsiteUrl"] ?? "http://localhost:5225";
            }
        }

        public class KhaltiInitiationResponse
        {
            public string Pidx { get; set; } = string.Empty;
            public string PaymentUrl { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }

        public class KhaltiVerificationResponse
        {
            public string Pidx { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = string.Empty;
            public string TransactionId { get; set; } = string.Empty;
            public decimal Fee { get; set; }
            public bool Refunded { get; set; }
        }
    }
}
