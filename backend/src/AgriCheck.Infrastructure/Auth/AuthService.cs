using System.Security.Cryptography;
using System.Text;
using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.Auth;
using AgriCheck.Application.Auth.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgriCheck.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AgriCheckDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordService _passwordService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> PublicRegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ROLE_IMPORTER", "ROLE_EXPORTER", "ROLE_BROKER"
    };

    public AuthService(
        AgriCheckDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IPasswordService passwordService,
        ILoginAttemptService loginAttemptService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _passwordService = passwordService;
        _loginAttemptService = loginAttemptService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (_loginAttemptService.IsLockedOut(email))
        {
            var minutes = _loginAttemptService.GetRemainingLockoutMinutes(email);
            throw new AuthException("ACCOUNT_LOCKED", $"Account is temporarily locked. Try again in {minutes} minute(s).");
        }

        var user = await _dbContext.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !_passwordService.Verify(request.Password, user.PasswordHash))
        {
            _loginAttemptService.RecordFailedAttempt(email);
            throw new AuthException("INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (user.Status == UserStatus.Suspended)
        {
            throw new AuthException("ACCOUNT_SUSPENDED", "Your account has been suspended.");
        }

        if (user.Status == UserStatus.Deactivated)
        {
            throw new AuthException("ACCOUNT_DEACTIVATED", "Your account has been deactivated.");
        }

        _loginAttemptService.ResetAttempts(email);
        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await IssueAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var roleCode = request.RoleCode.Trim().ToUpperInvariant();

        if (!PublicRegistrationRoles.Contains(roleCode))
        {
            throw new AuthException("INVALID_ROLE", "Invalid registration role.");
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new AuthException("EMAIL_EXISTS", "An account with this email already exists.");
        }

        if (!_passwordService.ValidateStrength(request.Password, out var passwordError))
        {
            throw new AuthException("WEAK_PASSWORD", passwordError!);
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Code == roleCode, cancellationToken)
            ?? throw new AuthException("ROLE_NOT_FOUND", "Registration role is not configured.");

        var user = new User
        {
            Uuid = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordService.Hash(request.Password),
            Status = UserStatus.Pending,
            Profile = new UserProfile
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim()
            }
        };

        user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await CreateEmailVerificationTokenAsync(user, cancellationToken);
        await WriteAuditAsync(user.Id, "USER_REGISTERED", "User", user.Uuid.ToString(), ipAddress, cancellationToken);

        _logger.LogInformation("New user registered: {Email}", email);

        return await IssueAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _dbContext.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.Profile)
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AuthException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        var response = await IssueAuthResponseAsync(stored.User, ipAddress, cancellationToken, stored.TokenHash);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            return;
        }

        var rawToken = GenerateRawToken();
        _dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SendPasswordResetEmailAsync(email, rawToken, cancellationToken);
        _logger.LogInformation("Password reset token for {Email}: {Token}", email, rawToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!_passwordService.ValidateStrength(request.NewPassword, out var passwordError))
        {
            throw new AuthException("WEAK_PASSWORD", passwordError!);
        }

        var tokenHash = HashToken(request.Token);
        var resetToken = await _dbContext.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, cancellationToken)
            ?? throw new AuthException("INVALID_TOKEN", "Password reset token is invalid or expired.");

        resetToken.UsedAt = DateTime.UtcNow;
        resetToken.User.PasswordHash = _passwordService.Hash(request.NewPassword);
        resetToken.User.MustChangePassword = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.Token);
        var verification = await _dbContext.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.VerifiedAt == null && t.ExpiresAt > DateTime.UtcNow, cancellationToken)
            ?? throw new AuthException("INVALID_TOKEN", "Verification token is invalid or expired.");

        verification.VerifiedAt = DateTime.UtcNow;
        verification.User.EmailVerifiedAt = DateTime.UtcNow;
        verification.User.Status = UserStatus.Active;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        if (user is null || user.EmailVerifiedAt is not null)
        {
            return;
        }

        await CreateEmailVerificationTokenAsync(user, cancellationToken);
    }

    public async Task<MeResponseDto?> GetCurrentUserAsync(Guid userUuid, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Uuid == userUuid, cancellationToken);

        if (user is null) return null;

        var assignedRoles = user.UserRoles.Select(ur => ur.Role.Code).OrderBy(RoleDefinitions.SortKey).ToList();
        var effectiveRoles = RoleHierarchy.ExpandRoles(assignedRoles);
        var summary = MapUser(user, assignedRoles);
        return new MeResponseDto(summary, RoleHierarchy.ResolveRedirectPath(effectiveRoles));
    }

    public async Task ChangePasswordAsync(Guid userUuid, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Uuid == userUuid, cancellationToken)
            ?? throw new AuthException("USER_NOT_FOUND", "User not found.");

        if (!_passwordService.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthException("INVALID_PASSWORD", "Current password is incorrect.");
        }

        if (!_passwordService.ValidateStrength(request.NewPassword, out var passwordError))
        {
            throw new AuthException("WEAK_PASSWORD", passwordError!);
        }

        user.PasswordHash = _passwordService.Hash(request.NewPassword);
        user.MustChangePassword = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponseDto> IssueAuthResponseAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken,
        string? replacedTokenHash = null)
    {
        var assignedRoles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var effectiveRoles = RoleHierarchy.ExpandRoles(assignedRoles);
        var (accessToken, accessExpires) = _jwtTokenService.CreateAccessToken(user, effectiveRoles);
        var refreshRaw = GenerateRawToken();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshRaw),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedByIp = ipAddress,
            ReplacedByTokenHash = replacedTokenHash
        };

        _dbContext.RefreshTokens.Add(refreshEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tokens = new AuthTokensDto(accessToken, refreshRaw, accessExpires, refreshEntity.ExpiresAt);
        var summary = MapUser(user, assignedRoles);
        var redirect = RoleHierarchy.ResolveRedirectPath(effectiveRoles);
        return new AuthResponseDto(tokens, summary, redirect);
    }

    private async Task CreateEmailVerificationTokenAsync(User user, CancellationToken cancellationToken)
    {
        var rawToken = GenerateRawToken();
        _dbContext.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendVerificationEmailAsync(user.Email, rawToken, cancellationToken);
        _logger.LogInformation("Email verification token for {Email}: {Token}", user.Email, rawToken);
    }

    private async Task SendVerificationEmailAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5173";
        var link = $"{baseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(rawToken)}";
        var html = $"""
            <p>Welcome to AgriCheck. Please verify your email address:</p>
            <p><a href="{link}">Verify email</a></p>
            <p>If the button does not work, copy this link: {link}</p>
            """;
        try
        {
            await _emailService.SendAsync(email, "Verify your AgriCheck account", html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send verification email to {Email}", email);
        }
    }

    private async Task SendPasswordResetEmailAsync(string email, string rawToken, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5173";
        var link = $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var html = $"""
            <p>You requested a password reset for your AgriCheck account.</p>
            <p><a href="{link}">Reset password</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;
        try
        {
            await _emailService.SendAsync(email, "Reset your AgriCheck password", html, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", email);
        }
    }

    private async Task WriteAuditAsync(long? actorId, string action, string entityType, string? entityId, string? ip, CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ip
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static UserSummaryDto MapUser(User user, IReadOnlyList<string> roles) =>
        new(
            user.Uuid,
            user.Email,
            user.Profile?.FirstName ?? string.Empty,
            user.Profile?.LastName ?? string.Empty,
            user.Status.ToString().ToLowerInvariant(),
            user.EmailVerifiedAt is not null,
            user.MustChangePassword,
            roles);

    private static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

public class AuthException : Exception
{
    public string Code { get; }

    public AuthException(string code, string message) : base(message)
    {
        Code = code;
    }
}
