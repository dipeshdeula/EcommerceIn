using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Provider
{
    public class KhaltiProvider : IPaymentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KhaltiProvider> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly KhaltiConfig _config;

        public string ProviderName => "Khalti";

        public KhaltiProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<KhaltiProvider> logger,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _config = new KhaltiConfig(configuration);
        }

        public async Task<Result<PaymentInitiationResponse>> InitiateAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Initiating Khalti payment: PaymentRequestId={PaymentRequestId}, Amount={Amount}",
                    paymentRequest.Id, paymentRequest.PaymentAmount);

                // Get user information for customer details
                var user = await _unitOfWork.Users.GetByIdAsync(paymentRequest.UserId, cancellationToken);
                if (user == null)
                {
                    return Result<PaymentInitiationResponse>.Failure("User not found for payment request");
                }

                // Create HTTP client without pre-configured headers
                using var client = _httpClientFactory.CreateClient();

                // Prepare Khalti payment request with correct class name
                var khaltiRequest = new KhaltiInitiateRequest // Fixed class name
                {
                    ReturnUrl = _config.ReturnUrl,
                    WebsiteUrl = _config.WebsiteUrl,
                    Amount = (int)(paymentRequest.PaymentAmount * 100), // Convert to paisa
                    PurchaseOrderId = $"ORD_{paymentRequest.OrderId}_{paymentRequest.Id}",
                    PurchaseOrderName = paymentRequest.Description ?? $"Payment for Order #{paymentRequest.OrderId}",
                    CustomerInfo = new KhaltiCustomerInfo
                    {
                        Name = user.Name ?? "Customer",
                        Email = user.Email ?? "customer@example.com",
                        Phone = user.Contact ?? "9800000000"
                    }
                };

                // Serialize request payload
                var jsonContent = JsonSerializer.Serialize(khaltiRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Add authorization header correctly (only once)
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Key {_config.SecretKey}");

                _logger.LogDebug("Khalti request URL: {Url}", _config.InitiateUrl);
                _logger.LogDebug("Khalti request payload: {Payload}", jsonContent);

                //   Use InitiateUrl instead of BaseUrl
                var response = await client.PostAsync(_config.InitiateUrl, content, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug(" Khalti response: StatusCode={StatusCode}, Response={Response}",
                    response.StatusCode, responseContent);

                //  Better error handling
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Khalti API error: StatusCode={StatusCode}, Response={Response}",
                        response.StatusCode, responseContent);

                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => Result<PaymentInitiationResponse>.Failure("Khalti API endpoint not found. Please check configuration."),
                        System.Net.HttpStatusCode.Unauthorized => Result<PaymentInitiationResponse>.Failure("Khalti authentication failed. Please check your secret key."),
                        System.Net.HttpStatusCode.BadRequest => Result<PaymentInitiationResponse>.Failure($"Invalid request to Khalti: {responseContent}"),
                        _ => Result<PaymentInitiationResponse>.Failure($"Khalti API error: {response.StatusCode} - {responseContent}")
                    };
                }

                // Deserialize response
                var khaltiResponse = JsonSerializer.Deserialize<KhaltiInitiateResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (khaltiResponse == null || string.IsNullOrEmpty(khaltiResponse.Pidx))
                {
                    _logger.LogError(" Invalid Khalti response: {Response}", responseContent);
                    return Result<PaymentInitiationResponse>.Failure("Invalid response from Khalti API");
                }

                //  Update payment request with Khalti Pidx
                paymentRequest.KhaltiPidx = khaltiResponse.Pidx;
                paymentRequest.PaymentStatus = "Initiated";
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Khalti payment initiated successfully: Pidx={Pidx}, PaymentUrl={PaymentUrl}",
                    khaltiResponse.Pidx, khaltiResponse.PaymentUrl);

                // Return successful response
                return Result<PaymentInitiationResponse>.Success(new PaymentInitiationResponse
                {
                    Provider = ProviderName,
                    PaymentUrl = khaltiResponse.PaymentUrl,
                    ProviderTransactionId = khaltiResponse.Pidx,
                    ExpiresAt = khaltiResponse.ExpiresAt != default ? khaltiResponse.ExpiresAt : DateTime.UtcNow.AddMinutes(15),
                    Status = "Initiated",
                    RequiresRedirect = true,
                    Instructions = "You will be redirected to Khalti for payment. Please complete within 15 minutes.",
                    Metadata = new Dictionary<string, string>
                    {
                        ["pidx"] = khaltiResponse.Pidx,
                        ["environment"] = _config.IsTestMode ? "test" : "live",
                        ["transactionId"] = khaltiResponse.Pidx,
                        ["customerName"] = user.Name ?? "",
                        ["customerEmail"] = user.Email ?? "",
                        ["amount"] = paymentRequest.PaymentAmount.ToString("F2"),
                        ["currency"] = "NPR"
                    }
                }, "Khalti payment initiated successfully");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during Khalti payment initiation: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout during Khalti payment initiation: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure("Request timeout. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Khalti payment initiation failed: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure($"Khalti initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Verifying Khalti payment: Pidx={Pidx}", request.KhaltiPidx);

                using var client = _httpClientFactory.CreateClient();

                // Prepare verification request
                var verificationPayload = new { pidx = request.KhaltiPidx };
                var jsonContent = JsonSerializer.Serialize(verificationPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Add authorization header
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Key {_config.SecretKey}");

                _logger.LogDebug("Khalti verification URL: {Url}", _config.VerifyUrl);
                _logger.LogDebug("Khalti verification payload: {Payload}", jsonContent);

                // Send verification request
                var response = await client.PostAsync(_config.VerifyUrl, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Khalti verification response: StatusCode={StatusCode}, Response={Response}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Khalti verification failed: StatusCode={StatusCode}, Response={Response}",
                        response.StatusCode, responseContent);
                    return Result<PaymentVerificationResponse>.Failure($"Khalti verification failed: HTTP {response.StatusCode}");
                }

                var verificationResult = JsonSerializer.Deserialize<KhaltiVerificationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Check if payment is successful
               var isSuccessful = verificationResult?.Status?.ToUpper() == "COMPLETED";
               var isPending = verificationResult?.Status?.ToUpper() == "PENDING";
               var isRefunded = verificationResult?.Status?.ToUpper() == "REFUNDED";
               var isExpired = verificationResult?.Status?.ToUpper() == "EXPIRED";
               var isCanceled = verificationResult?.Status?.ToUpper().Contains("CANCELED") == true;


                _logger.LogInformation("Khalti verification completed: IsSuccessful={IsSuccessful}, Status={Status}, Pidx={Pidx}",
                    isSuccessful, verificationResult?.Status, request.KhaltiPidx);

                string finalStatus = isSuccessful ? "Succeeded" :
                    isPending ? "Pending" :
                    isRefunded ? "Refunded" :
                    isExpired ? "Expired" :
                    isCanceled ? "Canceled" : "Failed";

                return Result<PaymentVerificationResponse>.Success(new PaymentVerificationResponse
                {
                    IsSuccessful = isSuccessful,
                    Status = finalStatus,
                    Message = isSuccessful ? "Payment verified successfully with Khalti" : $"Khalti payment status :{finalStatus}",
                    Provider = ProviderName,
                    TransactionId = request.KhaltiPidx ?? "",
                    CollectedAmount = isSuccessful ? (verificationResult?.TotalAmount ?? 0) / 100 : null, // Convert from paisa
                    CollectedAt = isSuccessful ? DateTime.UtcNow : null,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["khaltiStatus"] = verificationResult?.Status ?? "UNKNOWN",
                        ["transactionId"] = verificationResult?.TransactionId ?? "",
                        ["fee"] = verificationResult?.Fee ?? 0,
                        ["refunded"] = verificationResult?.Refunded ?? false,
                        ["verificationResponse"] = verificationResult ?? new object(),
                        ["verifiedAt"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }, $"Khalti verification completed: {finalStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Khalti verification error: Pidx={Pidx}", request.KhaltiPidx);
                return Result<PaymentVerificationResponse>.Failure($"Khalti verification error: {ex.Message}");
            }
        }

        public async Task<Result<PaymentStatusResponse>> GetStatusAsync(string transactionId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(" Getting Khalti payment status: TransactionId={TransactionId}", transactionId);

                // Use the same verification endpoint for status check
                var verificationRequest = new PaymentVerificationRequest { KhaltiPidx = transactionId };
                var verificationResult = await VerifyAsync(verificationRequest, cancellationToken);

                if (!verificationResult.Succeeded)
                {
                    return Result<PaymentStatusResponse>.Failure(verificationResult.Message);
                }

                return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse
                {
                    Status = verificationResult.Data.Status,
                    TransactionId = transactionId,
                    Message = verificationResult.Data.Message,
                    Provider = ProviderName,
                    AdditionalData = verificationResult.Data.AdditionalData
                }, "Khalti status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Khalti payment status: TransactionId={TransactionId}", transactionId);
                return Result<PaymentStatusResponse>.Failure($"Status check failed: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing Khalti webhook");
                return Result<bool>.Success(true, "Khalti webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Khalti webhook");
                return Result<bool>.Failure($"Webhook processing failed: {ex.Message}");
            }
        }
    }

    // FIXED Configuration class
    public class KhaltiConfig
    {
        public string SecretKey { get; }
        public string BaseUrl { get; }
        public string InitiateUrl { get; }
        public string VerifyUrl { get; }
        public string ReturnUrl { get; }
        public string WebsiteUrl { get; }
        public bool IsTestMode { get; }

        public KhaltiConfig(IConfiguration configuration)
        {
            var section = configuration.GetSection("PaymentGateways:Khalti");

            SecretKey = section["SecretKey"] ?? throw new ArgumentNullException(nameof(SecretKey), "Khalti SecretKey is required");

            // Handle both test and live URLs
            var environment = section["Environment"] ?? "test";
            IsTestMode = environment.ToLower() == "test";

            BaseUrl = IsTestMode
                ? "https://a.khalti.com/api/v2"
                : "https://khalti.com/api/v2";

            // Proper URL construction
            InitiateUrl = $"{BaseUrl}/epayment/initiate/";
            VerifyUrl = $"{BaseUrl}/epayment/lookup/";

            ReturnUrl = section["ReturnUrl"] ?? "http://localhost:5173/payment/success";
            WebsiteUrl = section["WebsiteUrl"] ?? "http://localhost:5173";

            // Validate configuration
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid Khalti BaseUrl format", nameof(BaseUrl));

            if (!Uri.TryCreate(InitiateUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid Khalti InitiateUrl format", nameof(InitiateUrl));

            if (!Uri.TryCreate(VerifyUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid Khalti VerifyUrl format", nameof(VerifyUrl));
        }
    }

    //  Request DTOs (according to Khalti official docs)
    public class KhaltiInitiateRequest
    {
        [JsonPropertyName("return_url")]
        public string ReturnUrl { get; set; } = string.Empty;

        [JsonPropertyName("website_url")]
        public string WebsiteUrl { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public int Amount { get; set; } // Amount in paisa

        [JsonPropertyName("purchase_order_id")]
        public string PurchaseOrderId { get; set; } = string.Empty;

        [JsonPropertyName("purchase_order_name")]
        public string PurchaseOrderName { get; set; } = string.Empty;

        [JsonPropertyName("customer_info")]
        public KhaltiCustomerInfo CustomerInfo { get; set; } = new();
    }

    public class KhaltiCustomerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;
    }

    // Response DTOs
    public class KhaltiInitiateResponse
    {
        [JsonPropertyName("pidx")]
        public string Pidx { get; set; } = string.Empty;

        [JsonPropertyName("payment_url")]
        public string PaymentUrl { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }

    public class KhaltiVerificationResponse
    {
        [JsonPropertyName("pidx")]
        public string Pidx { get; set; } = string.Empty;

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("fee")]
        public decimal Fee { get; set; }

        [JsonPropertyName("refunded")]
        public bool Refunded { get; set; }
    }
}