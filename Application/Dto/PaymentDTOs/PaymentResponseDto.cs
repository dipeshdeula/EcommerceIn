using Application.Dto.PaymentMethodDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentResponseDto
    {
        public int PaymentRequestId { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string Description { get; set; }
        public string PaymentUrl { get; set; }
        public string PaymentStatus { get; set; }
        public PaymentMethodDTO PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }

    }
}
