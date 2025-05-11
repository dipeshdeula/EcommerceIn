using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductStoreConfig : IEntityTypeConfiguration<ProductStore>
    {
        public void Configure(EntityTypeBuilder<ProductStore> builder)
        {
            builder.ToTable("ProductStores");

            builder.HasKey(ps => ps.Id);

            builder.HasOne(ps => ps.Product)
                   .WithMany(p => p.ProductStores) // Navigation property in Product
                   .HasForeignKey(ps => ps.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ps => ps.Store)
                   .WithMany(s => s.ProductStores) // Navigation property in Store
                   .HasForeignKey(ps => ps.StoreId)
                   .OnDelete(DeleteBehavior.Cascade);


            // Indexes for efficient queries
            builder.HasIndex(ps => ps.ProductId);
            builder.HasIndex(ps => ps.StoreId);
            builder.HasIndex(ps => new { ps.StoreId, ps.IsDeleted });
            builder.HasIndex(ps => new { ps.ProductId, ps.StoreId });
        }
    }
}
