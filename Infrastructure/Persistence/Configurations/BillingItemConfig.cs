using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class BillingItemConfig
    {
        public void Configure(EntityTypeBuilder<BillingItem> builder)
        {
            builder.ToTable("BillingItems");
            builder.HasKey(bi => bi.Id);

            builder.Property(bi => bi.ProductId).IsRequired();
            builder.Property(bi => bi.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(bi => bi.ProductSku).HasMaxLength(100).IsRequired();
            builder.Property(bi => bi.Quantity).IsRequired();
            builder.Property(bi => bi.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(bi => bi.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(bi => bi.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(bi => bi.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Property(bi => bi.Notes).HasMaxLength(500);

            builder.HasOne(bi => bi.Billing)
                .WithMany(b => b.Items)
                .HasForeignKey(bi => bi.BillingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
