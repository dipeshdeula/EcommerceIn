using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class KhaltiConfig
    {
        public string SecretKey { get; }
        public string BaseUrl { get; }
        public string InitiateUrl { get; }
        public string VerifyUrl { get; }
        public string ReturnUrl { get; }
        public string WebsiteUrl { get; }

        public KhaltiConfig(IConfiguration configuration)
        {
            var section = configuration.GetSection("PaymentGateways:Khalti");
            SecretKey = section["SecretKey"] ?? throw new ArgumentNullException("Khalti SecretKey");
            BaseUrl = section["BaseUrl"] ?? "https://khalti.com/api/v2";
            InitiateUrl = $"{BaseUrl}/epayment/initiate/";
            VerifyUrl = $"{BaseUrl}/epayment/lookup/";
            ReturnUrl = section["ReturnUrl"] ?? "http://localhost:5173/payment/success";
            WebsiteUrl = section["WebsiteUrl"] ?? "http://localhost:5225";
        }
    }
}
