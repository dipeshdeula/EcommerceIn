using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.PaymentDTOs
{
    public class KhaltiResponseDto
    {
        [JsonPropertyName("pidx")]
        public string Pidx { get; set; }

        [JsonPropertyName("payment_url")]
        public string PaymentUrl { get; set; }
    }
}
