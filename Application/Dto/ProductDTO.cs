using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; } // e.g. "home-appliances"
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public double? DiscountPrice { get; set; } // Nullable if no discount
        public int StockQuantity { get; set; }
        public bool IsDeleted { get; set; } = false;


        // Navigation property for product images
        public ICollection<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();
    }
}
