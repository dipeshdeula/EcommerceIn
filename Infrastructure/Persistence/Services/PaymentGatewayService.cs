/*using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Provider;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentSecurityService _securityService;
        private readonly ILogger<PaymentGatewayService> _logger;
        private readonly Dictionary<int, IPaymentProvider> _providers;

        public PaymentGatewayService(
            IUnitOfWork unitOfWork,
            IPaymentSecurityService securityService,
            ILogger<PaymentGatewayService> logger,
            EsewaProvider esewaProvider,
            KhaltiProvider khaltiProvider,
            CODProvider codProvider)
        {
            _unitOfWork = unitOfWork;
            _securityService = securityService;
            _logger = logger;

            // Initialize provider mapping
            _providers = new Dictionary<int, IPaymentProvider>
            {
                { 1, esewaProvider },   // eSewa
                { 2, khaltiProvider },  // Khalti  
                { 3, codProvider }      // COD
            };
        }

        public async Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Initiating payment: PaymentRequestId={PaymentRequestId}, Method={PaymentMethodId}, Amount={Amount}",
                    paymentRequest.Id, paymentRequest.PaymentMethodId, paymentRequest.PaymentAmount);

                // Get payment provider
                if (!_providers.TryGetValue(paymentRequest.PaymentMethodId, out var provider))
                {
                    return Result<PaymentInitiationResponse>.Failure($"Payment method {paymentRequest.PaymentMethodId} not supported");
                }

                // Validate payment amount
                if (paymentRequest.PaymentAmount <= 0)
                {
                    return Result<PaymentInitiationResponse>.Failure("Payment amount must be greater than zero");
                }

                // Initiate payment with provider
                var result = await provider.InitiateAsync(paymentRequest, cancellationToken);

                if (!result.Succeeded)
                {
                    _logger.LogError("Payment initiation failed: {Error}", result.Message);
                    return result;
                }

                //  Update payment request with provider data
                paymentRequest.PaymentStatus = result.Data.Status;
                paymentRequest.PaymentUrl = result.Data.PaymentUrl;
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                // Store provider-specific transaction ID
                if (!string.IsNullOrEmpty(result.Data.ProviderTransactionId))
                {
                    switch (paymentRequest.PaymentMethodId)
                    {
                        case 1: // eSewa
                            paymentRequest.EsewaTransactionId = result.Data.ProviderTransactionId;
                            break;
                        case 2: // Khalti
                            paymentRequest.KhaltiPidx = result.Data.ProviderTransactionId;
                            break;
                        case 3: // COD
                            // COD doesn't need external transaction ID
                            break;
                    }
                }

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment initiated successfully: PaymentRequestId={PaymentRequestId}, Provider={Provider}, Status={Status}",
                    paymentRequest.Id, result.Data.Provider, result.Data.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate payment: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure($"Payment initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyPaymentAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Verifying payment: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);

                // Get payment request
                var paymentRequest = await _unitOfWork.PaymentRequests.GetByIdAsync(request.PaymentRequestId, cancellationToken);
                if (paymentRequest == null)
                {
                    return Result<PaymentVerificationResponse>.Failure("Payment request not found");
                }

                // Get payment provider
                if (!_providers.TryGetValue(paymentRequest.PaymentMethodId, out var provider))
                {
                    return Result<PaymentVerificationResponse>.Failure($"Payment method {paymentRequest.PaymentMethodId} not supported");
                }

                // Verify payment with provider
                var verificationResult = await provider.VerifyAsync(request, cancellationToken);

                if (!verificationResult.Succeeded)
                {
                    _logger.LogError(" Payment verification failed: {Error}", verificationResult.Message);
                    return verificationResult;
                }

                //  Update payment status
                paymentRequest.PaymentStatus = verificationResult.Data.Status;
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);

                // Update order status if payment successful
                if (verificationResult.Data.IsSuccessful)
                {
                    await UpdateOrderPaymentStatusAsync(paymentRequest.OrderId, paymentRequest.PaymentMethodId, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment verification completed: PaymentRequestId={PaymentRequestId}, Status={Status}, Success={IsSuccessful}",
                    paymentRequest.Id, verificationResult.Data.Status, verificationResult.Data.IsSuccessful);

                return verificationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification failed: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);
                return Result<PaymentVerificationResponse>.Failure($"Payment verification failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentStatusResponse>> GetPaymentStatusAsync(string provider, string transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Find provider by name
                var paymentProvider = _providers.Values.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

                if (paymentProvider == null)
                {
                    return Result<PaymentStatusResponse>.Failure($"Provider {provider} not found");
                }

                return await paymentProvider.GetStatusAsync(transactionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status: Provider={Provider}, TransactionId={TransactionId}", provider, transactionId);
                return Result<PaymentStatusResponse>.Failure($"Error getting payment status: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing webhook: Provider={Provider}", provider);

                // Find provider by name
                var paymentProvider = _providers.Values.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

                if (paymentProvider == null)
                {
                    return Result<bool>.Failure($"Provider {provider} not found");
                }

                return await paymentProvider.ProcessWebhookAsync(payload, signature, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook: Provider={Provider}", provider);
                return Result<bool>.Failure($"Webhook processing failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentRefundResponse>> RefundPaymentAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing refund: PaymentRequestId={PaymentRequestId}, Amount={Amount}",
                    request.PaymentRequestId, request.RefundAmount);

                // Get payment request
                var paymentRequest = await _unitOfWork.PaymentRequests.GetByIdAsync(request.PaymentRequestId, cancellationToken);
                if (paymentRequest == null)
                {
                    return Result<PaymentRefundResponse>.Failure("Payment request not found");
                }

                // Validate refund amount
                if (request.RefundAmount > paymentRequest.PaymentAmount)
                {
                    return Result<PaymentRefundResponse>.Failure("Refund amount cannot exceed payment amount");
                }

                // For now, create a simple refund record (implement provider-specific refund later)
                var refundResponse = new PaymentRefundResponse
                {
                    IsSuccessful = true,
                    RefundId = _securityService.GenerateIdempotencyKey(),
                    RefundedAmount = request.RefundAmount,
                    Status = "Processed",
                    RefundedAt = DateTime.UtcNow,
                    Message = "Refund processed successfully"
                };

                // Update payment status
                paymentRequest.PaymentStatus = request.RefundAmount == paymentRequest.PaymentAmount ? "Refunded" : "PartiallyRefunded";
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Refund processed successfully: PaymentRequestId={PaymentRequestId}, RefundId={RefundId}",
                    request.PaymentRequestId, refundResponse.RefundId);

                return Result<PaymentRefundResponse>.Success(refundResponse, "Refund processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund processing failed: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);
                return Result<PaymentRefundResponse>.Failure($"Refund processing failed: {ex.Message}");
            }
        }

        private async Task UpdateOrderPaymentStatusAsync(int orderId, int paymentMethodId, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
                if (order != null)
                {
                    // Set different statuses based on payment method
                    order.PaymentStatus = paymentMethodId switch
                    {
                        1 => "Paid",           // eSewa
                        2 => "Paid",           // Khalti
                        3 => "Confirmed",      // COD
                        _ => "Processing"
                    };
                    order.OrderStatus = "COMPLETED";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);

                    _logger.LogInformation("Order status updated: OrderId={OrderId}, Status={Status}", orderId, order.PaymentStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order status: OrderId={OrderId}", orderId);
                // Don't throw here - payment was successful, order update is secondary
            }
        }
    }
}*/

