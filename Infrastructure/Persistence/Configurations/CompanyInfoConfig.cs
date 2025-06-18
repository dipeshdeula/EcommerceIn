using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class CompanyInfoConfig : IEntityTypeConfiguration<CompanyInfo>
    {
        public void Configure(EntityTypeBuilder<CompanyInfo> builder)
        {
            builder.ToTable("CompanyInfos");

            builder.HasKey(ci => ci.Id);

            builder.Property(ci => ci.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ci => ci.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ci => ci.Contact)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(ci => ci.RegisteredPanNumber)
                .HasMaxLength(50);

            builder.Property(ci => ci.RegisteredVatNumber)
                .HasMaxLength(50);

            builder.Property(ci => ci.Street)
                .HasMaxLength(100);

            builder.Property(ci => ci.City)
                .HasMaxLength(50);

            builder.Property(ci => ci.Province)
                .HasMaxLength(50);

            builder.Property(ci => ci.PostalCode)
                .HasMaxLength(20);

            builder.Property(ci => ci.LogoUrl)
                .HasMaxLength(300);

            builder.Property(ci => ci.WebsiteUrl)
                .HasMaxLength(200);

            builder.Property(ci => ci.RegistrationNumber)
                .HasMaxLength(100);           

            builder.Property(ci => ci.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(ci => ci.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
