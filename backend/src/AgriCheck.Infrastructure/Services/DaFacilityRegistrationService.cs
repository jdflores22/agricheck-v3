using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.DaPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class DaFacilityRegistrationService : IDaFacilityRegistrationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DaFacilityRegistrationService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FacilityRegistrationDaReviewDto?> GetPendingAsync(long warehouseFacilityId, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var registration = await _db.FacilityRegistrations
            .AsNoTracking()
            .Include(r => r.WarehouseFacility)
            .FirstOrDefaultAsync(
                r => r.WarehouseFacilityId == warehouseFacilityId && r.Status == FacilityRegistrationStatus.Submitted,
                cancellationToken);

        return registration is null ? null : Map(registration);
    }

    public async Task<FacilityRegistrationDaReviewDto> VerifyAsync(
        long warehouseFacilityId,
        VerifyFacilityRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var registration = await _db.FacilityRegistrations
            .Include(r => r.WarehouseFacility)
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseFacilityId, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Facility registration not found.");

        if (registration.Status != FacilityRegistrationStatus.Submitted)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only submitted RSAS registrations can be verified.");
        }

        registration.ReviewComments = string.IsNullOrWhiteSpace(request.ReviewComments) ? null : request.ReviewComments.Trim();
        if (request.Approve)
        {
            registration.Status = FacilityRegistrationStatus.Registered;
            registration.RegisteredAt = DateTime.UtcNow;
            registration.ExpiresAt = DateTime.UtcNow.AddYears(1);

            var actorId = await _db.Users
                .Where(u => u.Uuid == _currentUser.UserUuid)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var linkedEntries = await _db.Entries
                .Where(e => e.FinalWarehouseFacilityId == registration.WarehouseFacilityId)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
            foreach (var entryId in linkedEntries)
            {
                ComplianceTimelineRecorder.Add(
                    _db,
                    entryId,
                    "Compliance.Rsas",
                    "RSAS registration verified",
                    $"Warehouse {registration.WarehouseFacility.Code} registered in RSAS.",
                    actorId == 0 ? null : actorId);
            }

            _db.ComplianceSyncOutbox.Add(new ComplianceSyncOutbox
            {
                EntityType = "FacilityRegistration",
                EntityId = registration.Id,
                TargetSystem = ComplianceSyncTargetSystem.Rsas,
                Operation = "RSAS_REGISTER",
                PayloadJson = $"{{\"warehouseFacilityId\":{registration.WarehouseFacilityId}}}",
            });
        }
        else
        {
            registration.Status = FacilityRegistrationStatus.Rejected;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(registration);
    }

    public async Task<FacilityAccreditationSummaryDto> RegisterCswAsync(
        long warehouseFacilityId,
        RegisterFacilityCswRequest request,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        if (request.WarehouseFacilityId != warehouseFacilityId)
        {
            throw new ClientPortalException("VALIDATION", "Warehouse id mismatch.");
        }

        var registration = await _db.FacilityRegistrations
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseFacilityId, cancellationToken);

        if (registration is null || registration.Status != FacilityRegistrationStatus.Registered)
        {
            throw new ClientPortalException("RSAS_REQUIRED", "RSAS registration must be active before CSW accreditation.");
        }

        var certificateNumber = request.CertificateNumber.Trim();
        if (certificateNumber.Length == 0)
        {
            throw new ClientPortalException("VALIDATION", "CSW certificate number is required.");
        }

        if (request.ExpiresAt <= request.AccreditedAt)
        {
            throw new ClientPortalException("VALIDATION", "CSW expiry must be after the accreditation date.");
        }

        var accreditation = new FacilityAccreditation
        {
            Uuid = Guid.NewGuid(),
            FacilityRegistrationId = registration.Id,
            WarehouseFacilityId = registration.WarehouseFacilityId,
            Status = request.ExpiresAt > DateTime.UtcNow ? FacilityAccreditationStatus.Active : FacilityAccreditationStatus.Expired,
            CertificateNumber = certificateNumber,
            LeadAgencyId = request.LeadAgencyId,
            AccreditedAt = request.AccreditedAt.ToUniversalTime(),
            ExpiresAt = request.ExpiresAt.ToUniversalTime(),
        };

        _db.FacilityAccreditations.Add(accreditation);
        await _db.SaveChangesAsync(cancellationToken);

        string? leadCode = null;
        if (request.LeadAgencyId is long agencyId)
        {
            leadCode = await _db.Agencies.AsNoTracking()
                .Where(a => a.Id == agencyId)
                .Select(a => a.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new FacilityAccreditationSummaryDto(
            accreditation.Uuid,
            accreditation.Status.ToString(),
            accreditation.CertificateNumber,
            accreditation.AccreditedAt,
            accreditation.ExpiresAt,
            leadCode);
    }

    private static FacilityRegistrationDaReviewDto Map(FacilityRegistration registration) =>
        new(
            registration.Uuid,
            registration.WarehouseFacilityId,
            registration.WarehouseFacility.Code,
            registration.WarehouseFacility.Name,
            registration.Status.ToString(),
            registration.OwnerName,
            registration.RsasExternalRef,
            registration.UpdatedAt,
            registration.ReviewComments);
}
