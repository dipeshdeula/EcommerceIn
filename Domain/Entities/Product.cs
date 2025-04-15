using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; } // e.g. "home-appliances"
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; } // Nullable if no discount
        public int StockQuantity { get; set; }
        public bool IsDeleted { get; set; } = false;

        public int CategoryId { get; set; } // Foreign key to Category

        public Category Category { get; set; } // Navigation property to Category entity
        public ICollection<ProductImage> Images { get; set; }
    }
}
