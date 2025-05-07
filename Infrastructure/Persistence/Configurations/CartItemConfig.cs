using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class CartItemConfig : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            builder.HasKey(c => c.Id);

            builder.Property(ci => ci.CreatedAt)
                  .HasColumnType("timestamp with time zone")
                  .HasDefaultValueSql("NOW()"); // Default to current UTC time

            builder.Property(ci => ci.UpdatedAt)
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("NOW()"); // Default to current UTC time

            builder.Property(ci => ci.IsDeleted)
                   .HasDefaultValue(false);

            builder.HasOne(c => c.User)
                   .WithMany()
                   .HasForeignKey(c => c.UserId);

            builder.HasOne(c => c.Product)
                   .WithMany()
                   .HasForeignKey(c => c.ProductId);
        }
    }
}
