using Application.Common;
using Application.Dto.PaymentDTOs.EsewaDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IEsewaService
    {
        Task<Result<EsewaPaymentResponse>> InitiatePaymentAsync(EsewaPaymentRequest request, CancellationToken cancellationToken = default);
        Task<Result<EsewaVerificationResponse>> VerifyPaymentAsync(EsewaVerificationRequest request, CancellationToken cancellationToken = default);
        string GenerateSignature(string message, string secretKey);
        bool ValidateSignature(string message, string signature, string secretKey);
    }
}
