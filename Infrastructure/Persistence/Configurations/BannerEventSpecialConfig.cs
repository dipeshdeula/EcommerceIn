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
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("NOW()")
                   .IsRequired();

            builder.Property(b => b.EndDate)
                   .HasColumnType("timestamp with time zone")
                   .IsRequired();

            builder.Property(b => b.EventType)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            builder.Property(b => b.PromotionType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

           
            builder.Property(b => b.IsActive)
                   .HasDefaultValue(false);

            builder.Property(u => u.IsDeleted).HasDefaultValue(false);

            builder.Property(b => b.Priority)
                .HasDefaultValue(1);

            builder.Property(b => b.MaxUsageCount)
                .HasDefaultValue(int.MaxValue);

            builder.Property(b => b.MaxUsagePerUser)
                .HasDefaultValue(int.MaxValue);

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
            builder.HasIndex(b => new { b.StartDate, b.EndDate, b.IsActive, b.Status })
                .HasDatabaseName("IX_BannerEvents_ActivePeriod");

            builder.HasIndex(b => b.EventType)
                .HasDatabaseName("IX_BannerEvents_Type");

            builder.HasIndex(b => b.Priority)
                .HasDatabaseName("IX_BannerEvents_Priority");
        }




    }
    
}
