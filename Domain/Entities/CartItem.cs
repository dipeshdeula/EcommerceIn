using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class CartItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        //  PRICING WITH PROMO CODE SUPPORT
        public decimal OriginalPrice { get; set; } // Original product price (backup)
        public decimal ReservedPrice { get; set; } // Current effective price (after all discounts)
        public decimal RegularDiscountAmount { get; set; } // Product's regular discount
        
        //  EVENT INTEGRATION (Existing)
        public int? AppliedEventId { get; set; }
        public decimal? EventDiscountAmount { get; set; }
        public decimal? EventDiscountPercentage { get; set; }
        
        //  PROMO CODE INTEGRATION (New)
        public int? AppliedPromoCodeId { get; set; }
        public decimal? PromoCodeDiscountAmount { get; set; } // Per unit promo discount
        public string? AppliedPromoCode { get; set; } // Store code for reference
        
        //  TIMESTAMPS
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActivityAt { get; set; }

        //  CART EXPIRATION
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public bool IsStockReserved { get; set; } = false;
        public string? ReservationToken { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsExpired => ExpiresAt < DateTime.UtcNow;

        //  NAVIGATION PROPERTIES
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual BannerEventSpecial? AppliedEvent { get; set; }
        public virtual PromoCode? AppliedPromoCode_Navigation { get; set; } // Avoid naming conflict
        
        //  COMPUTED PROPERTIES
        public decimal TotalPrice => ReservedPrice * Quantity;
        public decimal TotalSavings => (OriginalPrice - ReservedPrice) * Quantity;
        public bool HasPromoDiscount => AppliedPromoCodeId.HasValue && PromoCodeDiscountAmount > 0;
        public bool HasEventDiscount => AppliedEventId.HasValue && EventDiscountAmount > 0;
        
        //  HELPER METHODS
        public void ApplyPromoCode(PromoCode promoCode, decimal discountPerUnit)
        {
            if (OriginalPrice == 0) OriginalPrice = ReservedPrice; // Backup current price
            
            AppliedPromoCodeId = promoCode.Id;
            AppliedPromoCode = promoCode.Code;
            PromoCodeDiscountAmount = discountPerUnit;
            ReservedPrice = Math.Max(0, ReservedPrice - discountPerUnit); // Don't go below 0
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void RemovePromoCode()
        {
            if (OriginalPrice > 0)
            {
                ReservedPrice = OriginalPrice; // Restore original price
            }
            
            AppliedPromoCodeId = null;
            AppliedPromoCode = null;
            PromoCodeDiscountAmount = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}