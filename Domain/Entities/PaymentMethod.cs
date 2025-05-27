using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PaymentMethod
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public PaymentMethodType Type { get; set; } // enum : 1 = Esewa, 2 = Khalit , 3 = COD

        public string Logo { get; set; } 
        public bool IsDeleted { get; set; }
        public ICollection<PaymentRequest> PaymentRequests { get; set; }
    }
}