using Application.Common;
using Application.Dto.PaymentDTOs;
using Application.Dto.PaymentMethodDTOs;
using Application.Interfaces.Services;
using Application.Provider;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentSecurityService _securityService;
        private readonly ILogger<PaymentGatewayService> _logger;
        private readonly Dictionary<string, IPaymentProvider> _providers;
        private readonly IMemoryCache _cache;

        public PaymentGatewayService(
            IUnitOfWork unitOfWork,
            IPaymentSecurityService securityService,
            ILogger<PaymentGatewayService> logger,
            IMemoryCache cache,
            IEnumerable<IPaymentProvider> paymentProviders) // Inject all providers via DI
        {
            _unitOfWork = unitOfWork;
            _securityService = securityService;
            _logger = logger;
            _cache = cache;

            // Build provider dictionary by provider name instead of hardcoded IDs
            _providers = new Dictionary<string, IPaymentProvider>(StringComparer.OrdinalIgnoreCase);
            foreach (var provider in paymentProviders)
            {
                _providers[provider.ProviderName.ToLower()] = provider;


                // common variation for compatibility
                switch (provider.ProviderName.ToLower())
                {
                    case "esewa":
                        _providers["Esewa"] = provider; // Database format
                        break;
                    case "khalti":
                        _providers["Khalti"] = provider; // Database format
                        break;
                    case "cod":
                        _providers["COD"] = provider; // Database format
                        break;
                }
            }

            _logger.LogInformation("Initialized PaymentGatewayService with {ProviderCount} providers: {Providers}",
                _providers.Count, string.Join(", ", _providers.Keys));
        }

        public async Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Initiating payment: PaymentRequestId={PaymentRequestId}, Method={PaymentMethodId}, Amount={Amount}",
                    paymentRequest.Id, paymentRequest.PaymentMethodId, paymentRequest.PaymentAmount);

                // ✅ Get payment method configuration from database
                var paymentMethod = await GetPaymentMethodAsync(paymentRequest.PaymentMethodId, cancellationToken);
                if (paymentMethod == null)
                {
                    return Result<PaymentInitiationResponse>.Failure($"Payment method {paymentRequest.PaymentMethodId} not found");
                }

                if (!paymentMethod.IsActive)
                {
                    return Result<PaymentInitiationResponse>.Failure($"Payment method {paymentMethod.ProviderName} is currently unavailable");
                }
                             

                // Get payment provider by name instead of hardcoded ID
                if (!_providers.TryGetValue(paymentMethod.ProviderName, out var provider))
                {
                    return Result<PaymentInitiationResponse>.Failure($"Payment provider '{paymentMethod.ProviderName}' not available");
                }

                // Initiate payment with provider
                var result = await provider.InitiateAsync(paymentRequest, cancellationToken);

                if (!result.Succeeded)
                {
                    _logger.LogError("Payment initiation failed for provider {Provider}: {Error}",
                        paymentMethod.ProviderName, result.Message);
                    return result;
                }

                // Update payment request with provider data
                await UpdatePaymentRequestWithProviderData(paymentRequest, paymentMethod, result.Data, cancellationToken);

                _logger.LogInformation("Payment initiated successfully: PaymentRequestId={PaymentRequestId}, Provider={Provider}, Status={Status}",
                    paymentRequest.Id, result.Data.Provider, result.Data.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate payment: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure($"Payment initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyPaymentAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Verifying payment: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);

                //  Get payment request with method information
                var paymentRequest = await _unitOfWork.PaymentRequests.GetAsync(
                    predicate: pr => pr.Id == request.PaymentRequestId,
                    includeProperties: "PaymentMethod",
                    cancellationToken: cancellationToken);

                if (paymentRequest == null)
                {
                    return Result<PaymentVerificationResponse>.Failure("Payment request not found");
                }

                //  Get provider by method name
                var paymentMethod = paymentRequest.PaymentMethod ?? await GetPaymentMethodAsync(paymentRequest.PaymentMethodId, cancellationToken);
                if (paymentMethod == null)
                {
                    return Result<PaymentVerificationResponse>.Failure("Payment method not found");
                }

                if (!_providers.TryGetValue(paymentMethod.ProviderName, out var provider))
                {
                    return Result<PaymentVerificationResponse>.Failure($"Payment provider '{paymentMethod.ProviderName}' not available");
                }

                //  Verify payment with provider
                var verificationResult = await provider.VerifyAsync(request, cancellationToken);

                if (!verificationResult.Succeeded)
                {
                    _logger.LogError("Payment verification failed for provider {Provider}: {Error}",
                        paymentMethod.ProviderName, verificationResult.Message);
                    return verificationResult;
                }

                //  Update payment and order status
                await UpdatePaymentVerificationResult(paymentRequest, paymentMethod, verificationResult.Data, cancellationToken);

                _logger.LogInformation("Payment verification completed: PaymentRequestId={PaymentRequestId}, Status={Status}, Success={IsSuccessful}",
                    paymentRequest.Id, verificationResult.Data.Status, verificationResult.Data.IsSuccessful);

                return verificationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification failed: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);
                return Result<PaymentVerificationResponse>.Failure($"Payment verification failed: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentMethodResponseDTO>>> GetAvailablePaymentMethodsAsync(decimal? amount = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"payment_methods_{amount}";               

                

                if (_cache.TryGetValue(cacheKey, out List<PaymentMethodResponseDTO> cachedMethods))
                {
                    return Result<List<PaymentMethodResponseDTO>>.Success(cachedMethods);
                }

                var paymentMethods = await _unitOfWork.PaymentMethods.GetAllAsync(
                    predicate: pm => pm.IsActive,
                    orderBy: q => q.OrderByDescending(pm => pm.Id).ThenBy(pm => pm.ProviderName),
                    cancellationToken: cancellationToken);

                var availableMethods = paymentMethods
                    .Where(pm => IsPaymentMethodAvailable(pm, amount))
                    .Select(pm => new PaymentMethodResponseDTO
                    {
                        Id = pm.Id,                       
                        ProviderName = pm.ProviderName,
                        Type = pm.Type.ToString(),
                        RequiresRedirect = pm.RequiresRedirect,                       
                        Logo = pm.Logo,                       
                        SupportedCurrencies = pm.SupportedCurrencies?.Split(',') ?? new[] { "NPR" },
                        IsAvailable = _providers.ContainsKey(pm.ProviderName)
                    })
                    .ToList();

                    var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Size = Math.Max(1, availableMethods.Count),
                    Priority = CacheItemPriority.High
                };

                // Cache for 5 minutes
                _cache.Set(cacheKey, availableMethods, cacheOptions);

                return Result<List<PaymentMethodResponseDTO>>.Success(availableMethods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available payment methods");
                return Result<List<PaymentMethodResponseDTO>>.Failure($"Error getting payment methods: {ex.Message}");
            }
        }

        public async Task<Result<PaymentStatusResponse>> GetPaymentStatusAsync(string provider, string transactionId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_providers.TryGetValue(provider, out var paymentProvider))
                {
                    return Result<PaymentStatusResponse>.Failure($"Provider {provider} not found");
                }

                return await paymentProvider.GetStatusAsync(transactionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status: Provider={Provider}, TransactionId={TransactionId}", provider, transactionId);
                return Result<PaymentStatusResponse>.Failure($"Error getting payment status: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_providers.TryGetValue(provider, out var paymentProvider))
                {
                    return Result<bool>.Failure($"Provider {provider} not found");
                }

                return await paymentProvider.ProcessWebhookAsync(payload, signature, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook: Provider={Provider}", provider);
                return Result<bool>.Failure($"Webhook processing failed: {ex.Message}");
            }
        }

        //  PRIVATE HELPER METHODS

        private async Task<PaymentMethod?> GetPaymentMethodAsync(int paymentMethodId, CancellationToken cancellationToken)
        {
            var cacheKey = $"payment_method_{paymentMethodId}";
            
            if (_cache.TryGetValue(cacheKey, out PaymentMethod cachedMethod))
            {
                return cachedMethod;
            }

            var paymentMethod = await _unitOfWork.PaymentMethods.GetByIdAsync(paymentMethodId, cancellationToken);

            if (paymentMethod != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Size = 1,
                Priority = CacheItemPriority.High
            };

                _cache.Set(cacheKey, paymentMethod, cacheOptions);
            }

            return paymentMethod;
        }

       

        private bool IsPaymentMethodAvailable(PaymentMethod paymentMethod, decimal? amount)
        {
            if (!paymentMethod.IsActive)
                return false;

            if (!_providers.ContainsKey(paymentMethod.ProviderName))
                return false;
            

            return true;
        }

        private async Task UpdatePaymentRequestWithProviderData(PaymentRequest paymentRequest, PaymentMethod paymentMethod, PaymentInitiationResponse response, CancellationToken cancellationToken)
        {
            paymentRequest.PaymentStatus = response.Status;
            paymentRequest.PaymentUrl = response.PaymentUrl;
            paymentRequest.UpdatedAt = DateTime.UtcNow;

            // ✅ Store provider-specific transaction ID based on provider name
            switch (paymentMethod.ProviderName.ToLower())
            {
                case "esewa":
                    paymentRequest.EsewaTransactionId = response.ProviderTransactionId;
                    break;
                case "khalti":
                    paymentRequest.KhaltiPidx = response.ProviderTransactionId;
                    break;
                case "cod":
                    // COD doesn't need external transaction ID
                    break;
                default:
                    _logger.LogWarning("Unknown provider name for transaction ID storage: {ProviderName}", paymentMethod.ProviderName);
                    break;
            }

            await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdatePaymentVerificationResult(PaymentRequest paymentRequest, PaymentMethod paymentMethod, PaymentVerificationResponse response, CancellationToken cancellationToken)
        {
            paymentRequest.PaymentStatus = response.Status;
            paymentRequest.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);

            // Update order status if payment successful
            if (response.IsSuccessful)
            {
                await UpdateOrderPaymentStatusAsync(paymentRequest.OrderId, paymentMethod, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateOrderPaymentStatusAsync(int orderId, PaymentMethod paymentMethod, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
                if (order != null)
                {
                    //  Set different statuses based on payment method type
                    order.PaymentStatus = paymentMethod.Type switch
                    {
                        PaymentMethodType.DigitalPayments => "Paid",
                        PaymentMethodType.COD => "Confirmed",
                        
                        _ => "Processing"
                    };

                    order.OrderStatus = "Confirmed";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);

                    _logger.LogInformation("Order status updated: OrderId={OrderId}, PaymentStatus={PaymentStatus}, PaymentMethod={PaymentMethod}",
                        orderId, order.PaymentStatus, paymentMethod.ProviderName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order status: OrderId={OrderId}", orderId);
            }
        }

        public async Task<Result<PaymentRefundResponse>> RefundPaymentAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing refund: PaymentRequestId={PaymentRequestId}, Amount={Amount}",
                    request.PaymentRequestId, request.RefundAmount);

                var paymentRequest = await _unitOfWork.PaymentRequests.GetAsync(
                    predicate: pr => pr.Id == request.PaymentRequestId,
                    includeProperties: "PaymentMethod",
                    cancellationToken: cancellationToken);

                if (paymentRequest == null)
                {
                    return Result<PaymentRefundResponse>.Failure("Payment request not found");
                }

                if (request.RefundAmount > paymentRequest.PaymentAmount)
                {
                    return Result<PaymentRefundResponse>.Failure("Refund amount cannot exceed payment amount");
                }

                var refundResponse = new PaymentRefundResponse
                {
                    IsSuccessful = true,
                    RefundId = _securityService.GenerateIdempotencyKey(),
                    RefundedAmount = request.RefundAmount,
                    Status = "Processed",
                    RefundedAt = DateTime.UtcNow,
                    Message = "Refund processed successfully"
                };

                paymentRequest.PaymentStatus = request.RefundAmount == paymentRequest.PaymentAmount ? "Refunded" : "PartiallyRefunded";
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<PaymentRefundResponse>.Success(refundResponse, "Refund processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund processing failed: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);
                return Result<PaymentRefundResponse>.Failure($"Refund processing failed: {ex.Message}");
            }
        }
    }
}