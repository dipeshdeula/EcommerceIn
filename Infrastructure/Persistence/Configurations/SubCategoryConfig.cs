using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class SubCategoryConfig : IEntityTypeConfiguration<SubCategory>
    {
        public void Configure(EntityTypeBuilder<SubCategory> builder)
        {
            builder.ToTable("SubCategories");

            builder.HasKey(sc => sc.Id);

            builder.Property(sc => sc.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sc => sc.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sc => sc.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(sc => sc.ImageUrl)
                .IsRequired()
                .HasMaxLength(200);

            // Relationship with Category
            builder.HasOne(sc => sc.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with SubSubCategories
            builder.HasMany(sc => sc.SubSubCategories)
                .WithOne(ssc => ssc.SubCategory)
                .HasForeignKey(ssc => ssc.SubCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
