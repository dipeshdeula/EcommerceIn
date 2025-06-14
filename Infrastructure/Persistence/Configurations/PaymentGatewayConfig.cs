using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class PaymentGatewayConfig
    {
        public EsewaConfig Esewa { get; }
        public KhaltiConfig Khalti { get; }
        public CODConfig COD { get; } // Cash on Delivery

        public PaymentGatewayConfig(IConfiguration configuration)
        {
            Esewa = new EsewaConfig(configuration);
            Khalti = new KhaltiConfig(configuration);
            COD = new CODConfig(configuration);
        }
    }
}
