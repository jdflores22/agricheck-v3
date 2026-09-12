using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class WorkflowDemoSeeder
{
    private const string ReferencePrefix = "ENT-WF-";

    public static async Task SeedAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Entries.AnyAsync(e => e.ReferenceNo.StartsWith(ReferencePrefix), cancellationToken))
        {
            return;
        }

        var importer = await db.Users.FirstOrDefaultAsync(u => u.Email == "importer@agricheck.local", cancellationToken);
        if (importer is null)
        {
            return;
        }

        await EnsureAccreditationAsync(db, importer, cancellationToken);

        var bai = await db.Agencies.FirstAsync(a => a.Code == "BAI", cancellationToken);
        var admin = await db.Users.FirstAsync(u => u.Email == "admin@agricheck.local", cancellationToken);
        var billingAgent = await db.Users.FirstOrDefaultAsync(u => u.Email == "billing@agricheck.local", cancellationToken) ?? admin;
        var inspector = await db.Users.FirstOrDefaultAsync(u => u.Email == "inspector@agricheck.local", cancellationToken) ?? admin;
        var operatorUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "operator@agricheck.local", cancellationToken);
        var doctor = await db.Users.FirstOrDefaultAsync(u => u.Email == "doctor@agricheck.local", cancellationToken);
        var driver = await db.Users.FirstOrDefaultAsync(u => u.Email == "driver@agricheck.local", cancellationToken);
        var warehouseStaff = await db.Users.FirstOrDefaultAsync(u => u.Email == "warehouse@agricheck.local", cancellationToken) ?? admin;
        var facility = await db.WarehouseFacilities.FirstAsync(w => w.Code == "WH-MNL", cancellationToken);
        var certTemplateVersion = await db.CertificateTemplateVersions
            .Include(v => v.Template)
            .Where(v => v.IsPublished && v.Template!.AgencyId == bai.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var billCounter = 1;

        // 1) DA billing issued — client must upload payment proof
        var billingIssued = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}001", EntryStatus.DaIssueBilling, now.AddDays(-12));
        db.Entries.Add(billingIssued);
        await db.SaveChangesAsync(cancellationToken);
        db.AgencyBillings.Add(CreateAgencyBilling(
            bai.Id,
            billingIssued.Id,
            billingAgent.Id,
            $"AB-WF-{billCounter++:000}",
            AgencyBillingStatus.Issued,
            now.AddDays(-2)));

        // 2) DA billing payment pending — proof uploaded, awaiting agency verification
        var billingPending = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}002", EntryStatus.DaIssueBilling, now.AddDays(-11));
        db.Entries.Add(billingPending);
        await db.SaveChangesAsync(cancellationToken);
        db.AgencyBillings.Add(CreateAgencyBilling(
            bai.Id,
            billingPending.Id,
            billingAgent.Id,
            $"AB-WF-{billCounter++:000}",
            AgencyBillingStatus.PaymentPending,
            now.AddDays(-3),
            paymentReference: "GCASH-WF-002",
            paymentProofFile: "wf-payment-proof-002.pdf"));

        // 3) For inspection — billing paid, certificate issued, client uploads container photos
        var forInspection = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}003", EntryStatus.ForInspection, now.AddDays(-10));
        db.Entries.Add(forInspection);
        await db.SaveChangesAsync(cancellationToken);
        db.AgencyBillings.Add(CreateAgencyBilling(
            bai.Id,
            forInspection.Id,
            billingAgent.Id,
            $"AB-WF-{billCounter++:000}",
            AgencyBillingStatus.Paid,
            now.AddDays(-8),
            paidAt: now.AddDays(-7)));
        if (certTemplateVersion is not null)
        {
            db.Certificates.Add(CreateCertificate(importer.Id, forInspection.Id, bai.Id, certTemplateVersion.Id, $"{ReferencePrefix}003"));
        }

        var inspectionContainer = CreateContainer(forInspection.Id, "CONT-WF-003", 1, ContainerStatus.UnderInspection);
        db.Containers.Add(inspectionContainer);
        await db.SaveChangesAsync(cancellationToken);
        AddInspectionPhotos(db, inspectionContainer, forInspection.Id, EvaluationDecision.Pending);

        // 4) Ready for transport — all photos approved
        var readyTransport = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}004", EntryStatus.ReadyForTransport, now.AddDays(-9));
        db.Entries.Add(readyTransport);
        await db.SaveChangesAsync(cancellationToken);
        db.AgencyBillings.Add(CreateAgencyBilling(
            bai.Id,
            readyTransport.Id,
            billingAgent.Id,
            $"AB-WF-{billCounter++:000}",
            AgencyBillingStatus.Paid,
            now.AddDays(-7),
            paidAt: now.AddDays(-6)));
        if (certTemplateVersion is not null)
        {
            db.Certificates.Add(CreateCertificate(importer.Id, readyTransport.Id, bai.Id, certTemplateVersion.Id, $"{ReferencePrefix}004"));
        }

        var readyContainer = CreateContainer(readyTransport.Id, "CONT-WF-004", 1, ContainerStatus.ReadyForTransport);
        db.Containers.Add(readyContainer);
        await db.SaveChangesAsync(cancellationToken);
        AddInspectionPhotos(db, readyContainer, readyTransport.Id, EvaluationDecision.Approved, inspector.Id);

        // 5) Awaiting transport confirmation — agency tagged for AgriTrack pickup
        var awaitingTransport = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}005", EntryStatus.AwaitingTransport, now.AddDays(-8));
        db.Entries.Add(awaitingTransport);
        await db.SaveChangesAsync(cancellationToken);
        if (certTemplateVersion is not null)
        {
            db.Certificates.Add(CreateCertificate(importer.Id, awaitingTransport.Id, bai.Id, certTemplateVersion.Id, $"{ReferencePrefix}005"));
        }

        var awaitingContainer = CreateContainer(awaitingTransport.Id, "CONT-WF-005", 1, ContainerStatus.AwaitingConfirmation);
        db.Containers.Add(awaitingContainer);
        await db.SaveChangesAsync(cancellationToken);
        AddInspectionPhotos(db, awaitingContainer, awaitingTransport.Id, EvaluationDecision.Approved, inspector.Id);
        db.ContainerTransportTags.Add(new ContainerTransportTag
        {
            Uuid = Guid.NewGuid(),
            ContainerId = awaitingContainer.Id,
            EntryId = awaitingTransport.Id,
            TransportType = "warehouse_nmis",
            TaggedByUserId = inspector.Id,
            TaggedAt = now.AddDays(-2)
        });

        // 6) In transit — operator claimed and driver assigned
        if (operatorUser is not null && driver is not null)
        {
            var inTransit = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}006", EntryStatus.InTransit, now.AddDays(-7));
            db.Entries.Add(inTransit);
            await db.SaveChangesAsync(cancellationToken);
            if (certTemplateVersion is not null)
            {
                db.Certificates.Add(CreateCertificate(importer.Id, inTransit.Id, bai.Id, certTemplateVersion.Id, $"{ReferencePrefix}006"));
            }

            var transitContainer = CreateContainer(inTransit.Id, "CONT-WF-006", 1, ContainerStatus.InTransit);
            transitContainer.ClaimedByUserId = operatorUser.Id;
            transitContainer.AssignedDriverUserId = driver.Id;
            transitContainer.DepartureTime = now.AddHours(-6);
            db.Containers.Add(transitContainer);
            await db.SaveChangesAsync(cancellationToken);
            AddInspectionPhotos(db, transitContainer, inTransit.Id, EvaluationDecision.Approved, inspector.Id);
            db.ContainerTransportTags.Add(new ContainerTransportTag
            {
                Uuid = Guid.NewGuid(),
                ContainerId = transitContainer.Id,
                EntryId = inTransit.Id,
                TransportType = "warehouse_nmis",
                TaggedByUserId = inspector.Id,
                TaggedAt = now.AddDays(-3)
            });

            await EnsureDriverProfile(db, driver.Id, cancellationToken);
        }

        // 7) Doctor inspected — ready for warehouse receive
        if (doctor is not null)
        {
            var inspectedEntry = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}007", EntryStatus.InTransit, now.AddDays(-6));
            db.Entries.Add(inspectedEntry);
            await db.SaveChangesAsync(cancellationToken);
            if (certTemplateVersion is not null)
            {
                db.Certificates.Add(CreateCertificate(importer.Id, inspectedEntry.Id, bai.Id, certTemplateVersion.Id, $"{ReferencePrefix}007"));
            }

            var inspectedContainer = CreateContainer(inspectedEntry.Id, "CONT-WF-007", 1, ContainerStatus.Inspected);
            inspectedContainer.ArrivalTime = now.AddHours(-2);
            db.Containers.Add(inspectedContainer);
            await db.SaveChangesAsync(cancellationToken);
            AddInspectionPhotos(db, inspectedContainer, inspectedEntry.Id, EvaluationDecision.Approved, inspector.Id);
            db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
            {
                Uuid = Guid.NewGuid(),
                ContainerId = inspectedContainer.Id,
                DoctorUserId = doctor.Id,
                Status = DoctorInspectionStatus.Approved,
                Findings = "Demo seed: fit for warehouse storage.",
                ClaimedAt = now.AddHours(-3),
                CompletedAt = now.AddHours(-2)
            });
        }

        // 8) At warehouse — eligible for client warehouse booking (V3 enhancement over V1)
        var atWarehouseEntry = CreateEntry(importer.Id, bai.Id, $"{ReferencePrefix}008", EntryStatus.InTransit, now.AddDays(-5));
        db.Entries.Add(atWarehouseEntry);
        await db.SaveChangesAsync(cancellationToken);
        if (certTemplateVersion is not null)
        {
            db.Certificates.Add(CreateCertificate(importer.Id, atWarehouseEntry.Id, bai.Id, certTemplateVersion.Id, $"{ReferencePrefix}008"));
        }

        var warehouseContainer = CreateContainer(atWarehouseEntry.Id, "CONT-WF-008", 1, ContainerStatus.AtWarehouse);
        warehouseContainer.ArrivalTime = now.AddDays(-1);
        db.Containers.Add(warehouseContainer);
        await db.SaveChangesAsync(cancellationToken);
        AddInspectionPhotos(db, warehouseContainer, atWarehouseEntry.Id, EvaluationDecision.Approved, inspector.Id);
        if (doctor is not null)
        {
            db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
            {
                Uuid = Guid.NewGuid(),
                ContainerId = warehouseContainer.Id,
                DoctorUserId = doctor.Id,
                Status = DoctorInspectionStatus.Approved,
                Findings = "Demo seed: cleared for warehouse intake.",
                ClaimedAt = now.AddDays(-2),
                CompletedAt = now.AddDays(-1).AddHours(-2)
            });
        }

        db.WarehouseInventories.Add(new WarehouseInventory
        {
            Uuid = Guid.NewGuid(),
            ContainerId = warehouseContainer.Id,
            WarehouseFacilityId = facility.Id,
            LocationCode = "A-01",
            ReceivedAt = now.AddDays(-1),
            ReceivedByUserId = warehouseStaff.Id,
            Status = WarehouseInventoryStatus.Stored
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded V3 workflow demo pipeline ({Prefix}001–008) for importer@agricheck.local",
            ReferencePrefix);
    }

    private static async Task EnsureAccreditationAsync(AgriCheckDbContext db, User importer, CancellationToken cancellationToken)
    {
        if (await db.AccreditationSubmissions.AnyAsync(
                s => s.UserId == importer.Id && s.Status == AccreditationSubmissionStatus.Approved,
                cancellationToken))
        {
            return;
        }

        db.AccreditationSubmissions.Add(new AccreditationSubmission
        {
            Uuid = Guid.NewGuid(),
            UserId = importer.Id,
            CompanyName = importer.Profile?.CompanyName ?? "Agri Trade Corp",
            SubmissionType = "NEW",
            Status = AccreditationSubmissionStatus.Approved,
            AccreditationNumber = "DA-ACC-WF-2026-0001",
            SubmittedAt = DateTime.UtcNow.AddMonths(-3),
            FormDataJson = """{"businessName":"Agri Trade Corp","businessType":"Importer"}"""
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Entry CreateEntry(long userId, long agencyId, string referenceNo, EntryStatus status, DateTime submittedAt) =>
        new()
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = referenceNo,
            UserId = userId,
            AgencyId = agencyId,
            EntryType = EntryType.Import,
            Status = status,
            SubmittedAt = submittedAt,
            PaymentStatus = PaymentStatus.Paid,
            PaymentAmount = 2500m,
            FormDataJson = """{"commodityName":"Frozen Beef","quantity":"100","originCountry":"Australia"}"""
        };

    private static Container CreateContainer(long entryId, string containerNumber, int sequence, ContainerStatus status) =>
        new()
        {
            Uuid = Guid.NewGuid(),
            EntryId = entryId,
            SequenceNumber = sequence,
            ContainerNumber = containerNumber,
            ContainerType = "40FT",
            Status = status
        };

    private static AgencyBilling CreateAgencyBilling(
        long agencyId,
        long entryId,
        long issuedByUserId,
        string billNumber,
        AgencyBillingStatus status,
        DateTime issuedAt,
        DateTime? paidAt = null,
        string? paymentReference = null,
        string? paymentProofFile = null) =>
        new()
        {
            Uuid = Guid.NewGuid(),
            AgencyId = agencyId,
            EntryId = entryId,
            BillNumber = billNumber,
            Description = "DA inspection and processing fee",
            Amount = 5000m,
            Status = status,
            IssuedByUserId = issuedByUserId,
            IssuedAt = issuedAt,
            PaidAt = paidAt,
            PaymentReference = paymentReference,
            PaymentProofStoredFileName = paymentProofFile,
            PaymentProofOriginalFileName = paymentProofFile,
            PaymentProofContentType = paymentProofFile is null ? null : "application/pdf",
            PaymentUploadedAt = paymentProofFile is null ? null : issuedAt.AddHours(2)
        };

    private static Certificate CreateCertificate(
        long userId,
        long entryId,
        long agencyId,
        long templateVersionId,
        string referenceSeed) =>
        new()
        {
            Uuid = Guid.NewGuid(),
            UserId = userId,
            EntryId = entryId,
            AgencyId = agencyId,
            TemplateVersionId = templateVersionId,
            CertificateNumber = $"CERT-{referenceSeed}",
            VerificationCode = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            Title = "Import Entry Certificate",
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow.AddDays(-5)
        };

    private static void AddInspectionPhotos(
        AgriCheckDbContext db,
        Container container,
        long entryId,
        EvaluationDecision decision,
        long? reviewedByUserId = null)
    {
        foreach (ContainerInspectionPhotoType photoType in Enum.GetValues<ContainerInspectionPhotoType>())
        {
            db.ContainerInspectionPhotos.Add(new ContainerInspectionPhoto
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                EntryId = entryId,
                PhotoType = photoType,
                OriginalFileName = $"{photoType.ToString().ToLowerInvariant()}.jpg",
                StoredFileName = $"seed-{container.ContainerNumber}-{photoType}.jpg",
                ContentType = "image/jpeg",
                ReviewDecision = decision,
                ReviewedByUserId = decision == EvaluationDecision.Approved ? reviewedByUserId : null,
                ReviewedAt = decision == EvaluationDecision.Approved ? DateTime.UtcNow.AddDays(-1) : null
            });
        }
    }

    private static async Task EnsureDriverProfile(AgriCheckDbContext db, long driverUserId, CancellationToken cancellationToken)
    {
        if (await db.DriverProfiles.AnyAsync(p => p.UserId == driverUserId, cancellationToken))
        {
            return;
        }

        db.DriverProfiles.Add(new DriverProfile
        {
            UserId = driverUserId,
            LicenseNumber = "DL-WF-123456",
            LicenseExpiryDate = DateTime.UtcNow.AddYears(2),
            VehicleType = "6-Wheeler Reefer Truck",
            VehicleRegistration = "WF-1234",
            PhoneNumber = "+639171234567",
            CompletionPercentage = 100,
            SubmittedAt = DateTime.UtcNow.AddDays(-30)
        });
    }
}
