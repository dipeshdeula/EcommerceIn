using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ShippingConfig : IEntityTypeConfiguration<Shipping>
    {
        public void Configure(EntityTypeBuilder<Shipping> builder)
        {
            // Table configuration
            builder.ToTable("Shippings");
            builder.HasKey(s => s.Id);

            // Property configurations
            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.LowOrderThreshold)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(300);

            builder.Property(s => s.LowOrderShippingCost)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(50);

            builder.Property(s => s.HighOrderShippingCost)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(100);

            builder.Property(s => s.FreeShippingThreshold)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(1000);

            builder.Property(s => s.WeekendSurcharge)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(s => s.HolidaySurcharge)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(s => s.RushDeliverySurcharge)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(s => s.MinOrderAmountForShipping)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(s => s.MaxOrderAmountForShipping)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);

            builder.Property(s => s.MaxDeliveryDistanceKm)
                .HasDefaultValue(15);

            builder.Property(s => s.EstimatedDeliveryDays)
                .HasDefaultValue(2);

            builder.Property(s => s.FreeShippingDescription)
                .HasMaxLength(500)
                .HasDefaultValue("");

            builder.Property(s => s.CustomerMessage)
                .HasMaxLength(1000)
                .HasDefaultValue("");

            builder.Property(s => s.AdminNotes)
                .HasMaxLength(2000)
                .HasDefaultValue("");

            builder.Property(s => s.AvailableDays)
                .HasMaxLength(20)
                .HasDefaultValue("1,2,3,4,5,6,7");

            // Default values
            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.Property(s => s.IsDefault)
                .HasDefaultValue(false);

            builder.Property(s => s.EnableFreeShippingEvents)
                .HasDefaultValue(true);

            builder.Property(s => s.IsFreeShippingActive)
                .HasDefaultValue(false);

            builder.Property(s => s.RequireLocationValidation)
                .HasDefaultValue(true);

            builder.Property(s => s.IsDeleted)
                .HasDefaultValue(false);

            // Relationships
            builder.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.LastModifiedByUser)
                .WithMany()
                .HasForeignKey(s => s.LastModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(s => s.IsActive);
            builder.HasIndex(s => s.IsDefault);
            builder.HasIndex(s => s.IsDeleted);
            builder.HasIndex(s => new { s.IsActive, s.IsDefault, s.IsDeleted });
        }
    }

}
