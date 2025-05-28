using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class StoreAddressConfig : IEntityTypeConfiguration<StoreAddress>
    {
        public void Configure(EntityTypeBuilder<StoreAddress> builder)
        {
            builder.ToTable("StoreAddresses");

            builder.HasKey(sa => sa.Id);
            builder.Property(sa => sa.Id).ValueGeneratedOnAdd();

            builder.Property(sa => sa.Street).HasMaxLength(200);
            builder.Property(sa => sa.City).HasMaxLength(100);
            builder.Property(sa => sa.Province).HasMaxLength(100);
            builder.Property(sa => sa.PostalCode).HasMaxLength(20);

            builder.Property(sa => sa.Latitude).IsRequired();
            builder.Property(sa => sa.Longitude).IsRequired();

            builder.HasOne(sa => sa.Store)
                .WithOne(s => s.Address)
                .HasForeignKey<StoreAddress>(sa => sa.StoreId) // ✅ Fix: specify target entity type
                .OnDelete(DeleteBehavior.Cascade); // Optional: cascade delete if store is deleted
        }
    }
}
