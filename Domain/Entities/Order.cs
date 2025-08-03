namespace Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string OrderStatus { get; set; } = "Pending"; // e.g. "Pending", "Shipped", "Delivered", "Cancelled"
        public string PaymentStatus { get; set; } = "Pending";  // e.g "Pending", "Paid"
        public decimal? EventDiscountAmount { get; set; }
        public string? Notes { get; set; } = null; // Optional notes for the order
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; } = false;
        public bool IsCancelled { get; set; } = false;
        public string? ReasonToCancel { get; set; }

        public bool IsDeleted { get; set; } = false;    
        public User User { get; set; }
        public ICollection<OrderItem> Items { get; set; }
        public ICollection<Notification> Notifications { get; set; } = 
        new List<Notification>();

    }
}
