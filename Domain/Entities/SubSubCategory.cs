using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class SubSubCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        public int SubCategoryId { get; set; } // Foreign key to SubCategory
        public SubCategory SubCategory { get; set; }  // Navigation property to SubCategory entity

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
