using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ProductStore
    {
        public int Id { get; set; }
        public int ProductId { get; set; } // Foreign key to Product
        public int StoreId { get; set; } // Foreign key to Store
        public bool IsDeleted { get; set; } = false; // Soft delete flag

        public Product Product { get; set; } // Navigation property to Product entity
        public Store Store { get; set; } // Navigation property to Store entity

   
    }
}
