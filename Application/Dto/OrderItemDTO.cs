using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public class OrderItemDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; } // Foreign key to Order
        public int ProductId { get; set; } // Foreign key to Product
        public int Quantity { get; set; } // Quantity of the product in the order
        public double UnitPrice { get; set; } // Price per unit at the time of order
        public double TotalPrice { get; private set; } // Total price for this order item

        /*public OrderDTO Order { get; set; } // Navigation property to Order entity
        public ProductDTO Product { get; set; } // Navigation property to Product entity*/
    }
}
