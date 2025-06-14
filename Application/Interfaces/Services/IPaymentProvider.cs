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
    public interface IPaymentProvider
    {
        string ProviderName { get; }
        Task<Result<PaymentInitiationResponse>> InitiateAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken);
        Task<Result<PaymentVerificationResponse>> VerifyAsync(PaymentVerificationRequest request, CancellationToken cancellationToken);
        Task<Result<PaymentStatusResponse>> GetStatusAsync(string transactionId, CancellationToken cancellationToken);
        Task<Result<bool>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken);
    }
}
