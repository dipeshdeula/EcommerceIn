using Application.Common;
using Application.Dto.PaymentDTOs.EsewaDTOs;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                var transactionUuid = GenerateTransactionId(paymentRequest.Id);

                var esewaRequest = new EsewaPaymentRequest
                {
                    TotalAmount = paymentRequest.PaymentAmount,
                    Amount = paymentRequest.PaymentAmount,
                    TaxAmount = 0,
                    ServiceCharge = 0,
                    DeliveryCharge = 0,
                    TransactionUuid = transactionUuid,
                    ProductCode = _config.MerchantId,
                    SuccessUrl = _config.SuccessUrl,
                    FailureUrl = _config.FailureUrl
                };

                var signature = GenerateSignature(esewaRequest);
                //var formHtml = BuildPaymentForm(esewaRequest, signature);
                var paymentUrl = BuildEsewaPaymentUrl(esewaRequest, signature);

                _logger.LogInformation("✅ eSewa payment initiated: TransactionId={TransactionId}, Amount={Amount}",
                    transactionUuid, paymentRequest.PaymentAmount);

                return Result<PaymentInitiationResponse>.Success(new PaymentInitiationResponse
                {
                    Provider = ProviderName,
                    PaymentUrl = paymentUrl,
                    PaymentFormHtml = null,
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
                _logger.LogError(ex, "❌ eSewa payment initiation failed");
                return Result<PaymentInitiationResponse>.Failure($"eSewa initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("EsewaClient");

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

                _logger.LogDebug("📥 eSewa verification response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var verificationResult = JsonSerializer.Deserialize<EsewaVerificationApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var isSuccessful = verificationResult?.Status?.ToUpper() == "COMPLETE";

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
                            ["apiResponse"] = verificationResult 
                        }
                    }, "eSewa verification completed");
                }

                return Result<PaymentVerificationResponse>.Failure("eSewa verification failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ eSewa verification error");
                return Result<PaymentVerificationResponse>.Failure($"eSewa verification error: {ex.Message}");
            }
        }

        public async Task<Result<PaymentStatusResponse>> GetStatusAsync(string transactionId, CancellationToken cancellationToken)
        {
            // Implementation for getting payment status
            return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse
            {
                Status = "Pending",
                TransactionId = transactionId,
                Message = "Status check not implemented yet"
            }, "Status retrieved");
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
        {
            // Implementation for webhook processing
            return Result<bool>.Success(true, "Webhook processed");
        }

        private string GenerateTransactionId(int paymentRequestId)
        {
            return $"ESW_{paymentRequestId}_{DateTime.UtcNow.Ticks}";
        }

        private string GenerateSignature(EsewaPaymentRequest request)
        {
            var signatureMessage = $"total_amount={request.TotalAmount:F2},transaction_uuid={request.TransactionUuid},product_code={request.ProductCode}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.SecretKey));
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureMessage));
            return Convert.ToBase64String(signatureBytes);
        }

        private string BuildPaymentForm(EsewaPaymentRequest request, string signature)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>🔄 Redirecting to eSewa...</title>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #00b894 0%, #00a085 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            color: white;
        }}
        .container {{
            text-align: center;
            background: rgba(255, 255, 255, 0.1);
            backdrop-filter: blur(10px);
            padding: 40px;
            border-radius: 20px;
            box-shadow: 0 8px 32px rgba(31, 38, 135, 0.37);
            border: 1px solid rgba(255, 255, 255, 0.18);
        }}
        .loader {{
            border: 4px solid rgba(255, 255, 255, 0.3);
            border-top: 4px solid #fff;
            border-radius: 50%;
            width: 50px;
            height: 50px;
            animation: spin 1s linear infinite;
            margin: 20px auto;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
        .btn {{
            background: #ff6b6b;
            color: white;
            border: none;
            padding: 12px 24px;
            border-radius: 8px;
            cursor: pointer;
            font-size: 16px;
            margin-top: 20px;
            transition: background 0.3s;
        }}
        .btn:hover {{ background: #ee5a52; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>🔒 eSewa Secure Payment</h2>
        <div class='loader'></div>
        <p>Redirecting to eSewa for secure payment...</p>
        
        <div style='margin: 20px 0; font-size: 14px; opacity: 0.8;'>
            <p><strong>Amount:</strong> NPR {request.TotalAmount:F2}</p>
            <p><strong>Transaction ID:</strong> {request.TransactionUuid}</p>
        </div>
        
        <form id='esewaForm' action='{_config.BaseUrl}/epay/main' method='POST'>
            <input type='hidden' name='tAmt' value='{request.TotalAmount:F2}'>
            <input type='hidden' name='amt' value='{request.Amount:F2}'>
            <input type='hidden' name='txAmt' value='{request.TaxAmount:F2}'>
            <input type='hidden' name='psc' value='{request.ServiceCharge:F2}'>
            <input type='hidden' name='pdc' value='{request.DeliveryCharge:F2}'>
            <input type='hidden' name='scd' value='{request.ProductCode}'>
            <input type='hidden' name='pid' value='{request.TransactionUuid}'>
            <input type='hidden' name='su' value='{request.SuccessUrl}'>
            <input type='hidden' name='fu' value='{request.FailureUrl}'>
            <input type='hidden' name='signed_field_names' value='total_amount,transaction_uuid,product_code'>
            <input type='hidden' name='signature' value='{signature}'>
            
            <button type='submit' class='btn'>🚀 Continue to eSewa</button>
        </form>
    </div>
    
    <script>
        setTimeout(() => document.getElementById('esewaForm').submit(), 3000);
    </script>
</body>
</html>";
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
                ["scd"] = request.ProductCode,
                ["pid"] = request.TransactionUuid,
                ["su"] = request.SuccessUrl,
                ["fu"] = request.FailureUrl,
                ["signed_field_names"] = "total_amount,transaction_uuid,product_code",
                ["signature"] = signature
            };

            var queryString = string.Join("&", parameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var paymentUrl = $"{_config.BaseUrl}/epay/main?{queryString}";

            _logger.LogDebug("🔗 Generated eSewa payment URL: {PaymentUrl}", paymentUrl);

            return paymentUrl;
        }
    }

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
            MerchantId = section["MerchantId"] ?? "EPAYTEST";
            SecretKey = section["SecretKey"] ?? "8gBm/:&EnhH.1/q";
            BaseUrl = section["BaseUrl"] ?? "https://rc-epay.esewa.com.np";
            VerifyUrl = $"{BaseUrl}/api/epay/transaction/status";
            SuccessUrl = section["SuccessUrl"] ?? "http://localhost:5225/payment/callback/esewa/success";
            FailureUrl = section["FailureUrl"] ?? "http://localhost:5225/payment/callback/esewa/failure";
        }
    }

    public class EsewaVerificationApiResponse
    {
        public string Product_code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Transaction_uuid { get; set; } = string.Empty;
        public string Total_amount { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
    }
}

