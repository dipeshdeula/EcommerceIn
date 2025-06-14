using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IPaymentSecurityService
    {
        string GenerateSecureTransactionId(string provider, int paymentRequestId);
        string GenerateSignature(string message, string secretKey);
        bool ValidateSignature(string message, string signature, string secretKey);
        string EncryptSensitiveData(string data);
        string DecryptSensitiveData(string encryptedData);
        bool ValidateWebhookSignature(string payload, string signature, string secretKey);
        string GenerateIdempotencyKey();
    }
}
