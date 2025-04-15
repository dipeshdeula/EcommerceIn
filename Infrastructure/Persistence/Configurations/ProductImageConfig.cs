using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductImageConfig : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.ToTable("ProductImages");
            builder.HasKey(pi => pi.Id);
            builder.Property(pi => pi.Id).ValueGeneratedOnAdd();
            builder.HasOne(pi => pi.Product)
                            .WithMany(p => p.Images)
                            .HasForeignKey(pi => pi.ProductId)
                            .OnDelete(DeleteBehavior.Cascade); // Optional: specify delete behavior
        }
    }
}
