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
        public double MarketPrice { get; set; }
        public double CostPrice { get; set; }
        public double DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string Sku { get; set; }
        public string Weight { get; set; }
        public int Reviews { get; set; }
        public double Rating { get; set; }
        public string Dimensions { get; set; }
    }
}
