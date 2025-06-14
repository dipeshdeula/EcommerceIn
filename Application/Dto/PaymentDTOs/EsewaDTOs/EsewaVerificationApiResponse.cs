using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs.EsewaDTOs
{
    public class EsewaVerificationApiResponse
    {
        public string Product_code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Transaction_uuid { get; set; } = string.Empty;
        public string Total_amount { get; set; } = string.Empty;
        public string ReferenceId { get; set; } = string.Empty;
    }
}
