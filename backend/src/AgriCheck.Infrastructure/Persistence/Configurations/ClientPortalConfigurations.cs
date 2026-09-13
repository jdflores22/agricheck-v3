using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class CommodityCategoryConfiguration : IEntityTypeConfiguration<CommodityCategory>
{
    public void Configure(EntityTypeBuilder<CommodityCategory> builder)
    {
        builder.ToTable("commodity_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
    }
}

public class CommodityConfiguration : IEntityTypeConfiguration<Commodity>
{
    public void Configure(EntityTypeBuilder<Commodity> builder)
    {
        builder.ToTable("commodities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasOne(x => x.Category).WithMany(x => x.Commodities).HasForeignKey(x => x.CategoryId);
    }
}

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.ReferenceNo).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ReferenceNo).IsUnique();
        builder.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PaymentAmount).HasPrecision(12, 2);
        builder.Property(x => x.MavNo).HasMaxLength(64);
        builder.HasIndex(x => x.MavNo).IsUnique();
        builder.Property(x => x.ImportTrack).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.MavDocumentStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.MavRemarks).HasMaxLength(2000);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Agency).WithMany(x => x.Entries).HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.Detail).WithOne(x => x.Entry).HasForeignKey<EntryDetail>(x => x.EntryId);
        builder.HasOne(x => x.PrimaryMic).WithMany().HasForeignKey(x => x.PrimaryMicId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class EntryDetailConfiguration : IEntityTypeConfiguration<EntryDetail>
{
    public void Configure(EntityTypeBuilder<EntryDetail> builder)
    {
        builder.ToTable("entry_details");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EntryId).IsUnique();
        builder.Property(x => x.Quantity).HasPrecision(14, 3);
        builder.Property(x => x.Unit).HasMaxLength(16);
    }
}

public class EntryStatusHistoryConfiguration : IEntityTypeConfiguration<EntryStatusHistory>
{
    public void Configure(EntityTypeBuilder<EntryStatusHistory> builder)
    {
        builder.ToTable("entry_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Entry).WithMany(x => x.StatusHistory).HasForeignKey(x => x.EntryId);
    }
}

public class TimelineEventConfiguration : IEntityTypeConfiguration<TimelineEvent>
{
    public void Configure(EntityTypeBuilder<TimelineEvent> builder)
    {
        builder.ToTable("timeline_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.HasOne(x => x.Entry).WithMany(x => x.TimelineEvents).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class EntryFileConfiguration : IEntityTypeConfiguration<EntryFile>
{
    public void Configure(EntityTypeBuilder<EntryFile> builder)
    {
        builder.ToTable("entry_files");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasOne(x => x.Entry).WithMany(x => x.Files).HasForeignKey(x => x.EntryId);
    }
}

public class EntryFileVersionConfiguration : IEntityTypeConfiguration<EntryFileVersion>
{
    public void Configure(EntityTypeBuilder<EntryFileVersion> builder)
    {
        builder.ToTable("entry_file_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => new { x.EntryFileId, x.VersionNumber }).IsUnique();
        builder.HasOne(x => x.EntryFile).WithMany(x => x.Versions).HasForeignKey(x => x.EntryFileId);
    }
}

public class AccreditationSubmissionConfiguration : IEntityTypeConfiguration<AccreditationSubmission>
{
    public void Configure(EntityTypeBuilder<AccreditationSubmission> builder)
    {
        builder.ToTable("accreditation_submissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.AssignedOfficer).WithMany().HasForeignKey(x => x.AssignedOfficerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.AssignedOfficerUserId);
    }
}

public class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> builder)
    {
        builder.ToTable("submission_files");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Submission).WithMany(x => x.Files).HasForeignKey(x => x.SubmissionId);
    }
}

public class SubmissionFileVersionConfiguration : IEntityTypeConfiguration<SubmissionFileVersion>
{
    public void Configure(EntityTypeBuilder<SubmissionFileVersion> builder)
    {
        builder.ToTable("submission_file_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => new { x.SubmissionFileId, x.VersionNumber }).IsUnique();
        builder.HasOne(x => x.SubmissionFile).WithMany(x => x.Versions).HasForeignKey(x => x.SubmissionFileId);
    }
}

public class AccreditationHistoryConfiguration : IEntityTypeConfiguration<AccreditationHistory>
{
    public void Configure(EntityTypeBuilder<AccreditationHistory> builder)
    {
        builder.ToTable("accreditation_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Submission).WithMany(x => x.History).HasForeignKey(x => x.SubmissionId);
        builder.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.CertificateNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.CertificateNumber).IsUnique();
        builder.Property(x => x.VerificationCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.VerificationCode).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Entry).WithMany(x => x.Certificates).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.TemplateVersion).WithMany().HasForeignKey(x => x.TemplateVersionId);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.IssuedBy).WithMany().HasForeignKey(x => x.IssuedByUserId);
    }
}

public class WarehouseFacilityConfiguration : IEntityTypeConfiguration<WarehouseFacility>
{
    public void Configure(EntityTypeBuilder<WarehouseFacility> builder)
    {
        builder.ToTable("warehouse_facilities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(500);
        builder.Property(x => x.StreetAddress).HasMaxLength(500);
        builder.Property(x => x.ZipCode).HasMaxLength(16);
        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);
        builder.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Province).WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Barangay).WithMany().HasForeignKey(x => x.BarangayId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class WarehouseBookingConfiguration : IEntityTypeConfiguration<WarehouseBooking>
{
    public void Configure(EntityTypeBuilder<WarehouseBooking> builder)
    {
        builder.ToTable("warehouse_bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.BookingNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BookingNumber).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.WarehouseFacility).WithMany(x => x.Bookings).HasForeignKey(x => x.WarehouseFacilityId);
    }
}

public class ClientBillConfiguration : IEntityTypeConfiguration<ClientBill>
{
    public void Configure(EntityTypeBuilder<ClientBill> builder)
    {
        builder.ToTable("client_bills");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.BillNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BillNumber).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Entry).WithMany(x => x.Bills).HasForeignKey(x => x.EntryId);
        builder.HasOne<AgencyBilling>().WithMany().HasForeignKey(x => x.AgencyBillingId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.AgencyBillingId).IsUnique();
        builder.HasOne(x => x.WarehouseBooking).WithMany().HasForeignKey(x => x.WarehouseBookingId);
    }
}

public class ClientBillPaymentConfiguration : IEntityTypeConfiguration<ClientBillPayment>
{
    public void Configure(EntityTypeBuilder<ClientBillPayment> builder)
    {
        builder.ToTable("client_bill_payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasOne(x => x.ClientBill).WithMany(x => x.Payments).HasForeignKey(x => x.ClientBillId);
    }
}
