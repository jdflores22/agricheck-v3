using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.AdminPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Auth;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgriCheck.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AgriCheckDbContext _db;

    public AdminDashboardService(AgriCheckDbContext db) => _db = db;

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        return new AdminDashboardDto(
            await _db.Users.CountAsync(cancellationToken),
            await _db.Users.CountAsync(u => u.Status == UserStatus.Active, cancellationToken),
            await _db.Agencies.CountAsync(a => a.IsActive, cancellationToken),
            await _db.Entries.CountAsync(cancellationToken),
            await _db.Certificates.CountAsync(cancellationToken),
            await _db.Certificates.CountAsync(c => c.Status == CertificateStatus.Active, cancellationToken),
            await _db.FormTemplates.CountAsync(cancellationToken),
            await _db.CertificateTemplates.CountAsync(cancellationToken),
            await _db.AuditLogs.CountAsync(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-7), cancellationToken));
    }
}

public class AdminUserService : IAdminUserService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordService _passwordService;

    public AdminUserService(AgriCheckDbContext db, ICurrentUserService currentUser, IPasswordService passwordService)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordService = passwordService;
    }

    public async Task<PagedResult<AdminUserListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var query = _db.Users.Include(u => u.Profile).Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => MapListItem(u)).ToListAsync(cancellationToken);
        return new PagedResult<AdminUserListItemDto>(items, page, pageSize, total);
    }

    public async Task<AdminUserDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var user = await _db.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.AgencyMemberships)
            .FirstOrDefaultAsync(u => u.Uuid == uuid, cancellationToken);

        return user is null ? null : MapDetail(user);
    }

    public async Task<AdminUserListItemDto> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.Trim(), cancellationToken))
        {
            throw new ClientPortalException("EMAIL_EXISTS", "Email is already registered.");
        }

        var user = new User
        {
            Uuid = Guid.NewGuid(),
            Email = request.Email.Trim(),
            PasswordHash = _passwordService.Hash(request.Password),
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            Profile = new UserProfile { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim() }
        };

        foreach (var roleCode in request.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Code == roleCode, cancellationToken)
                ?? throw new ClientPortalException("ROLE_NOT_FOUND", $"Role {roleCode} not found.");
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
        }

        if (request.AgencyId is not null)
        {
            ApplyAgencyMemberships(user, new Dictionary<string, IReadOnlyList<long>>
            {
                ["ROLE_EVALUATOR"] = new List<long> { request.AgencyId.Value }
            });
        }
        else if (request.RoleAgencies is not null)
        {
            ApplyAgencyMemberships(user, request.RoleAgencies);
        }

        _db.Users.Add(user);
        AuditLogHelper.Write(_db, admin.Id, "user_created", "user", user.Uuid.ToString(), new { user.Email });
        await _db.SaveChangesAsync(cancellationToken);
        return MapListItem(await _db.Users.Include(u => u.Profile).Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstAsync(u => u.Id == user.Id, cancellationToken));
    }

    public async Task<AdminUserListItemDto> UpdateAsync(Guid uuid, UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var user = await _db.Users.Include(u => u.Profile).Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.AgencyMemberships)
            .FirstOrDefaultAsync(u => u.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("USER_NOT_FOUND", "User not found.");

        if (request.FirstName is not null) user.Profile!.FirstName = request.FirstName.Trim();
        if (request.LastName is not null) user.Profile!.LastName = request.LastName.Trim();
        if (request.Status is not null && Enum.TryParse<UserStatus>(request.Status, true, out var status))
        {
            user.Status = status;
        }

        if (request.RoleCodes is not null)
        {
            user.UserRoles.Clear();
            foreach (var roleCode in request.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = await _db.Roles.FirstAsync(r => r.Code == roleCode, cancellationToken);
                user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
            }
        }

        if (request.AgencyId is not null)
        {
            ApplyAgencyMemberships(user, new Dictionary<string, IReadOnlyList<long>>
            {
                ["ROLE_EVALUATOR"] = new List<long> { request.AgencyId.Value }
            });
        }
        else if (request.RoleAgencies is not null)
        {
            ApplyAgencyMemberships(user, request.RoleAgencies);
        }

        AuditLogHelper.Write(_db, admin.Id, "user_updated", "user", user.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
        return MapListItem(user);
    }

    private static void ApplyAgencyMemberships(User user, IReadOnlyDictionary<string, IReadOnlyList<long>> roleAgencies)
    {
        var agencyIds = roleAgencies.Values
            .SelectMany(ids => ids)
            .Distinct()
            .ToList();

        user.AgencyMemberships.Clear();
        for (var i = 0; i < agencyIds.Count; i++)
        {
            user.AgencyMemberships.Add(new AgencyMembership
            {
                AgencyId = agencyIds[i],
                IsPrimary = i == 0
            });
        }
    }

    private static AdminUserListItemDto MapListItem(User u) => new(
        u.Uuid,
        u.Email,
        u.Status.ToString(),
        u.Profile is not null ? $"{u.Profile.FirstName} {u.Profile.LastName}" : u.Email,
        u.UserRoles.Select(r => r.Role.Code).OrderBy(c => c).ToList(),
        u.CreatedAt,
        u.LastLoginAt);

    private static AdminUserDetailDto MapDetail(User u)
    {
        var agencyIds = u.AgencyMemberships.Select(m => m.AgencyId).Distinct().OrderBy(id => id).ToList();
        var roleCodes = u.UserRoles.Select(r => r.Role.Code).OrderBy(RoleDefinitions.SortKey).ToList();
        var roleAgencies = new Dictionary<string, IReadOnlyList<long>>();

        foreach (var roleCode in roleCodes.Where(RoleDefinitions.AgencyRoleCodes.Contains))
        {
            roleAgencies[roleCode] = agencyIds;
        }

        return new AdminUserDetailDto(
            u.Uuid,
            u.Email,
            u.Status.ToString(),
            u.Profile?.FirstName ?? string.Empty,
            u.Profile?.LastName ?? string.Empty,
            u.Profile?.Phone,
            u.Profile?.CompanyName,
            roleCodes,
            agencyIds,
            roleAgencies,
            u.CreatedAt,
            u.LastLoginAt);
    }

    private static AdminUserListItemDto Map(User u) => MapListItem(u);
}

