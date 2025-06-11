using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CartItemConfig : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.ReservedPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.EventDiscountAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.EventDiscountPercentage)
           .HasColumnType("decimal(5,2)");

            builder.Property(c => c.CreatedAt)
                  .HasColumnType("timestamp with time zone")
                  .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC' "); // Default to current UTC time

            builder.Property(c => c.UpdatedAt)
                   .HasColumnType("timestamp with time zone");


            builder.Property(c => c.IsDeleted)
                   .HasDefaultValue(false);

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

            //  INDEXES for performance
            builder.HasIndex(ci => ci.ExpiresAt);
            builder.HasIndex(ci => new { ci.UserId, ci.IsDeleted });
        }
    }
}
