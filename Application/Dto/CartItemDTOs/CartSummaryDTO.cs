using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.CartItemDTOs
{
    public class CartSummaryDTO
    {
        //  BASIC CART INFO 
        public int UserId { get; set; }
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }

        //  PRICING BREAKDOWN
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal EstimatedTotal { get; set; }

        //  CART STATUS 
        public bool CanCheckout { get; set; }
        public bool HasExpiredItems { get; set; }
        public bool HasOutOfStockItems { get; set; }

        //  VALIDATION 
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => !ValidationErrors.Any() && CanCheckout;

        //  CART ITEMS 
        public List<CartItemDTO> Items { get; set; } = new();
        public int ActiveItemsCount { get; set; }
        public int ExpiredItemsCount { get; set; }

        //  EXPIRATION INFO 
        public DateTime? EarliestExpiration { get; set; }
        public DateTime? LastUpdated { get; set; }

        // Performance tracking properties
        public DateTime CacheCalculatedAt { get; set; } = DateTime.UtcNow;
        public bool IsCacheData { get; set; } = false;
        public double CalculationTimeMs { get; set; }

        // Enhanced cart validation
        public decimal TotalWeight { get; set; }
        public int DistinctProductCount { get; set; }
        public bool HasEventItems { get; set; }
        public decimal EventSavings { get; set; }

        //  DISPLAY PROPERTIES 
        public string FormattedSubTotal => $"Rs. {SubTotal:F2}";
        public string FormattedTotalDiscount => $"Rs. {TotalDiscount:F2}";
        public string FormattedEstimatedTotal => $"Rs. {EstimatedTotal:F2}";
        public string FormattedSavings => TotalDiscount > 0 ? $"You Save Rs. {TotalDiscount:F2}" : string.Empty;

        public string FormattedEventSavings => EventSavings > 0 ? $"Event Savings: Rs. {EventSavings:F2}" : string.Empty;
        public string CartStatusText => GetCartStatusText();
        public string ExpirationWarning => GetExpirationWarning();
        
        // Helper methods
         private string GetCartStatusText()
        {
            if (!Items.Any()) return "Cart is empty";
            if (HasExpiredItems && !ActiveItemsCount.Equals(0)) return "Some items have expired";
            if (HasOutOfStockItems) return "Some items are out of stock";
            if (!CanCheckout) return "Cart needs attention";
            return "Ready for checkout";
        }

        private string GetExpirationWarning()
        {
            if (!EarliestExpiration.HasValue) return string.Empty;
            
            var timeUntilExpiry = EarliestExpiration.Value - DateTime.UtcNow;
            if (timeUntilExpiry.TotalMinutes < 30)
                return $" Items expire in {timeUntilExpiry.Minutes} minutes";
            if (timeUntilExpiry.TotalHours < 2)
                return $" Items expire in {timeUntilExpiry.Hours} hours";
            
            return string.Empty;
        }

        //  Create summary from cart items (static factory method)
        public static CartSummaryDTO CreateFromCartItems(
            int userId, 
            List<CartItemDTO> cartItems, 
            double calculationTimeMs = 0,
            bool isCacheData = false)
        {
            var activeItems = cartItems.Where(c => !c.IsExpired).ToList();
            var expiredItems = cartItems.Where(c => c.IsExpired).ToList();
            
            var subTotal = activeItems.Sum(c => c.ReservedPrice * c.Quantity);
            var totalDiscount = activeItems.Sum(c => (c.EventDiscountAmount ?? 0) * c.Quantity);
            var eventSavings = activeItems.Where(c => c.AppliedEventId.HasValue).Sum(c => (c.EventDiscountAmount ?? 0) * c.Quantity);

            var validationErrors = new List<string>();
            
            // Validate cart items
            foreach (var item in activeItems)
            {
                if (item.Product?.StockQuantity < item.Quantity)
                    validationErrors.Add($"{item.Product?.Name} has insufficient stock");
                
                if (item.IsExpired)
                    validationErrors.Add($"{item.Product?.Name} has expired");
            }

            return new CartSummaryDTO
            {
                UserId = userId,
                TotalItems = cartItems.Count,
                TotalQuantity = cartItems.Sum(c => c.Quantity),
                SubTotal = subTotal,
                TotalDiscount = totalDiscount,
                EstimatedTotal = subTotal, // After discounts are already applied to ReservedPrice
                CanCheckout = activeItems.Any() && !validationErrors.Any(),
                HasExpiredItems = expiredItems.Any(),
                HasOutOfStockItems = activeItems.Any(c => c.Product?.StockQuantity < c.Quantity),
                ValidationErrors = validationErrors,
                Items = cartItems,
                ActiveItemsCount = activeItems.Count,
                ExpiredItemsCount = expiredItems.Count,
                EarliestExpiration = cartItems.Where(c => c.ExpiresAt.ToString() != null).Min(c => c.ExpiresAt),
                LastUpdated = DateTime.UtcNow,
                CacheCalculatedAt = DateTime.UtcNow,
                IsCacheData = isCacheData,
                CalculationTimeMs = calculationTimeMs,
                TotalWeight = activeItems.Sum(c => c.Product?.Weight != null ? decimal.Parse(c.Product.Weight.Replace("g", "")) * c.Quantity : 0),
                DistinctProductCount = cartItems.Select(c => c.ProductId).Distinct().Count(),
                HasEventItems = activeItems.Any(c => c.AppliedEventId.HasValue),
                EventSavings = eventSavings
            };
        }

    }
}
