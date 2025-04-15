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
        public string Slug { get; set; } // e.g. "home-appliances"
        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; }

        public int ParentCategoryId { get; set; }


    }
}
