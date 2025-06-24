using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class AddToCartItemDTO
    {
        public int ProductId { get; set; }
        //public int UserId { get; set; }
        public int Quantity { get; set; }
    }
}
