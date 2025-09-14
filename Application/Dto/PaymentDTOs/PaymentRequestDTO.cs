using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentRequestDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }

        public int PaymentMethodId { get; set; } // esewa,khalti,cash on delivery

        public decimal PaymentAmount { get; set; }

        public string Currency { get; set; } = "NPR";
        public string Description { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        public string? PaymentUrl { get; set; }
        public string? KhaltiPidx { get; set; } // For khalti
        public string? EsewaTransactionId { get; set; } // For esewa

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? ExpiresAt { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public bool RequiresRedirect { get; set; }

        [JsonIgnore]
        public Dictionary<string,string> Metadata { get; set; }
        public string? UserName { get; set; }
        public string? PaymentMethodName { get; set; }
        public decimal? OrderTotal { get; set; }

    }
}
