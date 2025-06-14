using Application.Common;
using Application.Dto.PaymentDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public  interface IPaymentGatewayService
    {
        Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default);
        Task<Result<PaymentVerificationResponse>> VerifyPaymentAsync(PaymentVerificationRequest request, CancellationToken cancellationToken = default);
        Task<Result<PaymentStatusResponse>> GetPaymentStatusAsync(string provider, string transactionId, CancellationToken cancellationToken = default);
        Task<Result<bool>> ProcessWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default);
        Task<Result<PaymentRefundResponse>> RefundPaymentAsync(PaymentRefundRequest request, CancellationToken cancellationToken = default);
    }
}