public class AdminAgencyService : IAdminAgencyService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public AdminAgencyService(AgriCheckDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<AdminAgencyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        return await _db.Agencies
            .Include(a => a.Parent)
            .OrderBy(a => a.ParentId.HasValue ? 1 : 0)
            .ThenBy(a => a.Parent != null ? a.Parent.Name : a.Name)
            .ThenBy(a => a.Name)
            .Select(a => new AdminAgencyDto(
                a.Id,
                a.Code,
                a.Name,
                a.Description,
                a.ParentId,
                a.Parent != null ? a.Parent.Name : null,
                a.IsActive,
                a.Memberships.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminAgencyDetailDto?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var agency = await _db.Agencies
            .Include(a => a.Parent)
            .Include(a => a.Memberships).ThenInclude(m => m.User).ThenInclude(u => u.Profile)
            .Include(a => a.Memberships).ThenInclude(m => m.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (agency is null) return null;

        var totalEntries = await _db.Entries.CountAsync(e => e.AgencyId == id, cancellationToken);
        var users = agency.Memberships
            .OrderBy(m => m.User.Profile != null ? $"{m.User.Profile.FirstName} {m.User.Profile.LastName}" : m.User.Email)
            .Select(m => new AdminAgencyUserDto(
                m.User.Uuid,
                m.User.Profile is not null ? $"{m.User.Profile.FirstName} {m.User.Profile.LastName}".Trim() : m.User.Email,
                m.User.Email,
                m.User.UserRoles.Select(r => r.Role.Code).OrderBy(c => c).ToList(),
                m.JoinedAt))
            .ToList();

        var secretary = users
            .Where(u => u.Roles.Contains("ROLE_SECRETARY"))
            .Select(u => new AdminAgencyLeadershipUserDto(u.UserUuid, u.FullName, u.Email, u.AssignedAt))
            .FirstOrDefault();

        var undersecretaries = users
            .Where(u => u.Roles.Contains("ROLE_UNDERSECRETARY"))
            .Select(u => new AdminAgencyLeadershipUserDto(u.UserUuid, u.FullName, u.Email, u.AssignedAt))
            .ToList();

        return new AdminAgencyDetailDto(
            agency.Id,
            agency.Code,
            agency.Name,
            agency.Description,
            agency.Address,
            agency.ContactNumber,
            agency.Email,
            agency.LogoUrl,
            agency.ParentId,
            agency.Parent?.Name,
            agency.IsActive,
            users.Count,
            totalEntries,
            secretary,
            undersecretaries,
            users);
    }

    public async Task<AdminAgencyDto> CreateAsync(CreateAgencyRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Agencies.AnyAsync(a => a.Code == code, cancellationToken))
        {
            throw new ClientPortalException("CODE_EXISTS", "Agency code already exists.");
        }

        Agency? parent = null;
        if (request.ParentId is not null)
        {
            parent = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == request.ParentId, cancellationToken)
                ?? throw new ClientPortalException("NOT_FOUND", "Parent agency not found.");
        }

        var agency = new Agency
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Address = NormalizeOptional(request.Address),
            ContactNumber = NormalizeOptional(request.ContactNumber),
            Email = NormalizeOptional(request.Email),
            Parent = parent,
            IsActive = true,
        };
        _db.Agencies.Add(agency);
        AuditLogHelper.Write(_db, admin.Id, "agency_created", "agency", code);
        await _db.SaveChangesAsync(cancellationToken);
        return MapListItem(agency, parent?.Name);
    }

    public async Task<AdminAgencyDto> UpdateAsync(long id, UpdateAgencyRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var agency = await _db.Agencies
            .Include(a => a.Parent)
            .Include(a => a.Memberships)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Agency not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Agencies.AnyAsync(a => a.Code == code && a.Id != id, cancellationToken))
        {
            throw new ClientPortalException("CODE_EXISTS", "Agency code already exists.");
        }

        Agency? parent = null;
        if (request.ParentId is not null)
        {
            if (request.ParentId == id)
            {
                throw new ClientPortalException("INVALID_PARENT", "An agency cannot be its own parent.");
            }

            parent = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == request.ParentId, cancellationToken)
                ?? throw new ClientPortalException("NOT_FOUND", "Parent agency not found.");
        }

        agency.Code = code;
        agency.Name = request.Name.Trim();
        agency.Description = NormalizeOptional(request.Description);
        agency.Address = NormalizeOptional(request.Address);
        agency.ContactNumber = NormalizeOptional(request.ContactNumber);
        agency.Email = NormalizeOptional(request.Email);
        agency.Parent = parent;
        agency.ParentId = parent?.Id;
        agency.IsActive = request.IsActive;
        AuditLogHelper.Write(_db, admin.Id, "agency_updated", "agency", agency.Code);
        await _db.SaveChangesAsync(cancellationToken);
        return MapListItem(agency, parent?.Name);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var agency = await _db.Agencies
            .Include(a => a.Children)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Agency not found.");

        if (agency.Children.Count > 0)
        {
            throw new ClientPortalException("HAS_CHILDREN", "Cannot delete an agency that has child agencies.");
        }

        if (await _db.Entries.AnyAsync(e => e.AgencyId == id, cancellationToken))
        {
            throw new ClientPortalException("HAS_ENTRIES", "Cannot delete an agency that has entries.");
        }

        _db.Agencies.Remove(agency);
        AuditLogHelper.Write(_db, admin.Id, "agency_deleted", "agency", agency.Code);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminAgencyDetailDto> UploadLogoAsync(
        long id,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Agency not found.");

        var extension = ImageUploadHelper.ResolveAgencyLogoExtension(fileName, contentType)
            ?? throw new ClientPortalException("INVALID_FILE", "Logo uploads must be PNG, JPG, GIF, WEBP, or SVG files.");

        var uploadName = $"agency-{agency.Id}{extension}";
        var (storedFileName, _) = await _fileStorage.SaveAsync(fileStream, "agency-logos", uploadName, cancellationToken);
        agency.LogoUrl = $"/uploads/agency-logos/{storedFileName}";
        AuditLogHelper.Write(_db, admin.Id, "agency_logo_uploaded", "agency", agency.Code);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<AdminAgencyDetailDto> AssignSecretaryAsync(
        long id,
        AssignAgencySecretaryRequest request,
        CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Agency not found.");

        var currentSecretaries = await LoadAgencyLeadershipUsersAsync(id, "ROLE_SECRETARY", cancellationToken);
        foreach (var current in currentSecretaries)
        {
            if (request.UserUuid is not null && current.Uuid == request.UserUuid.Value)
            {
                continue;
            }

            RemoveAgencyRoleFromUser(current, id, "ROLE_SECRETARY");
        }

        if (request.UserUuid is not null)
        {
            var user = await LoadUserForLeadershipAssignmentAsync(request.UserUuid.Value, cancellationToken);
            await EnsureUserHasRoleAsync(user, "ROLE_SECRETARY", cancellationToken);
            EnsureAgencyMembership(user, id);
        }

        AuditLogHelper.Write(_db, admin.Id, "agency_secretary_assigned", "agency", agency.Code, new { request.UserUuid });
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<AdminAgencyDetailDto> AssignUndersecretariesAsync(
        long id,
        AssignAgencyUndersecretariesRequest request,
        CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Agency not found.");

        var requestedUuids = request.UserUuids.Distinct().ToHashSet();
        var currentUndersecretaries = await LoadAgencyLeadershipUsersAsync(id, "ROLE_UNDERSECRETARY", cancellationToken);
        var currentUuids = currentUndersecretaries.Select(u => u.Uuid).ToHashSet();

        foreach (var current in currentUndersecretaries.Where(u => !requestedUuids.Contains(u.Uuid)))
        {
            RemoveAgencyRoleFromUser(current, id, "ROLE_UNDERSECRETARY");
        }

        foreach (var userUuid in requestedUuids.Where(uuid => !currentUuids.Contains(uuid)))
        {
            var user = await LoadUserForLeadershipAssignmentAsync(userUuid, cancellationToken);
            await EnsureUserHasRoleAsync(user, "ROLE_UNDERSECRETARY", cancellationToken);
            EnsureAgencyMembership(user, id);
        }

        AuditLogHelper.Write(
            _db,
            admin.Id,
            "agency_undersecretaries_assigned",
            "agency",
            agency.Code,
            new { Count = requestedUuids.Count });
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    private async Task<List<User>> LoadAgencyLeadershipUsersAsync(long agencyId, string roleCode, CancellationToken cancellationToken) =>
        await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.AgencyMemberships)
            .Where(u =>
                u.AgencyMemberships.Any(m => m.AgencyId == agencyId) &&
                u.UserRoles.Any(ur => ur.Role.Code == roleCode))
            .ToListAsync(cancellationToken);

    private async Task<User> LoadUserForLeadershipAssignmentAsync(Guid userUuid, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.AgencyMemberships)
            .FirstOrDefaultAsync(u => u.Uuid == userUuid, cancellationToken);

        if (user is null)
        {
            throw new ClientPortalException("USER_NOT_FOUND", "User not found.");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new ClientPortalException("USER_INACTIVE", "Only active users can be assigned to agency leadership.");
        }

        return user;
    }

    private async Task EnsureUserHasRoleAsync(User user, string roleCode, CancellationToken cancellationToken)
    {
        if (user.UserRoles.Any(ur => string.Equals(ur.Role.Code, roleCode, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var role = await _db.Roles.FirstAsync(r => r.Code == roleCode, cancellationToken);
        user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
    }

    private static void EnsureAgencyMembership(User user, long agencyId)
    {
        if (user.AgencyMemberships.Any(m => m.AgencyId == agencyId))
        {
            return;
        }

        user.AgencyMemberships.Add(new AgencyMembership
        {
            AgencyId = agencyId,
            IsPrimary = user.AgencyMemberships.Count == 0,
            JoinedAt = DateTime.UtcNow,
        });
    }

    private static void RemoveAgencyRoleFromUser(User user, long agencyId, string roleCode)
    {
        var userRole = user.UserRoles.FirstOrDefault(ur =>
            string.Equals(ur.Role.Code, roleCode, StringComparison.OrdinalIgnoreCase));
        if (userRole is not null)
        {
            user.UserRoles.Remove(userRole);
        }

        if (!user.UserRoles.Any(ur => RoleDefinitions.AgencyRoleCodes.Contains(ur.Role.Code)))
        {
            user.AgencyMemberships.Clear();
        }
    }

    private static AdminAgencyDto MapListItem(Agency agency, string? parentName) =>
        new(agency.Id, agency.Code, agency.Name, agency.Description, agency.ParentId, parentName, agency.IsActive, agency.Memberships.Count);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class AdminCommodityService : IAdminCommodityService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminCommodityService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AdminCommodityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        return await _db.Commodities.Include(c => c.Category).OrderBy(c => c.Name)
            .Select(c => new AdminCommodityDto(c.Id, c.Code, c.Name, c.Category.Name, c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminCommodityDto> CreateAsync(CreateCommodityRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var category = await _db.CommodityCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Category not found.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Commodities.AnyAsync(c => c.Code == code, cancellationToken))
        {
            throw new ClientPortalException("CODE_EXISTS", "Commodity code already exists.");
        }

        var commodity = new Commodity { CategoryId = category.Id, Code = code, Name = request.Name.Trim(), IsActive = true };
        _db.Commodities.Add(commodity);
        AuditLogHelper.Write(_db, admin.Id, "commodity_created", "commodity", code);
        await _db.SaveChangesAsync(cancellationToken);
        return new AdminCommodityDto(commodity.Id, commodity.Code, commodity.Name, category.Name, commodity.IsActive);
    }

    public async Task<AdminCommodityDto> UpdateAsync(long id, UpdateCommodityRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var commodity = await _db.Commodities.Include(c => c.Category).FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Commodity not found.");
        commodity.Name = request.Name.Trim();
        commodity.IsActive = request.IsActive;
        AuditLogHelper.Write(_db, admin.Id, "commodity_updated", "commodity", commodity.Code);
        await _db.SaveChangesAsync(cancellationToken);
        return new AdminCommodityDto(commodity.Id, commodity.Code, commodity.Name, commodity.Category.Name, commodity.IsActive);
    }
}

public class AdminPaymentConfigService : IAdminPaymentConfigService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminPaymentConfigService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProcessingFeeConfigDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        return await _db.ProcessingFeeConfigs.Include(p => p.Agency).OrderBy(p => p.Agency.Code)
            .Select(p => new ProcessingFeeConfigDto(p.Id, p.AgencyId, p.Agency.Code, p.EntryType.ToString(), p.Amount, p.Currency, p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProcessingFeeConfigDto> UpsertAsync(UpsertProcessingFeeRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<EntryType>(request.EntryType, true, out var entryType))
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "Entry type must be Import or Export.");
        }

        var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == request.AgencyId, cancellationToken)
            ?? throw new ClientPortalException("AGENCY_NOT_FOUND", "Agency not found.");

        var config = await _db.ProcessingFeeConfigs
            .FirstOrDefaultAsync(p => p.AgencyId == agency.Id && p.EntryType == entryType, cancellationToken);

        if (config is null)
        {
            config = new ProcessingFeeConfig { AgencyId = agency.Id, EntryType = entryType };
            _db.ProcessingFeeConfigs.Add(config);
        }

        config.Amount = request.Amount;
        config.Currency = request.Currency.Trim().ToUpperInvariant();
        config.IsActive = request.IsActive;
        AuditLogHelper.Write(_db, admin.Id, "payment_config_upserted", "processing_fee_config", $"{agency.Code}:{entryType}");
        await _db.SaveChangesAsync(cancellationToken);
        return new ProcessingFeeConfigDto(config.Id, config.AgencyId, agency.Code, config.EntryType.ToString(), config.Amount, config.Currency, config.IsActive);
    }

    public async Task<AdminPaymentSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var settings = await PaymentSettingsReader.GetAllAsync(_db, cancellationToken);
        return MapSettings(settings);
    }

    public async Task<AdminPaymentSettingsDto> UpdateSettingsAsync(UpdateAdminPaymentSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);

        await UpsertSettingAsync(PaymentSettingsDefaults.PayMongoEnabled, request.PayMongoEnabled ? "1" : "0", cancellationToken);
        await UpsertSettingAsync(PaymentSettingsDefaults.PayMongoPublicKey, request.PayMongoPublicKey?.Trim() ?? string.Empty, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.PayMongoApiKey) && request.PayMongoApiKey != "********")
        {
            await UpsertSettingAsync(PaymentSettingsDefaults.PayMongoApiKey, request.PayMongoApiKey.Trim(), cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.PayMongoWebhookSecret) && request.PayMongoWebhookSecret != "********")
        {
            await UpsertSettingAsync(PaymentSettingsDefaults.PayMongoWebhookSecret, request.PayMongoWebhookSecret.Trim(), cancellationToken);
        }

        await UpsertSettingAsync(PaymentSettingsDefaults.EntryProcessingFeeImport, request.ImportFeeAmount.ToString("0.##"), cancellationToken);
        await UpsertSettingAsync(PaymentSettingsDefaults.EntryProcessingFeeExport, request.ExportFeeAmount.ToString("0.##"), cancellationToken);
        await UpsertSettingAsync(PaymentSettingsDefaults.EntryProcessingFeeCurrency, request.Currency.Trim().ToUpperInvariant(), cancellationToken);

        AuditLogHelper.Write(_db, admin.Id, "payment_settings_updated", "payment_settings", null);
        await _db.SaveChangesAsync(cancellationToken);

        var merged = await PaymentSettingsReader.GetAllAsync(_db, cancellationToken);
        return MapSettings(merged);
    }

    private async Task UpsertSettingAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);
        if (setting is null)
        {
            _db.SystemSettings.Add(new SystemSetting { SettingKey = key, SettingValue = value });
            return;
        }

        setting.SettingValue = value;
    }

    private static AdminPaymentSettingsDto MapSettings(IReadOnlyDictionary<string, string> settings)
    {
        var apiKey = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoApiKey);
        var webhookSecret = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoWebhookSecret);
        var enabled = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoEnabled) == "1";
        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
        var mode = enabled && hasApiKey ? "paymongo" : "simulated";
        var currency = settings.GetValueOrDefault(PaymentSettingsDefaults.EntryProcessingFeeCurrency) ?? "PHP";

        decimal importFee = decimal.TryParse(settings.GetValueOrDefault(PaymentSettingsDefaults.EntryProcessingFeeImport), out var importParsed) ? importParsed : 2500m;
        decimal exportFee = decimal.TryParse(settings.GetValueOrDefault(PaymentSettingsDefaults.EntryProcessingFeeExport), out var exportParsed) ? exportParsed : 2500m;

        return new AdminPaymentSettingsDto(
            new PaymentGatewaySettingsDto(
                enabled,
                mode,
                hasApiKey,
                MaskSecret(apiKey),
                !string.IsNullOrWhiteSpace(webhookSecret),
                string.IsNullOrWhiteSpace(settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoPublicKey))
                    ? null
                    : settings[PaymentSettingsDefaults.PayMongoPublicKey]),
            new List<GlobalEntryProcessingFeeDto>
            {
                new("Import", importFee, currency),
                new("Export", exportFee, currency),
            });
    }

    private static string MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length <= 8) return "********";
        return $"{value[..4]}...{value[^4..]}";
    }
}

