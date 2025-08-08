using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PromoCodeDTOs
{
    public class UpdatePromoCodeDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        public decimal? DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }

        public int? MaxTotalUsage { get; set; }
        public int? MaxUsagePerUser { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public bool? ApplyToShipping { get; set; }
        public bool? StackableWithEvents { get; set; }

        public string? AdminNotes { get; set; }
    }
}
