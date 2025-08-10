using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PromoCodeUsage : BaseEntity
    {
        public int Id { get; set; }
        public int PromoCodeId { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }

        public decimal OrderTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }

        [StringLength(100)]
        public string UsageContext { get; set; } = "Order"; // "Cart", "Order", "Checkout"
       
        public DateTime UsedAt { get; set; }

        public string? UserEmail { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public PromoCode PromoCode { get; set; } = null!;
        public User User { get; set; } = null!;
        public Order? Order { get; set; }
    }
}
