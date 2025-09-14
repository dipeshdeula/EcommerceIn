using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.Dto.OrderDTOs
{
    public class OrderItemDTO
    {
        [JsonIgnore]
        public int Id { get; set; }
        [JsonIgnore]
        public int OrderId { get; set; } // Foreign key to Order
        public int ProductId { get; set; } // Foreign key to Product
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } // Quantity of the product in the order
        public decimal UnitPrice { get; set; } // Price per unit at the time of order
        public decimal TotalPrice { get; set; } // Total price for this order item

        public decimal? EventDiscountAmount { get; set; } 
        public decimal? RegularDiscountAmount { get; set; }

        //  ADDED: Formatted properties

        [JsonIgnore]
        public string FormattedUnitPrice => $"Rs.{UnitPrice:F2}";
        [JsonIgnore]
        public string FormattedTotalPrice => $"Rs.{TotalPrice:F2}";

        /*public OrderDTO Order { get; set; } // Navigation property to Order entity
        public ProductDTO Product { get; set; } // Navigation property to Product entity*/
    }
}
