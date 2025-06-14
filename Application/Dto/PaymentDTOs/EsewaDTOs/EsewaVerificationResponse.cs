using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs.EsewaDTOs
{
    public class EsewaVerificationResponse
    {
        public bool IsSuccessful { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; }
        public EsewaVerificationApiResponse? ApiResponse { get; set; }
    }

}
