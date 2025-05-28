using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class BannerImageConfig : IEntityTypeConfiguration<BannerImage>
    {
        public void Configure(EntityTypeBuilder<BannerImage> builder)
        {
            builder.ToTable("BannerImages");

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedOnAdd();
            builder.Property(b => b.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);
            builder.Property(b => b.IsMain)
                .HasDefaultValue(false);
            builder.Property(b => b.IsDeleted)
                .HasDefaultValue(false);
            builder.HasOne(b=>b.BannerEventSpecial)
                .WithMany(bi=>bi.Images)
                .HasForeignKey(b=>b.BannerId)
                 .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(b => new { b.BannerId, b.IsMain })
                   .IsUnique()
                   .HasFilter("\"IsMain\" = TRUE"); // ← PostgreSQL uses double-quotes for identifiers
        }
    }
}
