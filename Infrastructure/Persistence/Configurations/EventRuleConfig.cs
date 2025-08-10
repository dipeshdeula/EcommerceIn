using Domain.Entities.Common;
using Domain.Enums.BannerEventSpecial;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class EventRuleConfig : IEntityTypeConfiguration<EventRule> //  FIX: Use EventRule, not Common.EventRule
    {
        public void Configure(EntityTypeBuilder<EventRule> builder) //  FIX: Remove override
        {
            builder.ToTable("EventRules");
            builder.HasKey(er => er.Id);

            //  BUSINESS PROPERTIES
            builder.Property(er => er.TargetValue)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(er => er.BannerEventId)
              .IsRequired();

            /*builder.Property(er => er.Type)
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<RuleType>(v))
               .HasMaxLength(50)
               .IsRequired();*/

            builder.Property(er => er.Conditions)
                .HasMaxLength(1000)
                .IsRequired(false);

       

            builder.Property(er => er.DiscountValue)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(er => er.MaxDiscount)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);

            builder.Property(er => er.MinOrderValue)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);            

            builder.Property(er => er.Priority)
                .HasDefaultValue(1);

            //  SIMPLE TRACKING
            builder.Property(er => er.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(er => er.UpdatedAt)
                .IsRequired(false);

            //  RELATIONSHIPS
            builder.HasOne(er => er.BannerEvent)
                .WithMany(be => be.Rules)
                .HasForeignKey(er => er.BannerEventId)
                .OnDelete(DeleteBehavior.Cascade);

            //  INDEXES
            builder.HasIndex(er => er.BannerEventId)
                 .HasDatabaseName("IX_EventRules_BannerEvent");
            builder.HasIndex(er => er.Type)
                 .HasDatabaseName("IX_EventRules_Type");

            builder.HasIndex(er => new { er.BannerEventId, er.IsDeleted })
                .HasDatabaseName("IX_EventRules_BannerEvent_Active");
            builder.HasIndex(er => new { er.BannerEventId, er.Priority });
        }
    }
}
