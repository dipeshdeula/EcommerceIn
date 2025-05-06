using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; } // Foreign key to Order
        public int ProductId { get; set; } // Foreign key to Product
        public int Quantity { get; set; } // Quantity of the product in the order
        public double UnitPrice { get; set; } // Price per unit at the time of order
        public double TotalPrice => UnitPrice * Quantity; // Total price for this order item

        public Order Order { get; set; } // Navigation property to Order entity
        public Product Product { get; set; } // Navigation property to Product entity
    }

}
