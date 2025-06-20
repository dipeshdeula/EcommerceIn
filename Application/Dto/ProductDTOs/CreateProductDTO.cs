using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class CreateProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MarketPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPercentage { get; set; } // percentage discount (0-100)
        public int StockQuantity { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public int Reviews { get; set; }
        public decimal Rating { get; set; }
        public string Dimensions { get; set; } = string.Empty;
    }
}
