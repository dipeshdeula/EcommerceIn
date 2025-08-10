using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CartItemConfig : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.ShippingId).IsRequired(false);

            builder.Property(c => c.ShippingCost)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m).IsRequired();

            //  PRICING COLUMNS
            builder.Property(c => c.OriginalPrice)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m).IsRequired();

            builder.Property(c => c.ReservedPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.RegularDiscountAmount)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            //  EVENT INTEGRATION
            builder.Property(c => c.EventDiscountAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.EventDiscountPercentage)
                .HasColumnType("decimal(5,2)");

            //  PROMO CODE INTEGRATION
            builder.Property(c => c.PromoCodeDiscountAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.AppliedPromoCode)
                .HasMaxLength(50);

            //  TIMESTAMPS
            builder.Property(c => c.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            builder.Property(c => c.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(c => c.LastActivityAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(c => c.ExpiresAt)
                .HasColumnType("timestamp with time zone");

            //  FLAGS
            builder.Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(c => c.IsStockReserved)
                .HasDefaultValue(false);

            //  RESERVATION
            builder.Property(c => c.ReservationToken)
                .HasMaxLength(100);

            //  RELATIONSHIPS
            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.AppliedEvent)
                .WithMany()
                .HasForeignKey(c => c.AppliedEventId)
                .OnDelete(DeleteBehavior.SetNull);

            //  PROMO CODE RELATIONSHIP
            builder.HasOne(c => c.AppliedPromoCode_Navigation)
                .WithMany()
                .HasForeignKey(c => c.AppliedPromoCodeId)
                .OnDelete(DeleteBehavior.SetNull);

             builder.HasOne(c => c.Shipping)
                .WithMany()
                .HasForeignKey(c => c.ShippingId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            //  INDEXES for performance
            builder.HasIndex(ci => ci.ExpiresAt)
                .HasDatabaseName("IX_CartItems_ExpiresAt");

            builder.HasIndex(ci => new { ci.UserId, ci.IsDeleted })
                .HasDatabaseName("IX_CartItems_UserId_IsDeleted");

            builder.HasIndex(ci => ci.AppliedPromoCodeId)
                .HasDatabaseName("IX_CartItems_AppliedPromoCodeId");

            builder.HasIndex(ci => ci.AppliedEventId)
                .HasDatabaseName("IX_CartItems_AppliedEventId");

            builder.HasIndex(ci => ci.ShippingId)
                .HasDatabaseName("IX_CartItems_ShippingId");

            builder.HasIndex(ci => new { ci.UserId, ci.ProductId, ci.IsDeleted })
                .HasDatabaseName("IX_CartItems_UserId_ProductId_IsDeleted");
        }
    }
}