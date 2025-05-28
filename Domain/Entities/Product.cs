using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public double MarketPrice { get; set; }
        public double CostPrice { get; set; }

        public double? DiscountPrice { get; set; } // Nullable if no discount
        public int StockQuantity { get; set; } // Total stock
        public int ReservedStock { get; set; } // Reserved stock for orders
        public string Sku { get; set; }
        public string Weight { get; set; }
        public int Reviews { get; set; }
        public double Rating { get; set; }
        public string Dimensions { get; set; }
        public bool IsDeleted { get; set; } = false;


        // Computed property for available stock (not mapped to the database)
        public int AvailableStock => StockQuantity - ReservedStock;

        //for sql server
        /*
                [Timestamp]
                public byte[] RowVersion { get; set; }*/

        // Replace RowVersion with PostgreSQL-compatible concurrency token
        public uint Version { get; set; } // Changed to uint for xmin compatibility



        // Foreign key to SubSubCategory
        public int SubSubCategoryId { get; set; }
        public SubSubCategory SubSubCategory { get; set; } // Navigation property to SubSubCategory

        // New: Foreign key to Category
        public int CategoryId { get; set; }
        public Category Category { get; set; } // Navigation property to Category

        // Navigation property for product images
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        // Navigation property for product stores
        public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
    }
}
