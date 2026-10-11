using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AgencyNewsService : IAgencyNewsService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "News",
        "Announcement",
        "Article",
    };

    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AgencyNewsService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AgencyNewsListItemDto>> ListPublishedAsync(
        string? category,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _db.AgencyNewsPosts
            .AsNoTracking()
            .Include(p => p.Agency)
            .Where(p => p.IsPublished && p.PublishedAt != null && p.PublishedAt <= now);

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var cat = NormalizeCategory(category);
            query = query.Where(p => p.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Title.Contains(term) ||
                (p.Summary != null && p.Summary.Contains(term)) ||
                p.Agency.Name.Contains(term) ||
                p.Agency.Code.Contains(term));
        }

        var rows = await query
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.PublishedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return rows.Select(MapListItem).ToList();
    }

    public async Task<AgencyNewsDetailDto?> GetPublishedAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var post = await _db.AgencyNewsPosts
            .AsNoTracking()
            .Include(p => p.Agency)
            .FirstOrDefaultAsync(
                p => p.Uuid == uuid && p.IsPublished && p.PublishedAt != null && p.PublishedAt <= now,
                cancellationToken);

        return post is null ? null : MapDetail(post);
    }

    public async Task<IReadOnlyList<AgencyNewsManageListItemDto>> ListForAgencyAsync(
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        var rows = await _db.AgencyNewsPosts
            .AsNoTracking()
            .Where(p => p.AgencyId == agency.Id)
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapManageListItem).ToList();
    }

    public async Task<AgencyNewsManageDetailDto?> GetForAgencyAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        var post = await _db.AgencyNewsPosts
            .AsNoTracking()
            .Include(p => p.Agency)
            .FirstOrDefaultAsync(p => p.Uuid == uuid && p.AgencyId == agency.Id, cancellationToken);

        return post is null ? null : MapManageDetail(post);
    }

    public async Task<AgencyNewsManageDetailDto> CreateDraftAsync(
        CreateAgencyNewsRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        ValidateRequest(request.Title, request.Body, request.Category);

        var displayOrder = request.DisplayOrder ?? await NextDisplayOrderAsync(agency.Id, cancellationToken);
        var post = new AgencyNewsPost
        {
            AgencyId = agency.Id,
            Title = request.Title.Trim(),
            Summary = request.Summary?.Trim(),
            Body = request.Body.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            Category = NormalizeCategory(request.Category),
            IsRegulatory = request.IsRegulatory,
            IsPublished = false,
            PublishedAt = null,
            DisplayOrder = displayOrder,
        };

        _db.AgencyNewsPosts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(post).Reference(p => p.Agency).LoadAsync(cancellationToken);
        return MapManageDetail(post);
    }

    public async Task<AgencyNewsManageDetailDto> UpdateAsync(
        Guid uuid,
        UpdateAgencyNewsRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        ValidateRequest(request.Title, request.Body, request.Category);

        var post = await _db.AgencyNewsPosts
            .Include(p => p.Agency)
            .FirstOrDefaultAsync(p => p.Uuid == uuid && p.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NEWS_NOT_FOUND", "News post not found.");

        post.Title = request.Title.Trim();
        post.Summary = request.Summary?.Trim();
        post.Body = request.Body.Trim();
        post.ImageUrl = request.ImageUrl?.Trim();
        post.Category = NormalizeCategory(request.Category);
        post.IsRegulatory = request.IsRegulatory;
        if (request.DisplayOrder is int order)
        {
            post.DisplayOrder = order;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapManageDetail(post);
    }

    public async Task<AgencyNewsManageDetailDto> PublishAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        var post = await _db.AgencyNewsPosts
            .Include(p => p.Agency)
            .FirstOrDefaultAsync(p => p.Uuid == uuid && p.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NEWS_NOT_FOUND", "News post not found.");

        post.IsPublished = true;
        post.PublishedAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapManageDetail(post);
    }

    public async Task<AgencyNewsManageDetailDto> UnpublishAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        var post = await _db.AgencyNewsPosts
            .Include(p => p.Agency)
            .FirstOrDefaultAsync(p => p.Uuid == uuid && p.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NEWS_NOT_FOUND", "News post not found.");

        post.IsPublished = false;
        await _db.SaveChangesAsync(cancellationToken);
        return MapManageDetail(post);
    }

    public async Task DeleteAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyNewsPublisherAsync(_db, _currentUser, cancellationToken);
        var post = await _db.AgencyNewsPosts
            .FirstOrDefaultAsync(p => p.Uuid == uuid && p.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NEWS_NOT_FOUND", "News post not found.");

        _db.AgencyNewsPosts.Remove(post);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> NextDisplayOrderAsync(long agencyId, CancellationToken cancellationToken)
    {
        var max = await _db.AgencyNewsPosts
            .Where(p => p.AgencyId == agencyId)
            .Select(p => (int?)p.DisplayOrder)
            .MaxAsync(cancellationToken);
        return (max ?? 0) + 10;
    }

    private static void ValidateRequest(string title, string body, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ClientPortalException("TITLE_REQUIRED", "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ClientPortalException("BODY_REQUIRED", "Body is required.");
        }

        if (!AllowedCategories.Contains(category))
        {
            throw new ClientPortalException("INVALID_CATEGORY", "Category must be News, Announcement, or Article.");
        }
    }

    private static string NormalizeCategory(string category)
    {
        if (string.Equals(category, "Announcements", StringComparison.OrdinalIgnoreCase))
        {
            return "Announcement";
        }

        if (string.Equals(category, "Articles", StringComparison.OrdinalIgnoreCase))
        {
            return "Article";
        }

        return AllowedCategories.First(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    private static AgencyNewsListItemDto MapListItem(AgencyNewsPost post) =>
        new(
            post.Uuid,
            post.Title,
            post.Summary,
            post.ImageUrl,
            post.Category,
            post.IsRegulatory,
            post.Agency.Code,
            post.Agency.Name,
            post.PublishedAt ?? post.CreatedAt);

    private static AgencyNewsDetailDto MapDetail(AgencyNewsPost post) =>
        new(
            post.Uuid,
            post.Title,
            post.Summary,
            post.Body,
            post.ImageUrl,
            post.Category,
            post.IsRegulatory,
            post.Agency.Code,
            post.Agency.Name,
            post.PublishedAt ?? post.CreatedAt);

    private static AgencyNewsManageListItemDto MapManageListItem(AgencyNewsPost post) =>
        new(
            post.Uuid,
            post.Title,
            post.Category,
            post.IsRegulatory,
            post.IsPublished,
            post.PublishedAt,
            post.DisplayOrder,
            post.UpdatedAt);

    private static AgencyNewsManageDetailDto MapManageDetail(AgencyNewsPost post) =>
        new(
            post.Uuid,
            post.Title,
            post.Summary,
            post.Body,
            post.ImageUrl,
            post.Category,
            post.IsRegulatory,
            post.IsPublished,
            post.PublishedAt,
            post.DisplayOrder,
            post.Agency.Code,
            post.Agency.Name,
            post.CreatedAt,
            post.UpdatedAt);
}
