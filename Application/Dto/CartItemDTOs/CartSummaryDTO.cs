using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class CartSummaryDTO
    {
        // ===== BASIC CART INFO =====
        public int UserId { get; set; }
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }

        // ===== PRICING BREAKDOWN (Following your pricing pattern) =====
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal EstimatedTotal { get; set; }

        // ===== CART STATUS (Following your banner event validation pattern) =====
        public bool CanCheckout { get; set; }
        public bool HasExpiredItems { get; set; }
        public bool HasOutOfStockItems { get; set; }

        // ===== VALIDATION (Following your Result pattern) =====
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => !ValidationErrors.Any() && CanCheckout;

        // ===== CART ITEMS =====
        public List<CartItemDTO> Items { get; set; } = new();

        // ===== EXPIRATION INFO (Business logic) =====
        public DateTime? EarliestExpiration { get; set; }
        public int ExpiredItemsCount { get; set; }

        // ===== DISPLAY PROPERTIES (Following your formatting style) =====
        public string FormattedSubTotal => $"Rs. {SubTotal:F2}";
        public string FormattedTotalDiscount => $"Rs. {TotalDiscount:F2}";
        public string FormattedEstimatedTotal => $"Rs. {EstimatedTotal:F2}";
        public string FormattedSavings => TotalDiscount > 0 ? $"You Save Rs. {TotalDiscount:F2}" : string.Empty;
    }
}
