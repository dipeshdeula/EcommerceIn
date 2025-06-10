using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.OrderDTOs
{
    public class OrderPlacedEventDTO
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; }
        public string[] ProductNames { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
