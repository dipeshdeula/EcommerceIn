using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // e.g. "electronics"
        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public ICollection<SubSubCategory> SubSubCategories { get; set; } = new List<SubSubCategory>();

        public ICollection<Product> Products { get; set; } = new List<Product>();



    }
}
