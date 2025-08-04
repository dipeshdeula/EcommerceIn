
using Domain.Enums;

namespace Domain.Entities
{
    public class PaymentMethod
    {
        /*public int Id { get; set; }

        public string ProviderName { get; set; }
        public PaymentMethodType Type { get; set; } // enum : 1 = DigitalPayments, 2 = Khalit , 3 = COD

        public string Logo { get; set; } 
        public bool IsDeleted { get; set; }
        public ICollection<PaymentRequest> PaymentRequests { get; set; }*/

        public int Id { get; set; }
        public string ProviderName { get; set; } = string.Empty; // "Esewa", "Khalti", "COD"
        public PaymentMethodType Type { get; set; } // DigitalPayments, COD, etc.        
        public bool IsActive { get; set; } = true;
        public bool RequiresRedirect { get; set; } = true;     
        public string? Logo { get; set; }      
        public string? SupportedCurrencies { get; set; } = "NPR";
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();
    }
}
