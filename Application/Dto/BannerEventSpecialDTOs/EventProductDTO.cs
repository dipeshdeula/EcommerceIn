using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.BannerEventSpecialDTOs
{
    public class EventProductDTO
    {
        public int Id { get; set; }
        public int BannerEventId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal? SpecificDiscount { get; set; }
        public bool IsActive { get; set; }
    }
}
