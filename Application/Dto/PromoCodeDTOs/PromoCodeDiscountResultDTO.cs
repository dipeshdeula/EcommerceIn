using Domain.Enums;

namespace Application.Dto.PromoCodeDTOs
{
    public class PromoCodeDiscountResultDTO
    {
        //  BASIC VALIDATION
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        
        //  PROMO CODE INFO
        public int? PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }
        public PromoCodeType? Type { get; set; }
        
        //  FINANCIAL BREAKDOWN
        public decimal OriginalSubtotal { get; set; }
        public decimal OriginalShipping { get; set; }
        public decimal OriginalTotal { get; set; }
        
        public decimal DiscountAmount { get; set; }
        public decimal ShippingDiscount { get; set; }
        
        public decimal FinalSubtotal { get; set; }
        public decimal FinalShipping { get; set; }
        public decimal FinalTotal { get; set; }
        
        //  CART INTEGRATION
        /// <summary>Cart items affected by this promo code</summary>
        public List<CartItemDiscountDTO> AffectedCartItems { get; set; } = new();
        
        /// <summary>Total number of cart items that qualify for this discount</summary>
        public int QualifyingItemsCount { get; set; }
        
        /// <summary>Category IDs that qualify for this promo code</summary>
        public List<int> QualifyingCategoryIds { get; set; } = new();
        
        //  USAGE INFO
        public bool AppliedToShipping { get; set; }
        public bool CanStackWithEvents { get; set; }
        public int? RemainingUsage { get; set; }
        public int? UserRemainingUsage { get; set; }
        
        //  DISPLAY MESSAGES
        public string FormattedDiscount { get; set; } = string.Empty;
        public string FormattedSavings { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        
        //  TIMESTAMP INFO
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string TimeZoneInfo { get; set; } = "NPT";
    }
    
    /// <summary>Represents discount applied to individual cart item</summary>
    public class CartItemDiscountDTO
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalSavings { get; set; }
    }
}