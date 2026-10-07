using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.IntegrationTests.Support;

internal static class ComplianceTestData
{
    public sealed record WiredImportContext(
        AgriCheckDbContext Db,
        EndToEndTestData.Context Base,
        User AgencyEvaluator,
        Entry Entry,
        WarehouseFacility Facility,
        SpsImportClearance Clearance,
        Container Container);

    public static async Task<WiredImportContext> CreatePortReadyImportAsync()
    {
        var baseCtx = await EndToEndTestData.CreateAsync();
        var db = baseCtx.Db;
        var facility = await db.WarehouseFacilities.FirstAsync();

        await SeedImporterComplianceAsync(db, baseCtx.Importer.Id, baseCtx.Agency.Id, facility.Id);
        await SeedAccreditationAsync(db, baseCtx.Importer.Id);

        var evaluator = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "evaluator.compliance@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "Agency", LastName = "Evaluator" }
        };
        evaluator.AgencyMemberships.Add(new AgencyMembership { Agency = baseCtx.Agency, IsPrimary = true });
        db.Users.Add(evaluator);
        await db.SaveChangesAsync();

        var entry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = "ENT-P8-SPINE-001",
            UserId = baseCtx.Importer.Id,
            AgencyId = baseCtx.Agency.Id,
            EntryType = EntryType.Import,
            Status = EntryStatus.Approved,
            PaymentStatus = PaymentStatus.Paid,
            FinalWarehouseFacilityId = facility.Id,
            CompliancePhase = EntryCompliancePhase.SpsicIssued,
            SubmittedAt = DateTime.UtcNow.AddDays(-3)
        };
        db.Entries.Add(entry);
        await db.SaveChangesAsync();

        db.EntryDetails.Add(new EntryDetail
        {
            EntryId = entry.Id,
            CommodityName = "Beef Products",
            Quantity = 100m,
            Unit = "KG"
        });

        var license = await db.AgencyLicenses.FirstAsync(l => l.UserId == baseCtx.Importer.Id);
        var application = new SpsImportClearanceApplication
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            UserId = baseCtx.Importer.Id,
            AgencyId = baseCtx.Agency.Id,
            AgencyLicenseId = license.Id,
            Status = SpsImportClearanceApplicationStatus.Approved
        };
        db.SpsImportClearanceApplications.Add(application);
        await db.SaveChangesAsync();

        var clearance = new SpsImportClearance
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            ApplicationId = application.Id,
            IssuingAgencyId = baseCtx.Agency.Id,
            ClearanceNumber = "SPSIC-TEST-001",
            Status = SpsImportClearanceStatus.Issued,
            IssuedAt = DateTime.UtcNow.AddDays(-2),
            MustShipOutBy = DateTime.UtcNow.AddMonths(1),
            FinalWarehouseFacilityId = facility.Id
        };
        db.SpsImportClearances.Add(clearance);
        application.IssuedClearance = clearance;

        db.ErfiSubmissions.Add(new ErfiSubmission
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            SpsImportClearanceId = clearance.Id,
            FiledByUserId = baseCtx.Importer.Id,
            Status = ErfiSubmissionStatus.Filed,
            FiledAt = DateTime.UtcNow.AddDays(-1),
            BolReference = "BOL-TEST-001"
        });

        var container = new Container
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            ContainerNumber = "CONT-P8-001",
            Status = ContainerStatus.Inspected,
            AssignedDriverUserId = baseCtx.Driver.Id,
            SpsImportClearanceId = clearance.Id
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();

        db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            DoctorUserId = baseCtx.WarehouseStaff.Id,
            Status = DoctorInspectionStatus.Approved,
            CompletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return new WiredImportContext(db, baseCtx, evaluator, entry, facility, clearance, container);
    }

    public static async Task SeedImporterComplianceAsync(
        AgriCheckDbContext db,
        long userId,
        long agencyId,
        long warehouseFacilityId)
    {
        if (!await db.WarehouseOperatingLicenses.AnyAsync(l => l.WarehouseFacilityId == warehouseFacilityId))
        {
            db.WarehouseOperatingLicenses.Add(new WarehouseOperatingLicense
            {
                WarehouseFacilityId = warehouseFacilityId,
                LicenseNumber = $"WH-LTO-{warehouseFacilityId}",
                Status = WarehouseOperatingLicenseStatus.Active,
                IssuedAt = DateTime.UtcNow.AddYears(-1),
                ExpiresAt = DateTime.UtcNow.AddYears(2),
                IssuedByUserId = userId,
            });
            await db.SaveChangesAsync();
        }

        if (!await db.ImporterFacilityAffiliations.AnyAsync(a => a.UserId == userId && a.WarehouseFacilityId == warehouseFacilityId))
        {
            db.ImporterFacilityAffiliations.Add(new ImporterFacilityAffiliation
            {
                UserId = userId,
                WarehouseFacilityId = warehouseFacilityId,
            });
            await db.SaveChangesAsync();
        }

        if (!await db.FacilityRegistrations.AnyAsync(r => r.WarehouseFacilityId == warehouseFacilityId))
        {
            var registration = new FacilityRegistration
            {
                Uuid = Guid.NewGuid(),
                WarehouseFacilityId = warehouseFacilityId,
                SubmittedByUserId = userId,
                Status = FacilityRegistrationStatus.Registered,
                RsasExternalRef = "RSAS-TEST-001",
                OwnerName = "Test Owner",
                RegisteredAt = DateTime.UtcNow.AddMonths(-1),
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };
            db.FacilityRegistrations.Add(registration);
            await db.SaveChangesAsync();

            db.FacilityAccreditations.Add(new FacilityAccreditation
            {
                Uuid = Guid.NewGuid(),
                FacilityRegistrationId = registration.Id,
                WarehouseFacilityId = warehouseFacilityId,
                Status = FacilityAccreditationStatus.Active,
                CertificateNumber = "CSW-TEST-001",
                AccreditedAt = DateTime.UtcNow.AddMonths(-1),
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            });
        }

        var licenseType = await db.AgencyLicenseTypes.FirstOrDefaultAsync(t => t.AgencyId == agencyId);
        if (licenseType is null)
        {
            licenseType = new AgencyLicenseType
            {
                AgencyId = agencyId,
                Code = "LTO",
                Name = "License to Operate",
                Kind = AgencyLicenseKind.Lto,
                IsRequiredForSpsic = true
            };
            db.AgencyLicenseTypes.Add(licenseType);
            await db.SaveChangesAsync();
        }

        if (!await db.AgencyLicenses.AnyAsync(l => l.UserId == userId && l.AgencyId == agencyId))
        {
            db.AgencyLicenses.Add(new AgencyLicense
            {
                Uuid = Guid.NewGuid(),
                UserId = userId,
                AgencyId = agencyId,
                AgencyLicenseTypeId = licenseType.Id,
                LicenseNumber = $"LTO-TEST-{agencyId}-{userId}",
                Status = AgencyLicenseStatus.Active,
                IssuedAt = DateTime.UtcNow.AddMonths(-6),
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            });
        }

        await db.SaveChangesAsync();
    }

    public static async Task SeedAccreditationAsync(AgriCheckDbContext db, long userId)
    {
        if (await db.AccreditationSubmissions.AnyAsync(s => s.UserId == userId))
        {
            return;
        }

        db.AccreditationSubmissions.Add(new AccreditationSubmission
        {
            Uuid = Guid.NewGuid(),
            UserId = userId,
            CompanyName = "Test Corp",
            SubmissionType = "NEW",
            Status = AccreditationSubmissionStatus.Approved,
            SubmittedAt = DateTime.UtcNow.AddMonths(-2)
        });
        await db.SaveChangesAsync();
    }
}
