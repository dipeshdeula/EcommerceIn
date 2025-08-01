using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class CartPriceRequestDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? MaxAcceptablePrice { get; set; }
        public int? PreferredEventId { get; set; }
        public DateTime? LastPriceCheck { get; set; }
    }
}
