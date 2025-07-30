using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.WhishListDTOs
{
    public class WishlistSummaryDTO
    {
        public int UserId { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalValue { get; set; }
        public List<WishlistDTO> Items { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
