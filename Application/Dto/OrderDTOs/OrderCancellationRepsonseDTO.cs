using Application.Dto.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.OrderDTOs
{
    public class OrderCancellationRepsonseDTO
    {
        public int OrderId { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string ReasonToCancel { get; set; } = string.Empty;
        public string EventUsageResult { get; set; } = string.Empty;
        public bool PreviousCancelled { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public int? EstimatedDeliveryMinutes { get; set; }
        public string Message { get; set; } = string.Empty;

        public NotificationResultDTO NotificationResult { get; set; } = new();
    }
}
