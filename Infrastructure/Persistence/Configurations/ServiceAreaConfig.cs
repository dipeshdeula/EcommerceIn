using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Persistence.Configurations
{
    public class ServiceAreaConfig : IEntityTypeConfiguration<ServiceArea>
    {
        public void Configure(EntityTypeBuilder<ServiceArea> builder)
        {
            builder.ToTable("ServiceAreas");

            builder.HasKey(sa => sa.Id);

            builder.Property(sa => sa.CityName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sa => sa.Province)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sa => sa.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Nepal");

            builder.Property(sa => sa.DisplayName)
                .HasMaxLength(150);

            builder.Property(sa => sa.Description)
                .HasMaxLength(500);

            builder.Property(sa => sa.NotAvailableMessage)
                .HasMaxLength(300);

            // Decimal precision for coordinates
            builder.Property(sa => sa.CenterLatitude)
                .HasPrecision(10, 8);

            builder.Property(sa => sa.CenterLongitude)
                .HasPrecision(11, 8);

            builder.Property(sa => sa.RadiusKm)
                .HasPrecision(8, 2);

            builder.Property(sa => sa.MaxDeliveryDistanceKm)
                .HasPrecision(8, 2);

            builder.Property(sa => sa.MinOrderAmount)
                .HasPrecision(18, 2);

            // Indexes for performance
            builder.HasIndex(sa => sa.CityName);
            builder.HasIndex(sa => sa.IsActive);
            builder.HasIndex(sa => new { sa.CenterLatitude, sa.CenterLongitude });

            // Relationships
            builder.HasMany(sa => sa.Stores)
                .WithOne(s => s.ServiceArea)
                .HasForeignKey(s => s.ServiceAreaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(sa => sa.Orders)
                .WithOne(o => o.ServiceArea)
                .HasForeignKey(o => o.ServiceAreaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed initial data
            builder.HasData(
                new ServiceArea
                {
                    Id = 1,
                    CityName = "Hetauda",
                    DisplayName = "Hetauda City",
                    Province = "Bagmati",
                    Country = "Nepal",
                    CenterLatitude = 27.4239,
                    CenterLongitude = 85.0478,
                    RadiusKm = 15.0,
                    IsActive = true,
                    IsComingSoon = false,
                    MaxDeliveryDistanceKm = 10.0,
                    MinOrderAmount = 500,
                    EstimatedDeliveryDays = 1,
                    DeliveryStartTime = new TimeSpan(9, 0, 0),
                    DeliveryEndTime = new TimeSpan(21, 0, 0),
                    Description = "Premium delivery service in Hetauda city and surrounding areas",
                    NotAvailableMessage = "Service not available in your area yet. Coming soon to Hetauda!",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
