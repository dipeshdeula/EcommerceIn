using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class CartItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Event Integration
        public int? AppliedEventId { get; set; }
        public decimal? ReservedPrice { get; set; }
        public decimal? EventDiscountAmount { get; set; }
        public decimal? EventDiscountPercentage { get; set; }

        // Pricing snapshot fields

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActivityAt { get; set; }


        // Cart Expiration
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public bool IsStockReserved { get; set; } = false;
        public string? ReservationToken { get; set; }
        public bool IsDeleted { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

        [NotMapped]
        public bool IsActive => !IsDeleted && !IsExpired && IsStockReserved;

        [NotMapped]
        public TimeSpan TimeRemaining => ExpiresAt > DateTime.UtcNow ? ExpiresAt - DateTime.UtcNow : TimeSpan.Zero;



        /// <summary>
        /// Navigation Properties
        /// </summary>
        public User User { get; set; } //Navigation property to User entity
        public Product Product { get; set; } // Navigation property to Product entity
        public BannerEventSpecial? AppliedEvent { get; set; }

    }
}
