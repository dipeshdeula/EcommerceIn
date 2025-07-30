using Application.Dto.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.OrderDTOs
{
    public class OrderCancelledDTO
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string ReasonToCancel { get; set; } = string.Empty;
        public bool IsCancelled { get; set; } = false;
        public NotificationResultDTO NotificationResult { get; set; } = new();


    }
}
