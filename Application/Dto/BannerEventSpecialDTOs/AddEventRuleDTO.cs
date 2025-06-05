using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class AddEventRuleDTO
    {
        public RuleType Type { get; set; }
        public string TargetValue { get; set; } = string.Empty;
        public string? Conditions { get; set; }
        public PromotionType DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public double? MaxDiscount { get; set; }
        public double? MinOrderValue { get; set; }
        public int Priority { get; set; } = 1;
    }
}
