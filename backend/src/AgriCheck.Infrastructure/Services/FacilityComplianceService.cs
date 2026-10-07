using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class FacilityComplianceService : IFacilityComplianceService
{
    private readonly AgriCheckDbContext _db;

    public FacilityComplianceService(AgriCheckDbContext db) => _db = db;

    public async Task<FacilityDestinationStatusDto> GetDestinationStatusAsync(long warehouseFacilityId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var registration = await _db.FacilityRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseFacilityId, cancellationToken);

        var accreditation = registration is null
            ? null
            : await _db.FacilityAccreditations
                .AsNoTracking()
                .Where(a => a.WarehouseFacilityId == warehouseFacilityId)
                .OrderByDescending(a => a.AccreditedAt)
                .FirstOrDefaultAsync(cancellationToken);

        var rsasActive = registration is not null
            && registration.Status == FacilityRegistrationStatus.Registered
            && (registration.ExpiresAt is null || registration.ExpiresAt > now);

        var cswActive = accreditation is not null
            && accreditation.Status == FacilityAccreditationStatus.Active
            && (accreditation.ExpiresAt is null || accreditation.ExpiresAt > now);

        return new FacilityDestinationStatusDto(
            rsasActive,
            cswActive,
            registration?.Status.ToString() ?? "NotStarted",
            registration?.RsasExternalRef,
            accreditation?.Status.ToString() ?? "NotStarted",
            accreditation?.CertificateNumber);
    }

    public async Task<bool> IsDestinationReadyAsync(long warehouseFacilityId, CancellationToken cancellationToken = default)
    {
        var registration = await _db.FacilityRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseFacilityId, cancellationToken);

        var status = await GetDestinationStatusAsync(warehouseFacilityId, cancellationToken);
        if (registration?.IsBmbeExempt == true)
        {
            return status.HasActiveCswAccreditation;
        }

        return status.HasRsasRegistration && status.HasActiveCswAccreditation;
    }

    public async Task<IReadOnlyDictionary<long, FacilityDestinationStatusDto>> GetDestinationStatusesAsync(
        IReadOnlyList<long> warehouseFacilityIds,
        CancellationToken cancellationToken = default)
    {
        if (warehouseFacilityIds.Count == 0)
        {
            return new Dictionary<long, FacilityDestinationStatusDto>();
        }

        var ids = warehouseFacilityIds.Distinct().ToList();
        var now = DateTime.UtcNow;

        var registrations = await _db.FacilityRegistrations
            .AsNoTracking()
            .Where(r => ids.Contains(r.WarehouseFacilityId))
            .ToListAsync(cancellationToken);

        var accreditations = await _db.FacilityAccreditations
            .AsNoTracking()
            .Where(a => ids.Contains(a.WarehouseFacilityId))
            .OrderByDescending(a => a.AccreditedAt)
            .ToListAsync(cancellationToken);

        var regByWarehouse = registrations.ToDictionary(r => r.WarehouseFacilityId);
        var accByWarehouse = accreditations
            .GroupBy(a => a.WarehouseFacilityId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new Dictionary<long, FacilityDestinationStatusDto>();
        foreach (var id in ids)
        {
            regByWarehouse.TryGetValue(id, out var registration);
            accByWarehouse.TryGetValue(id, out var accreditation);

            var rsasActive = registration is not null
                && registration.Status == FacilityRegistrationStatus.Registered
                && (registration.ExpiresAt is null || registration.ExpiresAt > now);

            var cswActive = accreditation is not null
                && accreditation.Status == FacilityAccreditationStatus.Active
                && (accreditation.ExpiresAt is null || accreditation.ExpiresAt > now);

            result[id] = new FacilityDestinationStatusDto(
                rsasActive,
                cswActive,
                registration?.Status.ToString() ?? "NotStarted",
                registration?.RsasExternalRef,
                accreditation?.Status.ToString() ?? "NotStarted",
                accreditation?.CertificateNumber);
        }

        return result;
    }
}
