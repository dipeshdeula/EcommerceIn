using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs.EsewaDTOs
{
    public class EsewaCallbackRequest
    {
        public string? Oid { get; set; }
        public string? Amt { get; set; }
        public string? RefId { get; set; }
    }
}
