using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services
{
    public class PaymentSecurityService : IPaymentSecurityService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentSecurityService> _logger;
        private readonly byte[] _encryptionKey;

        public PaymentSecurityService(IConfiguration configuration, ILogger<PaymentSecurityService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Initialize encryption key from configuration
            var keyString = _configuration["PaymentGateways:EncryptionKey"] ?? "DefaultKey123456789012345678901234";
            _encryptionKey = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
        }

        public string GenerateSecureTransactionId(string provider, int paymentRequestId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new Random().Next(1000, 9999);
            return $"{provider.ToUpper()}_{paymentRequestId}_{timestamp}_{random}";
        }

        public string GenerateSignature(string message, string secretKey)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return Convert.ToBase64String(signatureBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating signature");
                throw new SecurityException("Signature generation failed", ex);
            }
        }

        public bool ValidateSignature(string message, string signature, string secretKey)
        {
            try
            {
                var expectedSignature = GenerateSignature(message, secretKey);
                return string.Equals(expectedSignature, signature, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating signature");
                return false;
            }
        }

        public string EncryptSensitiveData(string data)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs))
                {
                    writer.Write(data);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error encrypting data");
                throw new SecurityException("Data encryption failed", ex);
            }
        }

        public string DecryptSensitiveData(string encryptedData)
        {
            try
            {
                var fullCipher = Convert.FromBase64String(encryptedData);

                using var aes = Aes.Create();
                aes.Key = _encryptionKey;

                var iv = new byte[aes.BlockSize / 8];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(cipher);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error decrypting data");
                throw new SecurityException("Data decryption failed", ex);
            }
        }

        public bool ValidateWebhookSignature(string payload, string signature, string secretKey)
        {
            return ValidateSignature(payload, signature, secretKey);
        }

        public string GenerateIdempotencyKey()
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}
