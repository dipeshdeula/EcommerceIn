using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class CreateProductDTO
    {
        public string Name { get; set;}
        public string Slug { get; set; }
        public string Description { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string Sku { get; set; }
        public string Weight { get; set; }
        public int Reviews { get; set; }
        public decimal Rating { get; set; }
        public string Dimensions { get; set; }
    }
}
