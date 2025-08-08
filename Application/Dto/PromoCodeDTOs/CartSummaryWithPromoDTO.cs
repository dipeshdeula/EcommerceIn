using Domain.Enums;

namespace Application.Dto.PromoCodeDTOs
{
    public class CartSummaryWithPromoDTO
    {
        public int UserId { get; set; }
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        
        //  FINANCIAL BREAKDOWN
        public decimal OriginalSubtotal { get; set; }
        public decimal RegularDiscounts { get; set; }
        public decimal EventDiscounts { get; set; }
        public decimal PromoCodeDiscounts { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal FinalSubtotal { get; set; }
        
        //  APPLIED PROMO CODES
        public List<AppliedPromoCodeSummaryDTO> AppliedPromoCodes { get; set; } = new();
        public bool HasActivePromoCodes => AppliedPromoCodes.Any();
        public int ActivePromoCodesCount => AppliedPromoCodes.Count;
        
        //  CART ITEMS
        public List<CartItemWithPromoDTO> Items { get; set; } = new();
        
        //  VALIDATION
        public bool CanCheckout { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool HasExpiredItems { get; set; }
        public bool HasOutOfStockItems { get; set; }
        
        //  TIMESTAMPS
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? EarliestExpiration { get; set; }
    }
    
    public class AppliedPromoCodeSummaryDTO
    {
        public int PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PromoCodeType Type { get; set; }
        public decimal TotalDiscount { get; set; }
        public int AffectedItemsCount { get; set; }
        public bool AppliedToShipping { get; set; }
        public DateTime AppliedAt { get; set; }
        public string FormattedDiscount { get; set; } = string.Empty;
    }
    
    public class CartItemWithPromoDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        
        //  PRICING BREAKDOWN
        public decimal OriginalPrice { get; set; }
        public decimal RegularDiscountAmount { get; set; }
        public decimal EventDiscountAmount { get; set; }
        public decimal PromoCodeDiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal TotalSavings { get; set; }
        
        //  APPLIED DISCOUNTS
        public int? AppliedEventId { get; set; }
        public string? AppliedEventName { get; set; }
        public int? AppliedPromoCodeId { get; set; }
        public string? AppliedPromoCode { get; set; }
        
        //  STATUS
        public bool HasPromoDiscount { get; set; }
        public bool HasEventDiscount { get; set; }
        public bool IsExpired { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}