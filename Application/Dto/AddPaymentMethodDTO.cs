using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class AddPaymentMethodDTO
    {
       public string? Name { get; set; }
       public PaymentMethodType Type { get; set; }
    }
}
