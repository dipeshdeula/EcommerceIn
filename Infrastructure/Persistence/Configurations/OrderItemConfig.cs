using Application.Extension;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");
            builder.HasKey(oi => oi.Id);

            builder.HasOne(oi => oi.Order)
              .WithMany(o => o.Items)
              .HasForeignKey(oi => oi.OrderId)
              .OnDelete(DeleteBehavior.Cascade); // Cascade delete when an order is deleted

            builder.HasOne(oi => oi.Product)
                   .WithMany()
                   .HasForeignKey(oi => oi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict); // Restrict delete for products

            builder.Property(oi => oi.Quantity)
                   .IsRequired();

            // Configure decimal properties
            builder.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();

            // Configure TotalPrice as a computed column (stored) with PostgreSQL-compatible syntax
            builder.Property(oi => oi.TotalPrice)
                   .HasColumnType("decimal(18,2)")
                   .HasComputedColumnSql("\"UnitPrice\" * \"Quantity\"", stored: true); // Use double quotes for PostgreSQL

            builder.Property(oi => oi.IsDeleted)
                  .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(oi => new { oi.OrderId, oi.ProductId })
                   .HasDatabaseName("IX_OrderItem_OrderId_ProductId")
                   .IsUnique(); // Ensure unique combination of OrderId and ProductId
        }
    }
}
