using Domain.Entities.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class BaseEntityConfig<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            // Automatically configure common properties for all entities
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            // Global query filter - automatically exclude soft-deleted records
            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }

}
