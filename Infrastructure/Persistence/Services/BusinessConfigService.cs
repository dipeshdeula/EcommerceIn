using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services
{
    public class BusinessConfigService : IBusinessConfigService
    {
        private readonly IConfiguration _configuration;

        public BusinessConfigService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string CompanyName => _configuration["BusinessSettings:CompanyName"] ?? "GetInstantMart";
        public string CompanyTagline => _configuration["BusinessSettings:CompanyTagline"] ?? "Fast delivery, great service, every time.";
        public string SupportEmail => _configuration["BusinessSettings:SupportEmail"] ?? "support@getinstantmart.com";
        public string SupportPhone => _configuration["BusinessSettings:SupportPhone"] ?? "+977-XXX-XXXX";
        public string WebsiteUrl => _configuration["BusinessSettings:WebsiteUrl"] ?? "https://getinstantmart.com";
        public int DefaultDeliveryTimeMinutes => int.Parse(_configuration["BusinessSettings:DefaultDeliveryTimeMinutes"] ?? "25");
        public string BusinessHoursStart => _configuration["BusinessSettings:BusinessHours:Start"] ?? "08:00";
        public string BusinessHoursEnd => _configuration["BusinessSettings:BusinessHours:End"] ?? "22:00";
        public string BusinessTimezone => _configuration["BusinessSettings:BusinessHours:Timezone"] ?? "Asia/Kathmandu";
    }
}
