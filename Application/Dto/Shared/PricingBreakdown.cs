using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Shared
{
    public class PricingBreakdown
    {
        public decimal OriginalPrice { get; set; }
        public decimal BasePrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public decimal ProductDiscountAmount { get; set; }
        public decimal EventDiscountAmount { get; set; }
        public decimal TotalSavings { get; set; }
        public decimal ProductDiscountPercentage { get; set; }
        public decimal EventDiscountPercentage { get; set; }
        public decimal TotalDiscountPercentage { get; set; }
        public bool HasProductDiscount { get; set; }
        public bool HasEventDiscount { get; set; }
        public string FormattedBreakdown { get; set; } = string.Empty;
    }
}
