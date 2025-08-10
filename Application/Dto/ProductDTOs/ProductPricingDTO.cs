using Application.Dto.Shared;
using Domain.Enums.BannerEventSpecial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.ProductDTOs
{
    public class ProductPricingDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        //  CORE PRICING 
        public decimal OriginalPrice { get; set; }      // Market price
        public decimal BasePrice { get; set; }          // After product discount
        public decimal EffectivePrice { get; set; }     // Final price after all discounts

        //  DISCOUNT BREAKDOWN 
        public decimal ProductDiscountAmount { get; set; }
        public decimal EventDiscountAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalDiscountPercentage { get; set; }
        public bool HasProductDiscount { get; set; }
        public bool HasEventDiscount { get; set; }
        public bool HasAnyDiscount { get; set; }
        public bool IsOnSale { get; set; }

        //  EVENT INFORMATION 
        public int? ActiveEventId { get; set; }
        public string? ActiveEventName { get; set; }
        public string? EventTagLine { get; set; }
        public PromotionType? PromotionType { get; set; }
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }

        //  EVENT STATUS 
        public bool HasActiveEvent { get; set; }
        public bool IsEventActive { get; set; }
        public TimeSpan? EventTimeRemaining { get; set; }
        public bool IsEventExpiringSoon { get; set; }

        //  FORMATTED DISPLAY 
        public string FormattedOriginalPrice { get; set; } = "";
        public string FormattedEffectivePrice { get; set; } = "";
        public string FormattedSavings { get; set; } = "";
        public string FormattedDiscountBreakdown { get; set; } = "";
        public string EventStatus { get; set; } = "";
        
         public bool HasFreeShipping { get; set; }

        //  METADATA
        public bool IsPriceStable { get; set; }
        public DateTime CalculatedAt { get; set; }
               
    }
}
