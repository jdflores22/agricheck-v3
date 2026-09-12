using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class MavApplicationPeriodConfiguration : IEntityTypeConfiguration<MavApplicationPeriod>
{
    public void Configure(EntityTypeBuilder<MavApplicationPeriod> builder)
    {
        builder.ToTable("mav_application_periods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.MavYear);
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.PoolType).HasConversion<string>().HasMaxLength(8);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}

public class MavCommodityAllocationConfiguration : IEntityTypeConfiguration<MavCommodityAllocation>
{
    public void Configure(EntityTypeBuilder<MavCommodityAllocation> builder)
    {
        builder.ToTable("mav_commodity_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HsCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CommodityName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.TotalVolume).HasPrecision(15, 3);
        builder.Property(x => x.AllocatedVolume).HasPrecision(15, 3);
        builder.Property(x => x.MinimumImportVolume).HasPrecision(15, 3);
        builder.HasOne(x => x.ApplicationPeriod).WithMany(x => x.CommodityAllocations).HasForeignKey(x => x.ApplicationPeriodId);
        builder.HasOne(x => x.Commodity).WithMany().HasForeignKey(x => x.CommodityId);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}

public class MavApplicationConfiguration : IEntityTypeConfiguration<MavApplication>
{
    public void Configure(EntityTypeBuilder<MavApplication> builder)
    {
        builder.ToTable("mav_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ReferenceNumber).IsUnique();
        builder.Property(x => x.HsCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CommodityName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RequestedVolume).HasPrecision(15, 3);
        builder.Property(x => x.AllocatedVolume).HasPrecision(15, 3);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.ApplicationPeriod).WithMany(x => x.Applications).HasForeignKey(x => x.ApplicationPeriodId);
        builder.HasOne(x => x.Importer).WithMany().HasForeignKey(x => x.ImporterId);
        builder.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedByUserId);
        builder.HasOne(x => x.MavHsDetail).WithMany().HasForeignKey(x => x.MavHsDetailId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.License).WithOne(x => x.Application).HasForeignKey<MavLicense>(x => x.ApplicationId);
    }
}

public class MavLicenseConfiguration : IEntityTypeConfiguration<MavLicense>
{
    public void Configure(EntityTypeBuilder<MavLicense> builder)
    {
        builder.ToTable("mav_licenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.LicenseNumber).IsUnique();
        builder.Property(x => x.HsCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CommodityName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.AwardedVolume).HasPrecision(15, 3);
        builder.Property(x => x.PoolType).HasConversion<string>().HasMaxLength(8);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Importer).WithMany().HasForeignKey(x => x.ImporterId);
        builder.HasOne(x => x.Account).WithOne(x => x.License).HasForeignKey<MavAccount>(x => x.LicenseId);
    }
}

public class MavAccountConfiguration : IEntityTypeConfiguration<MavAccount>
{
    public void Configure(EntityTypeBuilder<MavAccount> builder)
    {
        builder.ToTable("mav_accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AwardedVolume).HasPrecision(15, 3);
        builder.Property(x => x.UtilizedVolume).HasPrecision(15, 3);
    }
}

public class MavAccountTransactionConfiguration : IEntityTypeConfiguration<MavAccountTransaction>
{
    public void Configure(EntityTypeBuilder<MavAccountTransaction> builder)
    {
        builder.ToTable("mav_account_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Volume).HasPrecision(15, 3);
        builder.Property(x => x.BalanceBefore).HasPrecision(15, 3);
        builder.Property(x => x.BalanceAfter).HasPrecision(15, 3);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.HasOne(x => x.Account).WithMany(x => x.Transactions).HasForeignKey(x => x.AccountId);
        builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId);
    }
}

public class MavImportCertificateConfiguration : IEntityTypeConfiguration<MavImportCertificate>
{
    public void Configure(EntityTypeBuilder<MavImportCertificate> builder)
    {
        builder.ToTable("mav_import_certificates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.CertificateNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.CertificateNumber).IsUnique();
        builder.Property(x => x.HsCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CommodityName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.AuthorizedVolume).HasPrecision(15, 3);
        builder.Property(x => x.UtilizedVolume).HasPrecision(15, 3);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.License).WithMany(x => x.ImportCertificates).HasForeignKey(x => x.LicenseId);
        builder.HasOne(x => x.Account).WithMany(x => x.ImportCertificates).HasForeignKey(x => x.AccountId);
        builder.HasOne(x => x.Importer).WithMany().HasForeignKey(x => x.ImporterId);
    }
}

public class MicUtilizationConfiguration : IEntityTypeConfiguration<MicUtilization>
{
    public void Configure(EntityTypeBuilder<MicUtilization> builder)
    {
        builder.ToTable("mic_utilizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Volume).HasPrecision(15, 3);
        builder.HasOne(x => x.Mic).WithMany(x => x.Utilizations).HasForeignKey(x => x.MicId);
        builder.HasOne(x => x.Entry).WithMany(x => x.MicUtilizations).HasForeignKey(x => x.EntryId);
    }
}

public class MavAuditLogConfiguration : IEntityTypeConfiguration<MavAuditLog>
{
    public void Configure(EntityTypeBuilder<MavAuditLog> builder)
    {
        builder.ToTable("mav_audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public class MavNotificationPreferenceConfiguration : IEntityTypeConfiguration<MavNotificationPreference>
{
    public void Configure(EntityTypeBuilder<MavNotificationPreference> builder)
    {
        builder.ToTable("mav_notification_preferences");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
