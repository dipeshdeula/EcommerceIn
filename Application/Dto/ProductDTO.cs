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
        public double MarketPrice { get; set; }
        public double CostPrice { get; set; }
        public double? DiscountPrice { get; set; } // Nullable if no discount
        public int StockQuantity { get; set; }
        public int ReservedStock { get; set; } // Reserved stock for orders
        public int AvailableStock => StockQuantity - ReservedStock;
        public string Sku { get; set; }
        public string Weight { get; set; }
        public int Reviews { get; set; }
        public double Rating { get; set; }


        public bool IsDeleted { get; set; } = false;


        // Navigation property for product images
        public ICollection<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();
    }
}
