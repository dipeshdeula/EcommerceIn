using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; } = false; // Indicates if this image is the main image for the product
        public bool IsDeleted { get; set; } = false;

        public Product Product { get; set; } // Navigation property to Product entity
    }
}
