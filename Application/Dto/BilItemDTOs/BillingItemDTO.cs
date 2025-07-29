using Domain.Entities;

namespace Application.Dto.BilItemDTOs
{
    public class BillingItemDTO
    {
        public int Id { get; set; }
        public int BillingId { get; set; }       

        // Snapshot fields for data integrity
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Price per unit at billing time
        public decimal TotalPrice { get; set; } // UnitPrice * Quantity
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
