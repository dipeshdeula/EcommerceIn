using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class CartItemDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public bool IsDeleted { get; set; }
        public UserDTO User { get; set; } //Navigation property to User entity
        public ProductDTO Product { get; set; } // Navigation property to Product entity
    }
}

