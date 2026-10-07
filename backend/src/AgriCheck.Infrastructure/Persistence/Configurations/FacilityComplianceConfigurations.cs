using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class FacilityRegistrationConfiguration : IEntityTypeConfiguration<FacilityRegistration>
{
    public void Configure(EntityTypeBuilder<FacilityRegistration> builder)
    {
        builder.ToTable("facility_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.RsasExternalRef).HasMaxLength(64);
        builder.Property(x => x.OwnerName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProductsJson).HasColumnType("longtext");
        builder.Property(x => x.FormDataJson).HasColumnType("longtext");
        builder.Property(x => x.ReviewComments).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.WarehouseFacilityId).IsUnique();
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FacilityAccreditationConfiguration : IEntityTypeConfiguration<FacilityAccreditation>
{
    public void Configure(EntityTypeBuilder<FacilityAccreditation> builder)
    {
        builder.ToTable("facility_accreditations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CertificateNumber).HasMaxLength(64);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.WarehouseFacilityId);
        builder.HasOne(x => x.FacilityRegistration).WithMany(r => r.Accreditations).HasForeignKey(x => x.FacilityRegistrationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LeadAgency).WithMany().HasForeignKey(x => x.LeadAgencyId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class WarehouseOperatingLicenseConfiguration : IEntityTypeConfiguration<WarehouseOperatingLicense>
{
    public void Configure(EntityTypeBuilder<WarehouseOperatingLicense> builder)
    {
        builder.ToTable("warehouse_operating_licenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LicenseNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Notes).HasMaxLength(512);
        builder.HasIndex(x => x.WarehouseFacilityId);
        builder.HasIndex(x => x.LicenseNumber);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IssuedByUser).WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImporterFacilityAffiliationConfiguration : IEntityTypeConfiguration<ImporterFacilityAffiliation>
{
    public void Configure(EntityTypeBuilder<ImporterFacilityAffiliation> builder)
    {
        builder.ToTable("importer_facility_affiliations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.WarehouseFacilityId }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseStaffAssignmentConfiguration : IEntityTypeConfiguration<WarehouseStaffAssignment>
{
    public void Configure(EntityTypeBuilder<WarehouseStaffAssignment> builder)
    {
        builder.ToTable("warehouse_staff_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.UserId, x.WarehouseFacilityId }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseImporterAllocationConfiguration : IEntityTypeConfiguration<WarehouseImporterAllocation>
{
    public void Configure(EntityTypeBuilder<WarehouseImporterAllocation> builder)
    {
        builder.ToTable("warehouse_importer_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContractReference).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AllowedCommodityClassesJson).HasColumnType("longtext");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => new { x.ImporterUserId, x.WarehouseFacilityId, x.Status });
        builder.HasOne(x => x.ImporterUser).WithMany().HasForeignKey(x => x.ImporterUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WarehouseOperatingLicenseApplicationConfiguration : IEntityTypeConfiguration<WarehouseOperatingLicenseApplication>
{
    public void Configure(EntityTypeBuilder<WarehouseOperatingLicenseApplication> builder)
    {
        builder.ToTable("warehouse_operating_license_applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.OperatorLegalName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(256);
        builder.Property(x => x.ContactPhone).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ReviewComments).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.WarehouseFacilityId);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubmittedByUser).WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.IssuedLicense).WithMany().HasForeignKey(x => x.IssuedLicenseId).OnDelete(DeleteBehavior.SetNull);
    }
}
