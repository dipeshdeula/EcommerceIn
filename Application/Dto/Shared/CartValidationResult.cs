using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Shared
{
    public class CartValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public decimal CurrentPrice { get; set; }
        public int AvailableStock { get; set; }
        public bool CanProceed { get; set; }
        public string ErrorMessage => string.Join("; ", Errors);

        public static CartValidationResult Success(decimal price, int stock) => new()
        {
            IsValid = true,
            CanProceed = true,
            CurrentPrice = price,
            AvailableStock = stock
        };

        public static CartValidationResult Failed(string error) => new()
        {
            IsValid = false,
            CanProceed = false,
            Errors = new List<string> { error }
        };
    }

}
