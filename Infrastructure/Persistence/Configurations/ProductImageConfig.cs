using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductImageConfig : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.ToTable("ProductImages");

            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(pi => pi.ImageUrl)
                   .IsRequired()
                   .HasMaxLength(500); // Optional: limit URL size

            builder.Property(pi => pi.IsMain)
                   .HasDefaultValue(false);

            builder.Property(pi => pi.IsDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(pi => pi.Product)
                   .WithMany(p => p.Images)
                   .HasForeignKey(pi => pi.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            // ✅ PostgreSQL-compatible filtered unique index
            builder.HasIndex(pi => new { pi.ProductId, pi.IsMain })
                   .IsUnique()
                   .HasFilter("\"IsMain\" = TRUE"); // ← PostgreSQL uses double-quotes for identifiers
        }
    }
}
