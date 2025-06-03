using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Payment
{
    public class AddPamentRequestDTO
    {
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public int PaymentMethodId { get; set; }
        public string? Description { get; set; }
    }
}
