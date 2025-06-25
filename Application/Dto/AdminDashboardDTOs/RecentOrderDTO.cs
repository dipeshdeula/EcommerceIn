using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.AdminDashboardDTOs
{
    public class RecentOrderDTO
    {
        public int OrderId { get; set; }
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
