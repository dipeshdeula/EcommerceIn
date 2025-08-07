using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.StoreDTOs
{
    public class StoreTransactionDTO
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Transaction Summary
        public int TotalOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }

        // Transaction Details
        public List<StoreTransactionDetailDTO> TransactionDetails { get; set; } = new();

        // Summary Statistics
        public Dictionary<string, object> Statistics { get; set; } = new();
    }

    public class StoreTransactionDetailDTO
    {
         public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        
        // Product Details
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        
        // Transaction Details
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        
        //  Separate discount tracking
        public decimal EventDiscountApplied { get; set; }
        public decimal RegularDiscountApplied { get; set; }
        public decimal DiscountApplied { get; set; } // Total discount
        public decimal NetAmount { get; set; }
        
        // Status Information
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? DeliveredDate { get; set; }
        
        // Additional Information
        public bool HasDiscount { get; set; }
        public bool HasEventDiscount { get; set; }
        public bool HasRegularDiscount { get; set; }
        public int? AppliedEventId { get; set; }
        
        // Formatted Values
        public string FormattedUnitPrice { get; set; } = string.Empty;
        public string FormattedTotalPrice { get; set; } = string.Empty;
        public string FormattedNetAmount { get; set; } = string.Empty;
        public string FormattedEventDiscount { get; set; } = string.Empty;
        public string FormattedRegularDiscount { get; set; } = string.Empty;
    }
}
