using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class PaymentMethodDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public PaymentMethodType Type { get; set; } // enum : 1 = Esewa, 2 = Khalit , 3 = COD

        public string Logo { get; set; }
        public ICollection<PaymentRequestDTO> PaymentRequests { get; set; }
    }
}
