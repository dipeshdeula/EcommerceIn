using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PaymentRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }

        public int PaymentMethodId { get; set; } // esewa,khalti,cash on delivery

        public string? PaymentUrl { get; set; }
        public string PaymentStatus { get; set; } = "Pending";// e.g ,. "Pending","Succeeded","Failed"

        public decimal PaymentAmount { get; set; }

        public string Currency { get; set; } = "NPR";
        public string? Description { get; set; }

        public string? KhaltiPidx { get; set; } // For khalti
        public string? EsewaTransactionId { get; set; } // For esewa


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Order? Order { get; set; }
        public PaymentMethod? PaymentMethod { get; set; } // Navigation property to PaymentMethod
    }
}
