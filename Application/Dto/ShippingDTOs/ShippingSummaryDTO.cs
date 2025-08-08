using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ShippingDTOs
{
    public class ShippingSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFreeShippingActive { get; set; }
        public string FreeShippingDescription { get; set; } = string.Empty;
    }
}
