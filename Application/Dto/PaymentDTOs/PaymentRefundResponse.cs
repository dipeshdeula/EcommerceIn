using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentRefundResponse
    {
        public bool IsSuccessful { get; set; }
        public string RefundId { get; set; } = string.Empty;
        public decimal RefundedAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RefundedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
