using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentVerificationResponse
    {
        public bool IsSuccessful { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal? CollectedAmount { get; set; }
        public DateTime? CollectedAt { get; set; }
        public int? DeliveryPersonId { get; set; }
        public string? DeliveryNotes { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }
}
