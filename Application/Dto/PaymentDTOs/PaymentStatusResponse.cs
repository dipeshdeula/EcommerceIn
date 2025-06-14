using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentStatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> ProviderData { get; set; } = new();
    
        public string? FailureReason { get; set; }
    }
}
