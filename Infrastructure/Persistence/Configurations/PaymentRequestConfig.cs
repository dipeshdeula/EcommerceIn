using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class PaymentRequestConfig : IEntityTypeConfiguration<PaymentRequest>
    {
        public void Configure(EntityTypeBuilder<PaymentRequest> builder)
        {
            builder.ToTable("PaymentRequests");
            builder.HasKey(pr=>pr.Id);

            builder.Property(pr => pr.PaymentAmount).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(pr => pr.Currency).HasMaxLength(10).IsRequired();

            builder.Property(pr => pr.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            builder.Property(pr => pr.UpdatedAt)
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("NOW()");

            builder.HasOne(pr=>pr.User)
                .WithMany()
                .HasForeignKey(pr=>pr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pr=>pr.Order)
                .WithMany()
                .HasForeignKey(pr=>pr.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pr => pr.PaymentMethod)
                   .WithMany(pm => pm.PaymentRequests)
                   .HasForeignKey(pr => pr.PaymentMethodId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
