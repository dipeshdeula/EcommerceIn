using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs.EsewaDTOs
{
    public class EsewaPaymentRequest
    {
        public decimal TotalAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal ServiceCharge { get; set; } = 0;
        public decimal DeliveryCharge { get; set; } = 0;
        public string TransactionUuid { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string FailureUrl { get; set; } = string.Empty;
    }
}
