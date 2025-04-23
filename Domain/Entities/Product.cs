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
        public double Price { get; set; }
        public double? DiscountPrice { get; set; } // Nullable if no discount
        public int StockQuantity { get; set; }
        public bool IsDeleted { get; set; } = false;



        // Foreign key to SubSubCategory
        public int SubSubCategoryId { get; set; }
        public SubSubCategory SubSubCategory { get; set; } // Navigation property to SubSubCategory

        // Navigation property for product images
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // Navigation property for product stores
        public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
    }
}
