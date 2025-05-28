using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class BillingConfig : IEntityTypeConfiguration<Billing>
    {
        public void Configure(EntityTypeBuilder<Billing> builder)
        {
            builder.ToTable("Billings");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.BillingDate)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.PaymentRequest)
                .WithMany()
                .HasForeignKey(b => b.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.Order)
                .WithMany()
                .HasForeignKey(b => b.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
