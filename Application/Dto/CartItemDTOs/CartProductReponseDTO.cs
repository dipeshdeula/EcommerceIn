using Application.Dto.ProductDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class CartProductReponseDTO
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();
    }
}
