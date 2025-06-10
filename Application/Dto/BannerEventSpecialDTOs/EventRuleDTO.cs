using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventRuleDTO
    {
        public int Id { get; set; }
        public RuleType Type { get; set; }
        public string TargetValue { get; set; } = string.Empty;
        public string? Conditions { get; set; }
        public PromotionType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int Priority { get; set; }

        public string RuleDescription => GenerateRuleDescription();

        private string GenerateRuleDescription()
        {
            var discount = DiscountType == PromotionType.Percentage
                ? $"{DiscountValue}%"
                : $"${DiscountValue}";

            return $"{Type}: {TargetValue} → {discount} discount";
        }
    }
}
