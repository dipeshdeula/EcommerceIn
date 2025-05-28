using MediatR.NotificationPublishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class CategoryWithProductsDTO
    {
        public int Id { get; set; } // Category ID
        public string Name { get; set; } // Category Name

        public string Slug { get; set; } // Category Slug
        public string Description { get; set; } // Category Description
        public IEnumerable<ProductDTO> Products { get; set; } // List of Products
    }
}
