﻿using Application.Dto.UserDTOs;
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
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string ShippingCity { get; set; }

        public string ReasonToCancel { get; set; }

        public bool IsCancelled { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public UserDTO? UserDTO { get; set; }

        public ICollection<OrderItemDTO> Items { get; set; }

    }
}
