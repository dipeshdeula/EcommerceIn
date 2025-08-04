using Application.Dto.PaymentDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PaymentMethodDTOs
{

        public class PaymentMethodResponseDTO
        {
            public int Id { get; set; }
            public string ProviderName { get; set; } = string.Empty;            
            public string Type { get; set; } = string.Empty;
            public bool RequiresRedirect { get; set; }
            public string? Logo { get; set; }          
            public string[] SupportedCurrencies { get; set; } = Array.Empty<string>();
            public bool IsAvailable { get; set; }

            public ICollection<PaymentRequestDTO> PaymentRequests { get; set; } = new List<PaymentRequestDTO>();

    }

}
