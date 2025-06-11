using Application.Dto.PaymentMethodDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class PaymentVerificationDto
    {
        public int PaymenetRequestId { get; set; }
        public PaymentMethodDTO PaymentType { get; set; }
        public string Status { get; set; }
        public string? EsewaTransactionId { get; set; }
        public string? KhaltiPidx { get; set; }
    }
}
