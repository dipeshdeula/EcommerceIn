using Domain.Enums;

namespace Application.Dto.PromoCodeDTOs
{
    public class OrderPricingSummaryDTO
    {
        //  ORDER BREAKDOWN
        public decimal OriginalSubtotal { get; set; }
        public decimal OriginalShipping { get; set; }
        public decimal OriginalTax { get; set; }
        public decimal OriginalTotal { get; set; }
        
        //  DISCOUNT BREAKDOWN
        public decimal ProductDiscounts { get; set; }
        public decimal EventDiscounts { get; set; }
        public decimal PromoCodeDiscounts { get; set; }
        public decimal ShippingDiscounts { get; set; }
        public decimal TotalDiscounts { get; set; }
        
        //  FINAL AMOUNTS
        public decimal FinalSubtotal { get; set; }
        public decimal FinalShipping { get; set; }
        public decimal FinalTax { get; set; }
        public decimal FinalTotal { get; set; }
        
        //  APPLIED PROMO CODES
        public List<OrderPromoCodeSummaryDTO> AppliedPromoCodes { get; set; } = new();
        public decimal TotalPromoSavings => AppliedPromoCodes.Sum(p => p.DiscountAmount);
        
        //  CART ITEMS SUMMARY
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public List<OrderItemPricingDTO> Items { get; set; } = new();
        
        //  VALIDATION
        public bool IsValidForCheckout { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        
        //  METADATA
        public string Currency { get; set; } = "NPR";
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public string TimeZone { get; set; } = "NPT";
    }
    
    public class OrderPromoCodeSummaryDTO
    {
        public int PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PromoCodeType Type { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingDiscount { get; set; }
        public bool AppliedToShipping { get; set; }
        public int AffectedItemsCount { get; set; }
        public string FormattedDiscount { get; set; } = string.Empty;
    }
    
    public class OrderItemPricingDTO
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal FinalLineTotal { get; set; }
        
        public bool HasPromoDiscount { get; set; }
        public bool HasEventDiscount { get; set; }
        public string? AppliedPromoCode { get; set; }
        public string? AppliedEvent { get; set; }
    }
}