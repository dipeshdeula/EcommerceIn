using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentInitiationResponse
    {
        public string Provider { get; set; } = string.Empty;
        public string? PaymentUrl { get; set; }
        public string? PaymentFormHtml { get; set; }
        public string? ProviderTransactionId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool RequiresRedirect { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
