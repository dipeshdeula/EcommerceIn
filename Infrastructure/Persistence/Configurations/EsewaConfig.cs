using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class EsewaConfig
    {
        public string MerchantId { get; }
        public string SecretKey { get; }
        public string BaseUrl { get; }
        public string InitiateUrl { get; }
        public string VerifyUrl { get; }
        public string SuccessUrl { get; }
        public string FailureUrl { get; }

        public EsewaConfig(IConfiguration configuration)
        {
            var section = configuration.GetSection("PaymentGateways:Esewa");
            MerchantId = section["MerchantId"] ?? throw new ArgumentNullException("Esewa MerchantId");
            SecretKey = section["SecretKey"] ?? throw new ArgumentNullException("Esewa SecretKey");
            BaseUrl = section["BaseUrl"] ?? "https://epay.esewa.com.np";
            InitiateUrl = $"{BaseUrl}/api/epay/main/v2/form";
            VerifyUrl = $"{BaseUrl}/api/epay/transaction/status";
            SuccessUrl = section["SuccessUrl"] ?? "http://localhost:5173/payment/success";
            FailureUrl = section["FailureUrl"] ?? "http://localhost:5173/payment/failure";

        }

    }
}
