using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class KhaltiInitiateResponse
    {
        public string Pidx { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
