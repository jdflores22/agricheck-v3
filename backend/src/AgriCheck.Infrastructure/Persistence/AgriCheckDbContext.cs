using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence;

public class AgriCheckDbContext : DbContext
{
    public AgriCheckDbContext(DbContextOptions<AgriCheckDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<AgencyMembership> AgencyMemberships => Set<AgencyMembership>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<CommodityCategory> CommodityCategories => Set<CommodityCategory>();
    public DbSet<Commodity> Commodities => Set<Commodity>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<EntryDetail> EntryDetails => Set<EntryDetail>();
    public DbSet<EntryStatusHistory> EntryStatusHistories => Set<EntryStatusHistory>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<EntryFile> EntryFiles => Set<EntryFile>();
    public DbSet<EntryFileVersion> EntryFileVersions => Set<EntryFileVersion>();
    public DbSet<AccreditationSubmission> AccreditationSubmissions => Set<AccreditationSubmission>();
    public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
    public DbSet<SubmissionFileVersion> SubmissionFileVersions => Set<SubmissionFileVersion>();
    public DbSet<AccreditationHistory> AccreditationHistories => Set<AccreditationHistory>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<WarehouseFacility> WarehouseFacilities => Set<WarehouseFacility>();
    public DbSet<WarehouseBooking> WarehouseBookings => Set<WarehouseBooking>();
    public DbSet<ClientBill> ClientBills => Set<ClientBill>();
    public DbSet<ClientBillPayment> ClientBillPayments => Set<ClientBillPayment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<EvaluatorAssignment> EvaluatorAssignments => Set<EvaluatorAssignment>();
    public DbSet<FileEvaluation> FileEvaluations => Set<FileEvaluation>();
    public DbSet<EvaluatorNote> EvaluatorNotes => Set<EvaluatorNote>();
    public DbSet<ComplianceChecklist> ComplianceChecklists => Set<ComplianceChecklist>();
    public DbSet<ComplianceChecklistItem> ComplianceChecklistItems => Set<ComplianceChecklistItem>();
    public DbSet<EntryComplianceResult> EntryComplianceResults => Set<EntryComplianceResult>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionPhoto> InspectionPhotos => Set<InspectionPhoto>();
    public DbSet<AgencyBilling> AgencyBillings => Set<AgencyBilling>();
    public DbSet<SubmissionFileReview> SubmissionFileReviews => Set<SubmissionFileReview>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<CertificateTemplateVersion> CertificateTemplateVersions => Set<CertificateTemplateVersion>();
    public DbSet<CertificateElement> CertificateElements => Set<CertificateElement>();
    public DbSet<CertificateProcessAssignment> CertificateProcessAssignments => Set<CertificateProcessAssignment>();
    public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
    public DbSet<FormTemplateVersion> FormTemplateVersions => Set<FormTemplateVersion>();
    public DbSet<FormAgencyTag> FormAgencyTags => Set<FormAgencyTag>();
    public DbSet<ProcessingFeeConfig> ProcessingFeeConfigs => Set<ProcessingFeeConfig>();
    public DbSet<MavApplicationPeriod> MavApplicationPeriods => Set<MavApplicationPeriod>();
    public DbSet<MavCommodityAllocation> MavCommodityAllocations => Set<MavCommodityAllocation>();
    public DbSet<MavApplication> MavApplications => Set<MavApplication>();
    public DbSet<MavLicense> MavLicenses => Set<MavLicense>();
    public DbSet<MavAccount> MavAccounts => Set<MavAccount>();
    public DbSet<MavAccountTransaction> MavAccountTransactions => Set<MavAccountTransaction>();
    public DbSet<MavImportCertificate> MavImportCertificates => Set<MavImportCertificate>();
    public DbSet<MicUtilization> MicUtilizations => Set<MicUtilization>();
    public DbSet<MavAuditLog> MavAuditLogs => Set<MavAuditLog>();
    public DbSet<MavNotificationPreference> MavNotificationPreferences => Set<MavNotificationPreference>();
    public DbSet<MavHsCategory> MavHsCategories => Set<MavHsCategory>();
    public DbSet<MavHsHeading> MavHsHeadings => Set<MavHsHeading>();
    public DbSet<MavHsDetail> MavHsDetails => Set<MavHsDetail>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ContainerLocation> ContainerLocations => Set<ContainerLocation>();
    public DbSet<ContainerInspectionPhoto> ContainerInspectionPhotos => Set<ContainerInspectionPhoto>();
    public DbSet<ContainerTransportTag> ContainerTransportTags => Set<ContainerTransportTag>();
    public DbSet<ContainerDoctorInspection> ContainerDoctorInspections => Set<ContainerDoctorInspection>();
    public DbSet<WarehouseInventory> WarehouseInventories => Set<WarehouseInventory>();
    public DbSet<ReleaseAuthorization> ReleaseAuthorizations => Set<ReleaseAuthorization>();
    public DbSet<ReleaseRecord> ReleaseRecords => Set<ReleaseRecord>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
    public DbSet<DriverDocument> DriverDocuments => Set<DriverDocument>();
    public DbSet<DriverProfileHistory> DriverProfileHistories => Set<DriverProfileHistory>();
    public DbSet<MobilePushDevice> MobilePushDevices => Set<MobilePushDevice>();
    public DbSet<OfflineSyncQueue> OfflineSyncQueue => Set<OfflineSyncQueue>();
    public DbSet<FaceVerificationLog> FaceVerificationLogs => Set<FaceVerificationLog>();
    public DbSet<AddressRegion> AddressRegions => Set<AddressRegion>();
    public DbSet<AddressProvince> AddressProvinces => Set<AddressProvince>();
    public DbSet<AddressCity> AddressCities => Set<AddressCity>();
    public DbSet<AddressBarangay> AddressBarangays => Set<AddressBarangay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgriCheckDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Domain.Common.BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
