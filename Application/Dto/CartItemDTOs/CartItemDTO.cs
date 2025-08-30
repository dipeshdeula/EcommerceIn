using Application.Dto.CategoryDTOs;
using Application.Dto.ProductDTOs;
using Application.Dto.PromoCodeDTOs;
using Application.Dto.ShippingDTOs;
using Application.Dto.UserDTOs;
using System.Text.Json.Serialization;

namespace Application.Dto.CartItemDTOs
{
    public class CartItemDTO
    {
        // CORE CART DATA 
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }     
        //public decimal ShippingCost { get; set; }
        public int Quantity { get; set; }

        // CART-SPECIFIC PRICING (Locked-in at time of adding) 
        public decimal ReservedPrice { get; set; }
        public decimal? OriginalPrice { get; set; }

        [JsonIgnore]
        public decimal? EventDiscountAmount { get; set; }
        [JsonIgnore]
        public decimal PromoCodeDiscountAmount { get; set; }
        // public string? AppliedPromoCode { get; set; }

        //public decimal ShippingCost { get; set; }

        // STOCK RESERVATION 
        public bool IsStockReserved { get; set; }        
        public DateTime ExpiresAt { get; set; }

        // AUDIT TRAIL 
        // public DateTime CreatedAt { get; set; }
        //public DateTime UpdatedAt { get; set; }

        [JsonIgnore]
        public bool IsDeleted { get; set; }

        //  COMPUTED PROPERTIES (Business logic) 
        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
        public bool IsActive => !IsDeleted && !IsExpired && IsStockReserved;
        public decimal TotalItemPrice => ReservedPrice * Quantity   ;
        //public decimal TotalDiscountAmount => (EventDiscountAmount ?? 0) * Quantity;
        public decimal OriginalTotalPrice => OriginalPrice ?? ReservedPrice;


        

        //  STATUS TRACKING 
       // public string Status => IsExpired ? "Expired" : IsActive ? "Reserved" : IsDeleted ? "Removed" : "Invalid";
        public string TimeRemaining
        {
            get
            {
                if (IsExpired) return "Expired";
                var remaining = ExpiresAt - DateTime.UtcNow;
                return remaining.TotalHours > 24
                    ? $"{remaining.Days}d {remaining.Hours}h"
                    : $"{remaining.Hours}h {remaining.Minutes}m";
            }
        }

        //  NAVIGATION

        //public ProductDTO? Product { get; set; }
        public ShippingInfoDTO? shipping { get; set; }
        public CartProductReponseDTO? CartProduct { get; set; }
       
    }
}