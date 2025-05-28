using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.HasOne(o => o.User)
                   .WithMany()
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.Property(o => o.Status)
                .HasMaxLength(50).HasDefaultValue("Pending");

            builder.Property(o => o.ShippingAddress)
                          .HasMaxLength(250)
                          .IsRequired();

            builder.Property(o => o.ShippingCity)
                   .HasMaxLength(100)
                   .IsRequired();
            // Configure decimal properties
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(o => o.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(o => new { o.UserId, o.OrderDate })
                   .HasDatabaseName("IX_Order_UserId_OrderDate");

        }
    }
}
