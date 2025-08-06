using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(255);

            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(p => p.Description)
                .HasMaxLength(2000)
                .HasDefaultValue(string.Empty);

            // Configure decimal properties
            builder.Property(p => p.MarketPrice).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
            builder.Property(p => p.CostPrice).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
            builder.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)").IsRequired(false);



            builder.Property(p => p.StockQuantity)
           .IsRequired();

            builder.Property(p => p.ReservedStock)
                .IsRequired()
                .HasDefaultValue(0); // Default reserved stock is 0

            builder.Property(p => p.Reviews)
              .HasDefaultValue(0);

            builder.Property(p => p.Rating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0m);

            builder.Property(p => p.Sku)
              .HasMaxLength(50).IsRequired(false);

            builder.Property(p => p.Weight)
                .HasMaxLength(20);

            builder.Property(p => p.Dimensions)
                .HasMaxLength(50);



            builder.Property(p => p.Version)
            //.IsRowVersion(); // For Sql Server
            // for PostgreSQL
            .HasColumnName("xmin") // Maps to postgresSql's system column
            .HasColumnType("xid")
            .IsRowVersion()
            .IsConcurrencyToken();

            // Relationship with SubSubCategory
            builder.HasOne(p => p.SubSubCategory)
                .WithMany(ssc => ssc.Products)
                .HasForeignKey(p => p.SubSubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Category
            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with ProductImage
            builder.HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            builder.HasMany(p => p.ProductStores)
               .WithOne(ps => ps.Product)
               .HasForeignKey(ps => ps.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.EventProducts)
                .WithOne(ep => ep.Product)
                .HasForeignKey(ep => ep.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => p.Slug)
               .IsUnique();

            builder.HasIndex(p => p.Sku)
         .IsUnique()
         .HasFilter("\"Sku\" IS NOT NULL");

            builder.Property(p => p.IsDeleted)
       .IsRequired()
       .HasDefaultValue(false);

            builder.HasIndex(p => p.CategoryId);
            builder.HasIndex(p => p.SubSubCategoryId);
            builder.HasIndex(p => p.IsDeleted);

            // ✅ FIX: Computed column (not mapped)
            builder.Ignore(p => p.AvailableStock);

          
        }
    }
}
