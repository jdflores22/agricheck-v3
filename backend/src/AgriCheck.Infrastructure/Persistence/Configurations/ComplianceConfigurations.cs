using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class ComplianceSyncOutboxConfiguration : IEntityTypeConfiguration<ComplianceSyncOutbox>
{
    public void Configure(EntityTypeBuilder<ComplianceSyncOutbox> builder)
    {
        builder.ToTable("compliance_sync_outbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Operation).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TargetSystem).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.PayloadJson).HasColumnType("longtext");
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
