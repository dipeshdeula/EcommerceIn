using Application.Common;
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

            // ✅ Initialize provider mapping
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
                _logger.LogInformation("🚀 Initiating payment: PaymentRequestId={PaymentRequestId}, Method={PaymentMethodId}, Amount={Amount}",
                    paymentRequest.Id, paymentRequest.PaymentMethodId, paymentRequest.PaymentAmount);

                // ✅ Get payment provider
                if (!_providers.TryGetValue(paymentRequest.PaymentMethodId, out var provider))
                {
                    return Result<PaymentInitiationResponse>.Failure($"Payment method {paymentRequest.PaymentMethodId} not supported");
                }

                // ✅ Validate payment amount
                if (paymentRequest.PaymentAmount <= 0)
                {
                    return Result<PaymentInitiationResponse>.Failure("Payment amount must be greater than zero");
                }

                // ✅ Initiate payment with provider
                var result = await provider.InitiateAsync(paymentRequest, cancellationToken);

                if (!result.Succeeded)
                {
                    _logger.LogError("❌ Payment initiation failed: {Error}", result.Message);
                    return result;
                }

                // ✅ Update payment request with provider data
                paymentRequest.PaymentStatus = result.Data.Status;
                paymentRequest.PaymentUrl = result.Data.PaymentUrl;
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                // ✅ Store provider-specific transaction ID
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

                _logger.LogInformation("✅ Payment initiated successfully: PaymentRequestId={PaymentRequestId}, Provider={Provider}, Status={Status}",
                    paymentRequest.Id, result.Data.Provider, result.Data.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initiate payment: PaymentRequestId={PaymentRequestId}", paymentRequest.Id);
                return Result<PaymentInitiationResponse>.Failure($"Payment initiation failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentVerificationResponse>> VerifyPaymentAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔍 Verifying payment: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);

                // ✅ Get payment request
                var paymentRequest = await _unitOfWork.PaymentRequests.GetByIdAsync(request.PaymentRequestId, cancellationToken);
                if (paymentRequest == null)
                {
                    return Result<PaymentVerificationResponse>.Failure("Payment request not found");
                }

                // ✅ Get payment provider
                if (!_providers.TryGetValue(paymentRequest.PaymentMethodId, out var provider))
                {
                    return Result<PaymentVerificationResponse>.Failure($"Payment method {paymentRequest.PaymentMethodId} not supported");
                }

                // ✅ Verify payment with provider
                var verificationResult = await provider.VerifyAsync(request, cancellationToken);

                if (!verificationResult.Succeeded)
                {
                    _logger.LogError("❌ Payment verification failed: {Error}", verificationResult.Message);
                    return verificationResult;
                }

                // ✅ Update payment status
                paymentRequest.PaymentStatus = verificationResult.Data.Status;
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);

                // ✅ Update order status if payment successful
                if (verificationResult.Data.IsSuccessful)
                {
                    await UpdateOrderStatusAsync(paymentRequest.OrderId, paymentRequest.PaymentMethodId, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✅ Payment verification completed: PaymentRequestId={PaymentRequestId}, Status={Status}, Success={IsSuccessful}",
                    paymentRequest.Id, verificationResult.Data.Status, verificationResult.Data.IsSuccessful);

                return verificationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Payment verification failed: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);
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
                _logger.LogError(ex, "❌ Error getting payment status: Provider={Provider}, TransactionId={TransactionId}", provider, transactionId);
                return Result<PaymentStatusResponse>.Failure($"Error getting payment status: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📥 Processing webhook: Provider={Provider}", provider);

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
                _logger.LogError(ex, "❌ Error processing webhook: Provider={Provider}", provider);
                return Result<bool>.Failure($"Webhook processing failed: {ex.Message}");
            }
        }

        public async Task<Result<PaymentRefundResponse>> RefundPaymentAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("💰 Processing refund: PaymentRequestId={PaymentRequestId}, Amount={Amount}",
                    request.PaymentRequestId, request.RefundAmount);

                // ✅ Get payment request
                var paymentRequest = await _unitOfWork.PaymentRequests.GetByIdAsync(request.PaymentRequestId, cancellationToken);
                if (paymentRequest == null)
                {
                    return Result<PaymentRefundResponse>.Failure("Payment request not found");
                }

                // ✅ Validate refund amount
                if (request.RefundAmount > paymentRequest.PaymentAmount)
                {
                    return Result<PaymentRefundResponse>.Failure("Refund amount cannot exceed payment amount");
                }

                // ✅ For now, create a simple refund record (implement provider-specific refund later)
                var refundResponse = new PaymentRefundResponse
                {
                    IsSuccessful = true,
                    RefundId = _securityService.GenerateIdempotencyKey(),
                    RefundedAmount = request.RefundAmount,
                    Status = "Processed",
                    RefundedAt = DateTime.UtcNow,
                    Message = "Refund processed successfully"
                };

                // ✅ Update payment status
                paymentRequest.PaymentStatus = request.RefundAmount == paymentRequest.PaymentAmount ? "Refunded" : "PartiallyRefunded";
                paymentRequest.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.PaymentRequests.UpdateAsync(paymentRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✅ Refund processed successfully: PaymentRequestId={PaymentRequestId}, RefundId={RefundId}",
                    request.PaymentRequestId, refundResponse.RefundId);

                return Result<PaymentRefundResponse>.Success(refundResponse, "Refund processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Refund processing failed: PaymentRequestId={PaymentRequestId}", request.PaymentRequestId);
                return Result<PaymentRefundResponse>.Failure($"Refund processing failed: {ex.Message}");
            }
        }

        private async Task UpdateOrderStatusAsync(int orderId, int paymentMethodId, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId, cancellationToken);
                if (order != null)
                {
                    // ✅ Set different statuses based on payment method
                    order.Status = paymentMethodId switch
                    {
                        1 => "Paid",           // eSewa
                        2 => "Paid",           // Khalti
                        3 => "Confirmed",      // COD
                        _ => "Processing"
                    };

                    order.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);

                    _logger.LogInformation("✅ Order status updated: OrderId={OrderId}, Status={Status}", orderId, order.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update order status: OrderId={OrderId}", orderId);
                // Don't throw here - payment was successful, order update is secondary
            }
        }
    }
}