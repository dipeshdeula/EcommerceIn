using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(100);

            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(500);

            // Configure decimal properties
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");

            // Relationship with SubSubCategory
            builder.HasOne(p => p.SubSubCategory)
                .WithMany(ssc => ssc.Products)
                .HasForeignKey(p => p.SubSubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Category
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with ProductImage
            builder.HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete
        }
    }
}
