using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AgencyLicenseReviewService : IAgencyLicenseReviewService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AgencyLicenseReviewService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AgencyLicenseApplicationListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.AgencyLicenseApplications
            .AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u.Profile)
            .Include(a => a.LicenseType)
            .Where(a => a.AgencyId == agency.Id
                && a.Status != AgencyLicenseApplicationStatus.Draft
                && a.Status != AgencyLicenseApplicationStatus.Withdrawn);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(a => new AgencyLicenseApplicationListItemDto(
            a.Uuid,
            a.User.Profile?.CompanyName ?? a.User.Email,
            a.User.Email,
            a.LicenseType.Code,
            a.LicenseType.Name,
            a.Status.ToString(),
            a.SubmittedAt,
            a.CreatedAt)).ToList();

        return new PagedResult<AgencyLicenseApplicationListItemDto>(items, page, pageSize, total);
    }

    public async Task<AgencyLicenseApplicationDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var application = await _db.AgencyLicenseApplications
            .AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u.Profile)
            .Include(a => a.LicenseType)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.AgencyId == agency.Id, cancellationToken);

        if (application is null) return null;

        return new AgencyLicenseApplicationDetailDto(
            application.Uuid,
            application.User.Profile?.CompanyName ?? application.User.Email,
            application.User.Email,
            application.LicenseType.Code,
            application.LicenseType.Name,
            application.Status.ToString(),
            application.FormDataJson,
            application.SubmittedAt,
            application.ReviewComments,
            application.CreatedAt);
    }

    public async Task ApproveAsync(Guid uuid, ApproveAgencyLicenseApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var application = await _db.AgencyLicenseApplications
            .Include(a => a.LicenseType)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.AgencyId == agency.Id, cancellationToken);

        if (application is null)
        {
            throw new ClientPortalException("NOT_FOUND", "Application not found.");
        }

        if (application.Status is not (AgencyLicenseApplicationStatus.Submitted or AgencyLicenseApplicationStatus.UnderReview))
        {
            throw new ClientPortalException("INVALID_STATUS", "Only submitted applications can be approved.");
        }

        var licenseNumber = request.LicenseNumber.Trim();
        if (licenseNumber.Length == 0)
        {
            throw new ClientPortalException("VALIDATION", "License number is required.");
        }

        if (request.ExpiresAt <= request.IssuedAt)
        {
            throw new ClientPortalException("VALIDATION", "Expiry must be after issue date.");
        }

        var license = new AgencyLicense
        {
            Uuid = Guid.NewGuid(),
            UserId = application.UserId,
            AgencyId = application.AgencyId,
            AgencyLicenseTypeId = application.AgencyLicenseTypeId,
            ApplicationId = application.Id,
            LicenseNumber = licenseNumber,
            Status = request.ExpiresAt > DateTime.UtcNow ? AgencyLicenseStatus.Active : AgencyLicenseStatus.Expired,
            IssuedAt = request.IssuedAt.ToUniversalTime(),
            ExpiresAt = request.ExpiresAt.ToUniversalTime(),
        };

        _db.AgencyLicenses.Add(license);
        application.Status = AgencyLicenseApplicationStatus.Approved;
        application.ReviewComments = request.ReviewComments;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid uuid, RejectAgencyLicenseApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var application = await _db.AgencyLicenseApplications
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.AgencyId == agency.Id, cancellationToken);

        if (application is null)
        {
            throw new ClientPortalException("NOT_FOUND", "Application not found.");
        }

        if (application.Status is not (AgencyLicenseApplicationStatus.Submitted or AgencyLicenseApplicationStatus.UnderReview))
        {
            throw new ClientPortalException("INVALID_STATUS", "Only submitted applications can be rejected.");
        }

        application.Status = AgencyLicenseApplicationStatus.Rejected;
        application.ReviewComments = request.ReviewComments?.Trim();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
