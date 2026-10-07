using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriCheck.Infrastructure.Services;

public class ContainerImportComplianceGate : IContainerImportComplianceGate
{
    private readonly AgriCheckDbContext _db;
    private readonly IFacilityComplianceService _facilityCompliance;
    private readonly ComplianceOptions _options;

    public ContainerImportComplianceGate(
        AgriCheckDbContext db,
        IFacilityComplianceService facilityCompliance,
        IOptions<ComplianceOptions> options)
    {
        _db = db;
        _facilityCompliance = facilityCompliance;
        _options = options.Value;
    }

    public async Task ValidateCswReceiveAsync(
        Entry entry,
        Container container,
        long warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.RequireCswHaulCompliance || entry.EntryType != EntryType.Import)
        {
            return;
        }

        if (!await _facilityCompliance.IsDestinationReadyAsync(warehouseFacilityId, cancellationToken))
        {
            throw new ClientPortalException(
                "COMPLIANCE_DESTINATION_RSAS_CSW",
                "Warehouse must have active RSAS registration and CSW accreditation before receive.");
        }

        if (entry.FinalWarehouseFacilityId is long finalId && finalId != warehouseFacilityId)
        {
            throw new ClientPortalException(
                "COMPLIANCE_IFM_DESTINATION_MISMATCH",
                "Receive warehouse must match the entry final destination (IFM / SPSIC §3(E)).");
        }

        var clearance = await _db.SpsImportClearances
            .AsNoTracking()
            .Where(c => c.EntryId == entry.Id && c.Status == SpsImportClearanceStatus.Issued)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (clearance is null)
        {
            throw new ClientPortalException(
                "COMPLIANCE_SPSIC_REQUIRED",
                "Issued SPS Import Clearance is required before CSW receive.");
        }

        if (clearance.MustShipOutBy is DateTime mustShip && mustShip < DateTime.UtcNow)
        {
            throw new ClientPortalException(
                "COMPLIANCE_SPSIC_EXPIRED",
                "SPS Import Clearance has passed the Must Ship Out date.");
        }

        var erfi = await _db.ErfiSubmissions.AsNoTracking().FirstOrDefaultAsync(e => e.EntryId == entry.Id, cancellationToken);
        if (erfi is null || erfi.Status is ErfiSubmissionStatus.Draft)
        {
            throw new ClientPortalException(
                "COMPLIANCE_ERFI_REQUIRED",
                "File e-RFI before the container is received at the licensed CSW.");
        }

        if (container.SpsImportClearanceId is null)
        {
            container.SpsImportClearanceId = clearance.Id;
        }
        else if (container.SpsImportClearanceId != clearance.Id)
        {
            throw new ClientPortalException(
                "COMPLIANCE_SPSIC_CONTAINER_MISMATCH",
                "Container is linked to a different SPS clearance than this entry.");
        }
    }
}
