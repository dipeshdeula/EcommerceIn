using Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class PaymentMethodConfig : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> builder)
        {
            builder.ToTable("PaymentMethods");
            builder.HasKey(pm => pm.Id);
            builder.Property(pm=>pm.Type).IsRequired();
            builder.Property(pm => pm.Name).HasMaxLength(50).IsRequired();
            builder.Property(pm=>pm.Logo).HasMaxLength(250).IsRequired();
            builder.Property(u => u.IsDeleted).HasDefaultValue(false);

            builder.Property(pm => pm.Type)
                .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<PaymentMethodType>(v))
               .HasMaxLength(50)
               .IsRequired();
            

        }
    }
}
