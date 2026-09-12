using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class ClientFormService : IClientFormService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientFormService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ClientFormListItemDto>> ListPublishedAsync(long? agencyId, string formType, CancellationToken cancellationToken = default)
    {
        await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var normalizedType = formType.Trim().ToUpperInvariant();

        var query = _db.FormTemplates
            .AsNoTracking()
            .Include(t => t.AgencyTags)
            .Include(t => t.Versions)
            .Where(t => t.IsActive && t.Status == FormTemplateStatus.Published && t.FormType == normalizedType);

        if (normalizedType == "ACCREDITATION")
        {
            // Accreditation forms are global templates, not scoped to a single agency.
        }
        else if (agencyId is > 0)
        {
            query = query.Where(t => t.AgencyTags.Any(a => a.AgencyId == agencyId));
        }
        else
        {
            return Array.Empty<ClientFormListItemDto>();
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new ClientFormListItemDto(
                t.Uuid,
                t.Name,
                t.FormType,
                t.Versions.Where(v => v.IsPublished).Max(v => v.VersionNumber)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientFormDetailDto?> GetPublishedAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var template = await _db.FormTemplates
            .AsNoTracking()
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Uuid == uuid && t.IsActive && t.Status == FormTemplateStatus.Published, cancellationToken);
        if (template is null) return null;

        var version = template.Versions.Where(v => v.IsPublished).OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (version is null) return null;

        return new ClientFormDetailDto(template.Uuid, template.Name, template.FormType, version.VersionNumber, version.SchemaJson);
    }
}
