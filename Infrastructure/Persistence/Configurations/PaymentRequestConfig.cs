using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class PaymentRequestConfig : IEntityTypeConfiguration<PaymentRequest>
    {
        public void Configure(EntityTypeBuilder<PaymentRequest> builder)
        {
            builder.ToTable("PaymentRequests");
            builder.HasKey(pr => pr.Id);

            builder.Property(pr => pr.PaymentAmount).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(pr => pr.Currency)
                .HasMaxLength(10)
                .HasDefaultValue("NPR")
                .IsRequired();
            builder.Property(pr => pr.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            builder.Property(pr => pr.PaymentUrl)
                .HasMaxLength(2000);
            builder.Property(pr => pr.Description)
                .HasMaxLength(500);
            builder.Property(pr => pr.EsewaTransactionId)
                .HasMaxLength(200);
            builder.Property(pr => pr.KhaltiPidx)
                .HasMaxLength(200);

            builder.Property(pr => pr.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(pr => pr.UpdatedAt)
                   .HasColumnType("timestamp with time zone")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pr => pr.Order)
                .WithMany()
                .HasForeignKey(pr => pr.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pr => pr.PaymentMethod)
                   .WithMany(pm => pm.PaymentRequests)
                   .HasForeignKey(pr => pr.PaymentMethodId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
