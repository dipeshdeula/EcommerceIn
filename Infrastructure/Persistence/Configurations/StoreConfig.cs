using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class StoreConfig : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> builder)
        {
            builder.ToTable("Stores");
            builder.HasKey(s => s.Id);

            builder.HasOne(s=>s.Address)
                .WithOne(a=>a.Store)
                .HasForeignKey<StoreAddress>(a=>a.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
