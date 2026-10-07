using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class SpsImportClearanceApplicationConfiguration : IEntityTypeConfiguration<SpsImportClearanceApplication>
{
    public void Configure(EntityTypeBuilder<SpsImportClearanceApplication> builder)
    {
        builder.ToTable("sps_import_clearance_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.FormDataJson).HasColumnType("longtext");
        builder.Property(x => x.ReviewComments).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.EntryId).IsUnique();
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AgencyLicense).WithMany().HasForeignKey(x => x.AgencyLicenseId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SpsImportClearanceConfiguration : IEntityTypeConfiguration<SpsImportClearance>
{
    public void Configure(EntityTypeBuilder<SpsImportClearance> builder)
    {
        builder.ToTable("sps_import_clearances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClearanceNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.PortOfEntry).HasMaxLength(128);
        builder.Property(x => x.CommodityDescription).HasMaxLength(512);
        builder.Property(x => x.DaTradeReference).HasMaxLength(64);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.ClearanceNumber).IsUnique();
        builder.HasIndex(x => x.EntryId);
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Application).WithOne(a => a.IssuedClearance).HasForeignKey<SpsImportClearance>(c => c.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IssuingAgency).WithMany().HasForeignKey(x => x.IssuingAgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinalWarehouseFacility).WithMany().HasForeignKey(x => x.FinalWarehouseFacilityId).OnDelete(DeleteBehavior.SetNull);
    }
}
