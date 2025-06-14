using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class CODConfig
    {
        public bool IsEnabled { get; }
        public decimal MaxOrderAmount { get; }
        public List<string> AvailableAreas { get; }

        public CODConfig(IConfiguration configuration)
        {
            var section = configuration.GetSection("PaymentGateways:COD");
            IsEnabled = section.GetValue<bool>("IsEnabled", true);
            MaxOrderAmount = section.GetValue<decimal>("MaxOrderAmount", 50000);
            AvailableAreas = section.GetSection("AvailableAreas").Get<List<string>>() ?? new List<string>();
        }
    }
}
