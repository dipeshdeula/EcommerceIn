using Application.Dto.UserDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentContextDto
    {
       public PaymentRequestDTO PaymentRequest { get; set; }
       public UserDTO User { get; set; }

    }
}
