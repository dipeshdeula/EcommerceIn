using Domain.Enums.BannerEventSpecial;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

            builder.Property(b => b.TagLine)
                .HasMaxLength(200);

            builder.Property(b => b.DiscountValue)
                .HasColumnType("decimal(10,2)").IsRequired();

            builder.Property(b => b.MaxDiscountAmount)
           .HasColumnType("decimal(10,2)");

            builder.Property(b => b.MinOrderValue)
                .HasColumnType("decimal(10,2)");

            builder.Property(b => b.StartDate)
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                   .HasColumnType("timestamp with time zone")
                   .IsRequired();

            builder.Property(b => b.EndDate)
                   .HasColumnType("timestamp with time zone")
                   .IsRequired();

            // Explicit enum conversions

            builder.Property(b => b.EventType)
                 .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<EventType>(v))
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(b => b.PromotionType)
                 .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<PromotionType>(v))
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(b => b.Status)
                  .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<EventStatus>(v))
                .HasMaxLength(50)
                .HasDefaultValue(EventStatus.Draft)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                   .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                  .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(b => b.UpdatedAt)
               .HasColumnType("timestamp with time zone");

            builder.Property(b => b.IsActive)
                   .HasDefaultValue(false);

            builder.Property(b => b.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(b => b.Priority)
                .HasDefaultValue(1);

            builder.Property(b => b.MaxUsageCount)
                .HasDefaultValue(2147483647);

            builder.Property(b => b.MaxUsagePerUser)
                .HasDefaultValue(10);

            builder.Property(b => b.CurrentUsageCount)
                .HasDefaultValue(0);

            // Relationships
            builder.HasMany(b => b.Images)
                .WithOne(i => i.BannerEventSpecial)
                .HasForeignKey(i => i.BannerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.Rules)
                .WithOne(r => r.BannerEvent)
                .HasForeignKey(r => r.BannerEventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.EventProducts)
                .WithOne(ep => ep.BannerEvent)
                .HasForeignKey(ep => ep.BannerEventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(b => b.UsageHistory)
                .WithOne(u => u.BannerEvent)
                .HasForeignKey(u => u.BannerEventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            builder.HasIndex(b => new { b.Status, b.IsActive, b.StartDate, b.EndDate })
                .HasFilter("\"Status\" = 'Active' AND \"IsActive\" = true")
                .HasDatabaseName("IX_BannerEvents_ActiveTimeRange");

            builder.HasIndex(b => b.EventType)
                .HasDatabaseName("IX_BannerEvents_Type");

            builder.HasIndex(b => b.Priority)
                .HasDatabaseName("IX_BannerEvents_Priority");

            builder.HasIndex(b => new { b.IsActive, b.IsDeleted })
                .HasDatabaseName("IX_BannerEvents_ActiveDeleted");
        }
    }

}
