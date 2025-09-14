using Application.Dto.PaymentDTOs;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.PaymentMethodDTOs
{
    public class PaymentMethodDTO
    {
        public int Id { get; set; }

        public string ProviderName { get; set; }
        public PaymentMethodType Type { get; set; } // enum : 1 = DigitalPayments, 2 = Khalit , 3 = COD

        public string Logo { get; set; }
        public string? SupportedCurrencies { get; set; }
        public bool RequiresRedirect { get; set; }

        public bool IsAvailable { get; set; }

        [JsonIgnore]
        public ICollection<PaymentRequestDTO> PaymentRequests { get; set; }
    }
}
