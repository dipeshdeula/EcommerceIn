using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.AdminDashboardDTOs
{
    public class TopStoreDTO
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }

        public int OrderCount { get; set; }
        public int ProductCount { get; set; }
    }
}
