using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums.BannerEventSpecial
{
    public enum PromotionType
    {
        Percentage = 0, // 20% off
        FixedAmount = 1, // $10 off
        BuyOneGetOne = 2, // Buy 1 Get 1 Free
        FreeShipping = 3, // Free Shipping
        Bundle = 4 // package Deal
    }
}
