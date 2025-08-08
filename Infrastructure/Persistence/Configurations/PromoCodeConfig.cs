using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class PromoCodeConfig: IEntityTypeConfiguration<PromoCode>
    {
        public void Configure(EntityTypeBuilder<PromoCode> builder)
        {
            builder.ToTable("PromoCodes");
            builder.HasKey(p => p.Id);
            
            // Unique code constraint
            builder.HasIndex(p => p.Code).IsUnique();
            
            // Properties
            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(p => p.Description)
                .HasMaxLength(1000);
                
            builder.Property(p => p.Type)
                .HasConversion<int>()
                .HasDefaultValue(PromoCodeType.Percentage);
                
            builder.Property(p => p.DiscountValue)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.MaxDiscountAmount)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.MinOrderAmount)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.CurrentUsageCount)
                .HasDefaultValue(0);
                
            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);
                
            builder.Property(p => p.ApplyToShipping)
                .HasDefaultValue(false);
                
            builder.Property(p => p.StackableWithEvents)
                .HasDefaultValue(false);
                
            builder.Property(p => p.CustomerTier)
                .HasMaxLength(50);
                
            builder.Property(p => p.AdminNotes)
                .HasMaxLength(2000);
                
            // Relationships
            builder.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(p => p.LastModifiedByUser)
                .WithMany()
                .HasForeignKey(p => p.LastModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Indexes
            builder.HasIndex(p => p.IsActive);
            builder.HasIndex(p => p.StartDate);
            builder.HasIndex(p => p.EndDate);
            builder.HasIndex(p => new { p.IsActive, p.StartDate, p.EndDate });
        }
    }
    
    public class PromoCodeUsageConfiguration : IEntityTypeConfiguration<PromoCodeUsage>
    {
        public void Configure(EntityTypeBuilder<PromoCodeUsage> builder)
        {
            builder.ToTable("PromoCodeUsages");
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.OrderTotal)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(u => u.ShippingCost)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(u => u.DiscountAmount)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(u => u.UserEmail)
                .HasMaxLength(256);
                
            builder.Property(u => u.PaymentMethod)
                .HasMaxLength(50);
                
            builder.Property(u => u.Notes)
                .HasMaxLength(1000);
                
            // Relationships
            builder.HasOne(u => u.PromoCode)
                .WithMany(p => p.PromoCodeUsages)
                .HasForeignKey(u => u.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(u => u.Order)
                .WithMany()
                .HasForeignKey(u => u.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Indexes
            builder.HasIndex(u => u.UsedAt);
            builder.HasIndex(u => new { u.UserId, u.PromoCodeId });
        }
    }
}
