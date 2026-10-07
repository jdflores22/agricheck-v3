using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class AgencyLicenseTypeConfiguration : IEntityTypeConfiguration<AgencyLicenseType>
{
    public void Configure(EntityTypeBuilder<AgencyLicenseType> builder)
    {
        builder.ToTable("agency_license_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.HasIndex(x => new { x.AgencyId, x.Code }).IsUnique();
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AgencyLicenseConfiguration : IEntityTypeConfiguration<AgencyLicense>
{
    public void Configure(EntityTypeBuilder<AgencyLicense> builder)
    {
        builder.ToTable("agency_licenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LicenseNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.AgencyId, x.Status });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LicenseType).WithMany(t => t.Licenses).HasForeignKey(x => x.AgencyLicenseTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Application).WithOne(a => a.IssuedLicense).HasForeignKey<AgencyLicense>(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AgencyLicenseApplicationConfiguration : IEntityTypeConfiguration<AgencyLicenseApplication>
{
    public void Configure(EntityTypeBuilder<AgencyLicenseApplication> builder)
    {
        builder.ToTable("agency_license_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.FormDataJson).HasColumnType("longtext");
        builder.Property(x => x.ReviewComments).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LicenseType).WithMany(t => t.Applications).HasForeignKey(x => x.AgencyLicenseTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommodityLicenseRuleConfiguration : IEntityTypeConfiguration<CommodityLicenseRule>
{
    public void Configure(EntityTypeBuilder<CommodityLicenseRule> builder)
    {
        builder.ToTable("commodity_license_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HsCodePrefix).HasMaxLength(16);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LicenseType).WithMany(t => t.CommodityRules).HasForeignKey(x => x.AgencyLicenseTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Commodity).WithMany().HasForeignKey(x => x.CommodityId).OnDelete(DeleteBehavior.SetNull);
    }
}
