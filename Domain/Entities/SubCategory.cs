using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class SubCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // e.g. "home-appliances"
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public int CategoryId { get; set; } // Foreign key to Category
        public Category? Category { get; set; } // Navigation property to Category entity

        public ICollection<SubSubCategory> SubSubCategories { get; set; } = new List<SubSubCategory>();
        public ICollection<Product> Products { get; set; } = new List<Product>();


    }
}
