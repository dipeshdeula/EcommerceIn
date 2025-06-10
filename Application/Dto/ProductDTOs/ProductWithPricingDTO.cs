using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class ProductWithPricingDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;

        // Original pricing
        public decimal MarketPrice { get; set; }
        public decimal? DiscountPrice { get; set; }

        // Effective pricing (with banner events)
        public decimal EffectivePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public bool HasActivePromotion { get; set; }

        // Event information
        public int? ActiveEventId { get; set; }
        public string? ActiveEventName { get; set; }
        public string? EventTagLine { get; set; }

        // Stock and other info
        public int StockQuantity { get; set; }
        public decimal Rating { get; set; }
        public int Reviews { get; set; }

        public List<ProductImageDTO> Images { get; set; } = new();

    }
}
