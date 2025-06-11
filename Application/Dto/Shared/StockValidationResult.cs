using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Shared
{
    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int AvailableQuantity { get; set; }
        public int MaxAllowedQuantity { get; set; }
        public bool CanProceed { get; set; }
        public string ErrorMessage => string.Join("; ", Errors);
    }

}
