using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class ProductStoreDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; } // Foreign key to Product
        public int StoreId { get; set; } // Foreign key to Store
        public bool IsDeleted { get; set; } = false; // Soft delete flag




    }
}
