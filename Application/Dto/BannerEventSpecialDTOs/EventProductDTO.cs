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
        public DateTime AddedAt { get; set; }
        public decimal ProductMarketPrice { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public decimal CalculatedDiscountPrice { get; set; }
        public string FormattedSpecificDiscount => SpecificDiscount.HasValue ? $"Rs.{SpecificDiscount:F2} OFF" : "Event Discount";
        public bool HasSpecificDiscount => SpecificDiscount.HasValue;

    }
}