public class AdminEntryPaymentService : IAdminEntryPaymentService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminEntryPaymentService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AdminEntryPaymentListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ClientBillPayments.AsNoTracking()
            .Where(p => p.ClientBill.EntryId != null);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminEntryPaymentListItemDto(
                p.Id,
                p.ClientBill.Uuid,
                p.ClientBill.BillNumber,
                p.ClientBill.Entry!.ReferenceNo,
                p.ClientBill.Entry.Agency.Code,
                p.ClientBill.User.Profile != null
                    ? (p.ClientBill.User.Profile.FirstName + " " + p.ClientBill.User.Profile.LastName).Trim()
                    : p.ClientBill.User.Email,
                p.Amount,
                p.PaymentMethod,
                p.Status,
                p.ExternalReference,
                p.GatewayTransactionId,
                p.CreatedAt,
                p.ClientBill.PaidAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminEntryPaymentListItemDto>(items, page, pageSize, total);
    }

    public async Task<AdminRevenueSummaryDto> GetRevenueSummaryAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);

        var entryPayments = _db.ClientBillPayments.AsNoTracking()
            .Where(p => p.ClientBill.EntryId != null);

        var paidPayments = entryPayments.Where(p => p.Status == "completed");
        var totalCollected = await paidPayments.SumAsync(p => p.Amount, cancellationToken);
        var paidCount = await paidPayments.CountAsync(cancellationToken);
        var pendingCount = await entryPayments.CountAsync(p => p.Status == "pending", cancellationToken);
        var failedCount = await entryPayments.CountAsync(p => p.Status == "failed", cancellationToken);

        var pendingAmount = await _db.ClientBills.AsNoTracking()
            .Where(b => b.EntryId != null && b.Status != ClientBillStatus.Paid && b.Status != ClientBillStatus.Cancelled)
            .SumAsync(b => b.Amount, cancellationToken);

        var paidRows = await paidPayments
            .Select(p => new
            {
                p.Amount,
                p.CreatedAt,
                AgencyCode = p.ClientBill.Entry!.Agency.Code,
                AgencyName = p.ClientBill.Entry.Agency.Name,
            })
            .ToListAsync(cancellationToken);

        var byAgency = paidRows
            .GroupBy(row => new { row.AgencyCode, row.AgencyName })
            .Select(g => new AdminRevenueAgencyBreakdownDto(
                g.Key.AgencyCode,
                g.Key.AgencyName,
                g.Sum(x => x.Amount),
                g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var byMonth = paidRows
            .GroupBy(row => new { row.CreatedAt.Year, row.CreatedAt.Month })
            .Select(g => new AdminRevenueMonthlyBreakdownDto(
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                g.Sum(x => x.Amount),
                g.Count()))
            .OrderByDescending(x => x.Month)
            .Take(12)
            .ToList();

        return new AdminRevenueSummaryDto(
            totalCollected,
            pendingAmount,
            paidCount,
            pendingCount,
            failedCount,
            byAgency,
            byMonth);
    }
}

