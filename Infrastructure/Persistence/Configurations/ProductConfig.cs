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

            builder.HasOne(p=>p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal properties
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
        }
    }
}
