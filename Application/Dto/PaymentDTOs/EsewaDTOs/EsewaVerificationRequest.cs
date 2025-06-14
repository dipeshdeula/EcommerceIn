using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs.EsewaDTOs
{
    public class EsewaVerificationRequest
    {
        public string TransactionUuid { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ProductCode { get; set; } = string.Empty;
    }
}
