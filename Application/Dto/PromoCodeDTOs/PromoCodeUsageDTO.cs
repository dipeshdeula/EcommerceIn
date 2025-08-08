using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PromoCodeDTOs
{

    public class PromoCodeUsageDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime UsedAt { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
