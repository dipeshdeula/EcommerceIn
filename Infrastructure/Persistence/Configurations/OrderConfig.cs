using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.HasOne(o => o.User)
                   .WithMany()
                   .HasForeignKey(o => o.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal properties
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        }
    }
}
