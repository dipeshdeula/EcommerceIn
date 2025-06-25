using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.AdminDashboardDTOs
{
    public class TopProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int SoldQuantity { get; set; }
        public decimal TotalSales { get; set; }
    }
}
