using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SubSubCategoryConfig : IEntityTypeConfiguration<SubSubCategory>
    {
        public void Configure(EntityTypeBuilder<SubSubCategory> builder)
        {
            builder.ToTable("SubSubCategories");

            builder.HasKey(ssc => ssc.Id);

            builder.Property(ssc => ssc.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ssc => ssc.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ssc => ssc.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ssc => ssc.ImageUrl)
                .IsRequired()
                .HasMaxLength(200);

            // Relationship with SubCategory
            builder.HasOne(ssc => ssc.SubCategory)
                .WithMany(sc => sc.SubSubCategories)
                .HasForeignKey(ssc => ssc.SubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Products
            builder.HasMany(ssc => ssc.Products)
                .WithOne(p => p.SubSubCategory)
                .HasForeignKey(p => p.SubSubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
