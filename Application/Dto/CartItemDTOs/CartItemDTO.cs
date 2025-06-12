using Application.Dto.ProductDTOs;

namespace Application.Dto.CartItemDTOs
{
    public class CartItemDTO
    {
        // CORE CART DATA 
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // CART-SPECIFIC PRICING (Locked-in at time of adding) 
        public decimal ReservedPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? EventDiscountAmount { get; set; }
        public int? AppliedEventId { get; set; }

        // STOCK RESERVATION 
        public bool IsStockReserved { get; set; }
        public string? ReservationToken { get; set; }
        public DateTime ExpiresAt { get; set; }

        // AUDIT TRAIL 
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }

        //  COMPUTED PROPERTIES (Business logic) 
        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
        public bool IsActive => !IsDeleted && !IsExpired && IsStockReserved;
        public decimal TotalItemPrice => ReservedPrice * Quantity;
        public decimal TotalDiscountAmount => (EventDiscountAmount ?? 0) * Quantity;
        public decimal OriginalTotalPrice => (OriginalPrice ?? ReservedPrice) * Quantity;


        // DISPLAY PROPERTIES
        public string FormattedOriginalPrice => $"Rs. {OriginalTotalPrice:F2}";

        public string FormattedReservedPrice => $"Rs. {ReservedPrice:F2}";
        public string FormattedTotalPrice => $"Rs. {TotalItemPrice:F2}";
        public string FormattedDiscount => TotalDiscountAmount > 0 ? $"Save Rs. {TotalDiscountAmount:F2}" : string.Empty;
        public decimal TotalSavingsAmount => ((OriginalPrice ?? ReservedPrice) - ReservedPrice) * Quantity;
        public string FormattedEventDiscount => EventDiscountAmount.HasValue && EventDiscountAmount > 0
          ? $"Event Discount: Rs. {(EventDiscountAmount.Value * Quantity):F2}"
          : string.Empty;



        //  STATUS TRACKING 
        public string Status => IsExpired ? "Expired" : IsActive ? "Reserved" : IsDeleted ? "Removed" : "Invalid";
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
        public ProductDTO? Product { get; set; }
        public UserDTO? User { get; set; }
    }
}