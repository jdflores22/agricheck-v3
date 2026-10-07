using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class SpsPrerequisiteClearanceConfiguration : IEntityTypeConfiguration<SpsPrerequisiteClearance>
{
    public void Configure(EntityTypeBuilder<SpsPrerequisiteClearance> builder)
    {
        builder.ToTable("sps_prerequisite_clearances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PrerequisiteCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(64);
        builder.HasIndex(x => new { x.SpsApplicationId, x.PrerequisiteCode }).IsUnique();
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.SpsApplicationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.IssuingAgency).WithMany().HasForeignKey(x => x.IssuingAgencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpsMustShipOutRuleConfiguration : IEntityTypeConfiguration<SpsMustShipOutRule>
{
    public void Configure(EntityTypeBuilder<SpsMustShipOutRule> builder)
    {
        builder.ToTable("sps_must_ship_out_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HsCodePrefix).HasMaxLength(16).IsRequired();
        builder.Property(x => x.CommodityKeyword).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(256);
        builder.HasIndex(x => new { x.HsCodePrefix, x.CommodityKeyword });
    }
}

public class StorageTallySheetConfiguration : IEntityTypeConfiguration<StorageTallySheet>
{
    public void Configure(EntityTypeBuilder<StorageTallySheet> builder)
    {
        builder.ToTable("storage_tally_sheets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Direction).HasMaxLength(8).IsRequired();
        builder.Property(x => x.CommodityDescription).HasMaxLength(512);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Container).WithMany().HasForeignKey(x => x.ContainerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
