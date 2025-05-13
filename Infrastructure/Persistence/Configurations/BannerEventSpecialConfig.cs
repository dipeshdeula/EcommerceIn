using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class BannerEventSpecialConfig : IEntityTypeConfiguration<BannerEventSpecial>
    {
        public void Configure(EntityTypeBuilder<BannerEventSpecial> builder)
        {
            builder.ToTable("BannerEventSpecials");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Name)
                .HasMaxLength(100)
                .IsRequired();
            builder.Property(b => b.Description)
                .HasMaxLength(500);
            builder.Property(b => b.Offers)
                .HasColumnType("decimal(5,2)").IsRequired();

            builder.Property(b => b.StartDate)
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("NOW()")
                   .IsRequired();

            builder.Property(b => b.EndDate)
                   .HasColumnType("timestamp with time zone")
                   .IsRequired();

            builder.Property(b => b.IsActive)
                   .HasDefaultValue(true);

            builder.Property(u => u.IsDeleted).HasDefaultValue(false);


        }
    }
}
