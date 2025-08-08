using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.PromoCodeDTOs
{
    public class ApplyPromoCodeDTO
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        [Required]
        public int UserId { get; set; }
        public decimal? OrderTotal { get; set; }
        public decimal? ShippingCost { get; set; }
        public string? CustomerTier { get; set; }
        public bool IsCheckout { get; set; } = false;
        public bool UpdateCartPrices { get; set; } = false;
    }

}
