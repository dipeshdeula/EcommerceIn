using Domain.Entities.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class EventProductConfig : IEntityTypeConfiguration<EventProduct>
    {
        public void Configure(EntityTypeBuilder<EventProduct> builder)
        {
            builder.ToTable("EventProducts");
            builder.HasKey(ep => ep.Id);

            builder.Property(ep => ep.SpecificDiscount)
                .HasColumnType("decimal(18,4)");

            builder.Property(ep => ep.AddedAt)
              .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            builder.HasOne(ep => ep.BannerEvent)
                .WithMany(be => be.EventProducts)
                .HasForeignKey(ep => ep.BannerEventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ep => ep.Product)
                .WithMany(p => p.EventProducts)
                .HasForeignKey(ep => ep.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(ep => new { ep.BannerEventId, ep.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_EventProduct_BannerEvent_Product");

            builder.HasIndex(ep => ep.ProductId)
                .HasDatabaseName("IX_EventProduct_Product");

            builder.HasIndex(ep => new { ep.BannerEventId, ep.IsDeleted })
               .HasDatabaseName("IX_EventProducts_BannerEvent_Deleted");
        }
    }
}
