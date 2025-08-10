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
                .HasDefaultValue(300.00m);

            builder.Property(s => s.LowOrderShippingCost)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(50.00m);

            builder.Property(s => s.HighOrderShippingCost)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(100.00m);

            builder.Property(s => s.FreeShippingThreshold)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(1000.00m);

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
                .HasDefaultValue(1);

            builder.Property(s => s.FreeShippingDescription)
                .HasMaxLength(500)
                .HasDefaultValue("");

            builder.Property(s => s.CustomerMessage)
                .HasMaxLength(1000)
                .HasDefaultValue("Standard shipping rates apply");

            builder.Property(s => s.AdminNotes)
                .HasMaxLength(2000)
                .HasDefaultValue("");

            builder.Property(s => s.AvailableDays)
                .HasMaxLength(20)
                .HasDefaultValue("1,2,3,4,5,6,7");

            // DATETIME PROPERTIES
             builder.Property(s => s.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            builder.Property(s => s.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            builder.Property(s => s.FreeShippingStartDate)
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.FreeShippingEndDate)
                .HasColumnType("timestamp with time zone");

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
           builder.HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_Shippings_IsActive");

            builder.HasIndex(s => s.IsDefault)
                .HasDatabaseName("IX_Shippings_IsDefault");

            builder.HasIndex(s => s.IsDeleted)
                .HasDatabaseName("IX_Shippings_IsDeleted");

            builder.HasIndex(s => new { s.IsActive, s.IsDefault, s.IsDeleted })
                .HasDatabaseName("IX_Shippings_ActiveDefaultDeleted");

            //  UNIQUE CONSTRAINT - Only one default shipping config
            builder.HasIndex(s => new { s.IsDefault, s.IsDeleted })
                .HasDatabaseName("IX_Shippings_UniqueDefault")
                .HasFilter("\"IsDefault\" = true AND \"IsDeleted\" = false")
                .IsUnique();
        }
    }

}
