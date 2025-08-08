using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum PromoCodeType
    {
        Percentage = 0,     // 20% off
        FixedAmount = 1,    // Rs.100 off
        FreeShipping = 2,   // Free shipping
        BuyXGetY = 3       // Buy 2 Get 1 (future)
    }

    public enum PromoCodeStatus
    {
        Draft = 0,
        Active = 1,
        Expired = 2,
        Suspended = 3,
        ExhaustedUsage = 4
    }
}
