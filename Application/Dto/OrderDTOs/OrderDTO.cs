using Application.Dto.UserDTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.OrderDTOs
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }        
        public DateTime OrderDate { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal? EventDiscountAmount { get; set; } 
        public string? Notes { get; set; } 
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public decimal ShippingCost { get; set; }
        public int ShippingId { get; set; }
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }


        public string?ReasonToCancel { get; set; }
        public bool IsConfirmed { get; set; } = false;
        public bool IsCancelled { get; set; } = false;
        public bool IsDelivered { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public UserDTO? UserDTO { get; set; }

        public ICollection<OrderItemDTO> Items{ get; set; } = new List<OrderItemDTO>();

    }
}
