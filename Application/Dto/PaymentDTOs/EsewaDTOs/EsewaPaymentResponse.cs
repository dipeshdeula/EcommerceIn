using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs.EsewaDTOs
{
    public class EsewaPaymentResponse
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public string TransactionUuid { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
