using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PromoCodeDTOs
{
    public class PromoCodeValidationResultDTO
    {
        public bool IsValid { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal OriginalTotal { get; set; }
        public decimal FinalTotal { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string? Message { get; set; }
    }
}
