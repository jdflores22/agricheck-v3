using System.Text;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

internal static class MavHsDisplayHelper
{
    public static string BuildDetailLabel(MavHsDetail detail) =>
        $"{detail.Heading.Category.HsCode}.{detail.Heading.HeadingNumber} — {detail.Description}";

    public static string BuildHsCode(MavHsDetail detail) =>
        $"{detail.Heading.Category.HsCode}{detail.Heading.HeadingNumber}";
}

public class MavHsLibraryService : IMavHsLibraryService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavHsLibraryService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MavHsCategoryListItemDto>> ListCategoriesAsync(long? agencyId, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var query = _db.MavHsCategories.Include(c => c.Agency).Include(c => c.Headings).AsQueryable();
        if (agencyId is not null)
        {
            query = query.Where(c => c.AgencyId == agencyId || c.AgencyId == null);
        }

        return await query
            .OrderBy(c => c.HsCode)
            .Select(c => new MavHsCategoryListItemDto(
                c.Uuid, c.HsCode, c.Description, c.Agency != null ? c.Agency.Code : null, c.IsActive, c.Headings.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<MavHsCategoryDetailDto?> GetCategoryAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var category = await _db.MavHsCategories
            .Include(c => c.Agency)
            .Include(c => c.Headings)
            .ThenInclude(h => h.Details)
            .FirstOrDefaultAsync(c => c.Uuid == uuid, cancellationToken);
        return category is null ? null : MapCategory(category);
    }

    public async Task<MavHsCategoryDetailDto> CreateCategoryAsync(CreateMavHsCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var hsCode = request.HsCode.Trim();
        if (await _db.MavHsCategories.AnyAsync(c => c.HsCode == hsCode, cancellationToken))
        {
            throw new ClientPortalException("HS_CODE_EXISTS", "HS category code already exists.");
        }

        var category = new MavHsCategory
        {
            Uuid = Guid.NewGuid(),
            HsCode = hsCode,
            Description = request.Description.Trim(),
            Notes = request.Notes?.Trim(),
            AgencyId = request.AgencyId,
            IsActive = true
        };
        _db.MavHsCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return MapCategory(await _db.MavHsCategories
            .Include(c => c.Agency)
            .Include(c => c.Headings)
            .ThenInclude(h => h.Details)
            .FirstAsync(c => c.Id == category.Id, cancellationToken));
    }

    public async Task<MavHsCategoryDetailDto> UpdateCategoryAsync(Guid uuid, UpdateMavHsCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var category = await _db.MavHsCategories
            .Include(c => c.Agency)
            .Include(c => c.Headings)
            .ThenInclude(h => h.Details)
            .FirstOrDefaultAsync(c => c.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS category not found.");

        var hsCode = request.HsCode.Trim();
        if (await _db.MavHsCategories.AnyAsync(c => c.HsCode == hsCode && c.Id != category.Id, cancellationToken))
        {
            throw new ClientPortalException("HS_CODE_EXISTS", "HS category code already exists.");
        }

        category.HsCode = hsCode;
        category.Description = request.Description.Trim();
        category.Notes = request.Notes?.Trim();
        category.AgencyId = request.AgencyId;
        category.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task<IReadOnlyList<MavHsHeadingListItemDto>> ListHeadingsAsync(Guid categoryUuid, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var category = await _db.MavHsCategories.FirstOrDefaultAsync(c => c.Uuid == categoryUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS category not found.");

        return await _db.MavHsHeadings
            .Where(h => h.CategoryId == category.Id)
            .OrderBy(h => h.HeadingNumber)
            .Select(h => new MavHsHeadingListItemDto(h.Uuid, h.HeadingNumber, h.Description, h.IsActive, h.Details.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<MavHsHeadingListItemDto> CreateHeadingAsync(Guid categoryUuid, CreateMavHsHeadingRequest request, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var category = await _db.MavHsCategories.FirstOrDefaultAsync(c => c.Uuid == categoryUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS category not found.");

        var headingNumber = request.HeadingNumber.Trim();
        if (await _db.MavHsHeadings.AnyAsync(h => h.CategoryId == category.Id && h.HeadingNumber == headingNumber, cancellationToken))
        {
            throw new ClientPortalException("HEADING_EXISTS", "Heading number already exists for this category.");
        }

        var heading = new MavHsHeading
        {
            Uuid = Guid.NewGuid(),
            CategoryId = category.Id,
            HeadingNumber = headingNumber,
            Description = request.Description.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };
        _db.MavHsHeadings.Add(heading);
        await _db.SaveChangesAsync(cancellationToken);
        return new MavHsHeadingListItemDto(heading.Uuid, heading.HeadingNumber, heading.Description, heading.IsActive, 0);
    }

    public async Task<MavHsHeadingListItemDto> UpdateHeadingAsync(Guid uuid, UpdateMavHsHeadingRequest request, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var heading = await _db.MavHsHeadings.FirstOrDefaultAsync(h => h.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS heading not found.");

        var headingNumber = request.HeadingNumber.Trim();
        if (await _db.MavHsHeadings.AnyAsync(h => h.CategoryId == heading.CategoryId && h.HeadingNumber == headingNumber && h.Id != heading.Id, cancellationToken))
        {
            throw new ClientPortalException("HEADING_EXISTS", "Heading number already exists for this category.");
        }

        heading.HeadingNumber = headingNumber;
        heading.Description = request.Description.Trim();
        heading.Notes = request.Notes?.Trim();
        heading.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        var detailCount = await _db.MavHsDetails.CountAsync(d => d.HeadingId == heading.Id, cancellationToken);
        return new MavHsHeadingListItemDto(heading.Uuid, heading.HeadingNumber, heading.Description, heading.IsActive, detailCount);
    }

    public async Task<IReadOnlyList<MavHsDetailListItemDto>> ListDetailsAsync(Guid headingUuid, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var heading = await _db.MavHsHeadings.Include(h => h.Category).FirstOrDefaultAsync(h => h.Uuid == headingUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS heading not found.");

        return await _db.MavHsDetails
            .Where(d => d.HeadingId == heading.Id)
            .OrderBy(d => d.Description)
            .Select(d => new MavHsDetailListItemDto(
                d.Uuid,
                d.Description,
                $"{heading.Category.HsCode}.{heading.HeadingNumber} — {d.Description}",
                heading.Category.HsCode,
                heading.HeadingNumber,
                d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MavHsDetailListItemDto>> ListPickerDetailsAsync(long? agencyId, Guid? categoryUuid, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var query = _db.MavHsDetails
            .Include(d => d.Heading).ThenInclude(h => h.Category).ThenInclude(c => c.Agency)
            .Where(d => d.IsActive && d.Heading.IsActive && d.Heading.Category.IsActive);

        if (agencyId is not null)
        {
            query = query.Where(d => d.Heading.Category.AgencyId == agencyId || d.Heading.Category.AgencyId == null);
        }

        if (categoryUuid is not null)
        {
            query = query.Where(d => d.Heading.Category.Uuid == categoryUuid);
        }

        return await query
            .OrderBy(d => d.Heading.Category.HsCode)
            .ThenBy(d => d.Heading.HeadingNumber)
            .ThenBy(d => d.Description)
            .Select(d => new MavHsDetailListItemDto(
                d.Uuid,
                d.Description,
                $"{d.Heading.Category.HsCode}.{d.Heading.HeadingNumber} — {d.Description}",
                d.Heading.Category.HsCode,
                d.Heading.HeadingNumber,
                d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<MavHsDetailListItemDto> CreateDetailAsync(Guid headingUuid, CreateMavHsDetailRequest request, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var heading = await _db.MavHsHeadings.Include(h => h.Category).FirstOrDefaultAsync(h => h.Uuid == headingUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS heading not found.");

        var detail = new MavHsDetail
        {
            Uuid = Guid.NewGuid(),
            HeadingId = heading.Id,
            Description = request.Description.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };
        _db.MavHsDetails.Add(detail);
        await _db.SaveChangesAsync(cancellationToken);
        return new MavHsDetailListItemDto(
            detail.Uuid,
            detail.Description,
            MavHsDisplayHelper.BuildDetailLabel(await _db.MavHsDetails.Include(d => d.Heading).ThenInclude(h => h.Category).FirstAsync(d => d.Id == detail.Id, cancellationToken)),
            heading.Category.HsCode,
            heading.HeadingNumber,
            detail.IsActive);
    }

    public async Task<MavHsDetailListItemDto> UpdateDetailAsync(Guid uuid, UpdateMavHsDetailRequest request, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var detail = await _db.MavHsDetails.Include(d => d.Heading).ThenInclude(h => h.Category)
            .FirstOrDefaultAsync(d => d.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "HS detail not found.");

        detail.Description = request.Description.Trim();
        detail.Notes = request.Notes?.Trim();
        detail.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new MavHsDetailListItemDto(
            detail.Uuid,
            detail.Description,
            MavHsDisplayHelper.BuildDetailLabel(detail),
            detail.Heading.Category.HsCode,
            detail.Heading.HeadingNumber,
            detail.IsActive);
    }

    internal static async Task<(string HsCode, string CommodityName, long? DetailId)> ResolveApplicationCommodityAsync(
        AgriCheckDbContext db,
        CreateMavApplicationRequest request,
        CancellationToken cancellationToken,
        long? periodAgencyId = null)
    {
        if (request.HsDetailUuid is null)
        {
            return (request.HsCode.Trim(), request.CommodityName.Trim(), null);
        }

        var detail = await db.MavHsDetails
            .Include(d => d.Heading).ThenInclude(h => h.Category)
            .FirstOrDefaultAsync(d => d.Uuid == request.HsDetailUuid && d.IsActive, cancellationToken)
            ?? throw new ClientPortalException("HS_DETAIL_NOT_FOUND", "Selected HS commodity not found.");

        var categoryAgencyId = detail.Heading.Category.AgencyId;
        if (periodAgencyId.HasValue && categoryAgencyId.HasValue && categoryAgencyId != periodAgencyId)
        {
            throw new ClientPortalException("HS_AGENCY_MISMATCH", "Selected HS code is not assigned to this application period's agency.");
        }

        return (MavHsDisplayHelper.BuildHsCode(detail), detail.Description.Trim(), detail.Id);
    }

    private static MavHsCategoryDetailDto MapCategory(MavHsCategory category) => new(
        category.Uuid,
        category.HsCode,
        category.Description,
        category.Notes,
        category.AgencyId,
        category.Agency?.Code,
        category.IsActive,
        category.Headings
            .OrderBy(h => h.HeadingNumber)
            .Select(h => new MavHsHeadingWithDetailsDto(
                h.Uuid,
                h.HeadingNumber,
                h.Description,
                h.IsActive,
                h.Details
                    .OrderBy(d => d.Description)
                    .Select(d => new MavHsDetailSummaryDto(
                        d.Uuid,
                        d.Description,
                        MavHsDisplayHelper.BuildDetailLabel(d),
                        d.IsActive))
                    .ToList()))
            .ToList());
}
