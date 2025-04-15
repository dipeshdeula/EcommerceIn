using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class AddressConfig : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        { 
            builder.ToTable("Addresses");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedOnAdd();

            builder.HasOne(a=>a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Optional: specify delete behavior


        }
    }
    
}
