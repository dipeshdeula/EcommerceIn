using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class KhaltiPaymentRequest
    {
        public int Amount { get; set; } // Amount in paisa
        public string ReturnUrl { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string PurchaseOrderId { get; set; } = string.Empty;
        public string PurchaseOrderName { get; set; } = string.Empty;
        public KhaltiCustomerInfo CustomerInfo { get; set; } = new();
    }
}
