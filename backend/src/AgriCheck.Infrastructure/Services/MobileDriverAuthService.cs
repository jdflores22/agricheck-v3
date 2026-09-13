using AgriCheck.Application.Auth;
using AgriCheck.Application.Auth.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Auth;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class MobileDriverAuthService : IMobileDriverAuthService
{
    private readonly AgriCheckDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly IAuthService _authService;

    public MobileDriverAuthService(AgriCheckDbContext db, IPasswordService passwordService, IAuthService authService)
    {
        _db = db;
        _passwordService = passwordService;
        _authService = authService;
    }

    public async Task<InviteCodeValidationDto> ValidateInviteCodeAsync(
        ValidateInviteCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        await OperatorInviteCodeSeeder.EnsureSeedDataAsync(_db, cancellationToken: cancellationToken);
        var invite = await ResolveInviteCodeAsync(request.InviteCode, cancellationToken);
        if (invite is null)
        {
            return new InviteCodeValidationDto(false, null, null);
        }

        var operatorName = await _db.Users
            .Include(u => u.Profile)
            .Where(u => u.Id == invite.OperatorUserId)
            .Select(u => u.Profile == null ? u.Email : $"{u.Profile.FirstName} {u.Profile.LastName}".Trim())
            .FirstOrDefaultAsync(cancellationToken);

        return new InviteCodeValidationDto(true, operatorName, invite.Label);
    }

    public async Task<AuthResponseDto> RegisterDriverAsync(
        RegisterDriverRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ClientPortalException("EMAIL_EXISTS", "An account with this email already exists.");
        }

        if (!_passwordService.ValidateStrength(request.Password, out var passwordError))
        {
            throw new ClientPortalException("WEAK_PASSWORD", passwordError!);
        }

        await OperatorInviteCodeSeeder.EnsureSeedDataAsync(_db, cancellationToken: cancellationToken);
        var invite = await ResolveInviteCodeAsync(request.InviteCode, cancellationToken)
            ?? throw new ClientPortalException("INVALID_INVITE", "Invite code is invalid or expired.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Code == "ROLE_DRIVER", cancellationToken)
            ?? throw new ClientPortalException("ROLE_NOT_FOUND", "Driver role is not configured.");

        var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : request.FullName.Trim();
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var user = new User
        {
            Uuid = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordService.Hash(request.Password),
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            Profile = new UserProfile
            {
                FirstName = firstName,
                LastName = lastName,
                Phone = request.PhoneNumber.Trim(),
            },
        };
        user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });

        var profile = new DriverProfile
        {
            UserId = user.Id,
            OperatorUserId = invite.OperatorUserId,
            BirthDate = request.BirthDate.Date,
            RegionId = request.RegionId,
            ProvinceId = request.ProvinceId,
            CityId = request.CityId,
            BarangayId = request.BarangayId,
            ZipCode = request.ZipCode.Trim(),
            StreetAddress = request.StreetAddress.Trim(),
            LicenseNumber = request.LicenseNumber.Trim(),
            LicenseExpiryDate = request.LicenseExpiryDate.Date,
            PhoneNumber = request.PhoneNumber.Trim(),
            SubmittedAt = DateTime.UtcNow,
            ApprovedAt = DateTime.UtcNow,
        };
        profile.CompletionPercentage = CalculateInitialCompletion(profile);
        profile.Address = BuildAddressLine(profile);

        _db.Users.Add(user);
        _db.DriverProfiles.Add(profile);
        invite.UsedCount += 1;
        await _db.SaveChangesAsync(cancellationToken);

        return await _authService.LoginAsync(new LoginRequest(email, request.Password), ipAddress, cancellationToken);
    }

    private async Task<OperatorInviteCode?> ResolveInviteCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var invite = await _db.OperatorInviteCodes
            .FirstOrDefaultAsync(i => i.Code == normalized && i.IsActive, cancellationToken);
        if (invite is null)
        {
            return null;
        }

        if (invite.ExpiresAt is not null && invite.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        if (invite.MaxUses > 0 && invite.UsedCount >= invite.MaxUses)
        {
            return null;
        }

        return invite;
    }

    private static int CalculateInitialCompletion(DriverProfile profile)
    {
        var fields = new object?[]
        {
            profile.LicenseNumber,
            profile.LicenseExpiryDate,
            profile.PhoneNumber,
            profile.StreetAddress,
            profile.RegionId,
            profile.BirthDate,
        };
        var filled = fields.Count(f => f is not null && (f is not string s || !string.IsNullOrWhiteSpace(s)));
        return (int)Math.Round(filled * 100.0 / fields.Length);
    }

    private static string BuildAddressLine(DriverProfile profile) =>
        string.Join(", ", new[] { profile.StreetAddress, profile.ZipCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
