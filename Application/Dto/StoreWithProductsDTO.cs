using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class StoreWithProductsDTO
    {
        public StoreDTO Store { get; set; } // Store details
        public List<ProductDTO> Products { get; set; } // List of associated products
    }
}
