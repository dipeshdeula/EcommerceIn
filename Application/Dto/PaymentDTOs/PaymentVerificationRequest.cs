using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentVerificationRequest
    {
        public int PaymentRequestId { get; set; }
        public string? PaymentStatus { get; set; }
        public string? EsewaTransactionId { get; set; }
        public string? KhaltiPidx { get; set; }
        public int? DeliveryPersonId { get; set; } // For COD
        public string? DeliveryNotes { get; set; } // For COD
        public decimal? CollectedAmount { get; set; } // For COD
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }
}
