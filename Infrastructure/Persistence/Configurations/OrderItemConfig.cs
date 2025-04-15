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
        public void Configure(EntityTypeBuilder<OrderItem> builder) {
            builder.ToTable("OrderItems");
            builder.HasKey(oi => oi.Id);

            builder.HasOne(oi => oi.Order)
              .WithMany(o => o.Items)
              .HasForeignKey(oi => oi.OrderId);

            builder.HasOne(oi => oi.Product)
                   .WithMany()
                   .HasForeignKey(oi => oi.ProductId);

            // Configure decimal properties
            builder.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
        }
    }
}
