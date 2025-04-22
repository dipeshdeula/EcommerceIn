using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // e.g. "Pending", "Shipped", "Delivered", "Cancelled"
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; }

        public bool IsDeleted { get; set; } = false;

        public User User { get; set; }
        public ICollection<OrderItem> Items { get; set; }

    }
}