public class AdminAuditService : IAdminAuditService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminAuditService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AuditLogListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var query = _db.AuditLogs.Include(a => a.ActorUser).ThenInclude(u => u!.Profile);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogListItemDto(
                a.Id, a.Action, a.EntityType, a.EntityId,
                a.ActorUser != null && a.ActorUser.Profile != null ? $"{a.ActorUser.Profile.FirstName} {a.ActorUser.Profile.LastName}" : a.ActorUser != null ? a.ActorUser.Email : null,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<AuditLogListItemDto>(items, page, pageSize, total);
    }
}

public class AdminRoleService : IAdminRoleService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminRoleService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AdminRoleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var roles = await _db.Roles.ToListAsync(cancellationToken);
        return roles
            .OrderBy(r => RoleDefinitions.SortKey(r.Code))
            .Select(r => new AdminRoleDto(
                r.Code,
                r.Name,
                r.Description,
                RoleDefinitions.AgencyRoleCodes.Contains(r.Code)))
            .ToList();
    }
}

public class AdminSettingsService : IAdminSettingsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public AdminSettingsService(AgriCheckDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<AdminSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var stored = await _db.SystemSettings.ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var merged = new Dictionary<string, string>(AdminSettingsDefaults.Values, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in stored)
        {
            merged[key] = value;
        }

        return new AdminSettingsDto(merged);
    }

    public async Task<AdminSettingsDto> UpdateAsync(UpdateAdminSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var allowedKeys = new HashSet<string>(AdminSettingsDefaults.Values.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in request.Settings)
        {
            if (!allowedKeys.Contains(key)) continue;

            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key, cancellationToken);
            if (setting is null)
            {
                _db.SystemSettings.Add(new SystemSetting { SettingKey = key, SettingValue = value });
            }
            else
            {
                setting.SettingValue = value;
            }
        }

        AuditLogHelper.Write(_db, admin.Id, "settings_updated", "system_settings", null, new { keys = request.Settings.Keys.ToList() });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    public async Task<AdminSettingsDto> UploadBrandingAssetAsync(
        string assetType,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var settingKey = assetType.ToLowerInvariant() switch
        {
            "system-logo" => "system_logo_path",
            "spinner-logo" => "spinner_logo_path",
            "favicon" => "favicon_path",
            _ => throw new ClientPortalException("INVALID_ASSET", "Unsupported branding asset type.")
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico"
        };
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            throw new ClientPortalException("INVALID_FILE", "Logo uploads must be PNG, JPG, GIF, SVG, WEBP, or ICO files.");
        }

        var prefix = assetType.ToLowerInvariant() switch
        {
            "system-logo" => "system-logo",
            "spinner-logo" => "spinner-logo",
            "favicon" => "favicon",
            _ => "branding"
        };

        var uploadName = $"{prefix}{extension.ToLowerInvariant()}";
        var (storedFileName, _) = await _fileStorage.SaveAsync(fileStream, "system", uploadName, cancellationToken);
        var publicPath = $"/uploads/system/{storedFileName}";
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);
        if (setting is null)
        {
            _db.SystemSettings.Add(new SystemSetting { SettingKey = settingKey, SettingValue = publicPath });
        }
        else
        {
            setting.SettingValue = publicPath;
        }

        AuditLogHelper.Write(_db, admin.Id, "settings_branding_uploaded", "system_settings", null, new { assetType, path = publicPath });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }
}
