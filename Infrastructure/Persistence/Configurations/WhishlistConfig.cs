using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class WhishlistConfig : IEntityTypeConfiguration<Wishlist>

    {
        public void Configure(EntityTypeBuilder<Wishlist> builder)
        {
            builder.ToTable("Whishlists");

            builder.HasKey(x => x.Id);

            builder.Property(w => w.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'"); // Default to current UTC time

            builder.Property(w => w.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(w => w.IsDeleted)
                .HasDefaultValue(false);

            // Foreign key relationships

            builder.HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            //Indexes for performance
            builder.HasIndex(w => new { w.UserId, w.IsDeleted })
                .HasDatabaseName("IX_Whishlist_UserId_IsDeleted");

             builder.HasIndex(w => new { w.UserId, w.ProductId, w.IsDeleted })
                .IsUnique()
                .HasDatabaseName("IX_Wishlist_Unique_UserProduct");

            builder.HasIndex(w => w.ProductId)
                .HasDatabaseName("IX_Wishlist_ProductId");
            
        }
    }
}
