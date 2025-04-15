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
                   .WithMany()
                   .HasForeignKey(ps => ps.ProductId);

            builder.HasOne(ps => ps.Store)
                   .WithMany()
                   .HasForeignKey(ps => ps.StoreId);
        }
    }
}
