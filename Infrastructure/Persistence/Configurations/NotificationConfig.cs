using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(b => b.Id);

            builder.Property(b=>b.RowVersion)
            .IsRowVersion().HasColumnName("xmin");
            
            builder.Property(b => b.IsDeleted)
                .HasDefaultValue(false);

            builder.HasOne(n => n.Order)
                .WithMany(o => o.Notifications)
                .HasForeignKey(n => n.OrderId)
                .IsRequired(false)
                 .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => n.OrderId); 
        }
    }
}
