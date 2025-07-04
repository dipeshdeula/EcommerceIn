﻿using Domain.Entities.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class EventUsageConfig : IEntityTypeConfiguration<EventUsage>
    {
        public void Configure(EntityTypeBuilder<EventUsage> builder)
        {
            builder.ToTable("EventUsages");
            builder.HasKey(eu => eu.Id);

            builder.Property(eu => eu.DiscountApplied)
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            builder.Property(eu => eu.UsedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            // Relationships
            builder.HasOne(eu => eu.BannerEvent)
                .WithMany(be => be.UsageHistory)
                .HasForeignKey(eu => eu.BannerEventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(eu => eu.User)
                .WithMany()
                .HasForeignKey(eu => eu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(eu => new { eu.BannerEventId, eu.UserId })
                .HasDatabaseName("IX_EventUsage_BannerEvent_User");

            builder.HasIndex(eu => eu.UsedAt)
                .HasDatabaseName("IX_EventUsage_UsedAt");
        }
    }
}