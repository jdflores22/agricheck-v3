namespace AgriCheck.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string RoleCode = "ROLE_IMPORTER");
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record VerifyEmailRequest(string Token);

public record AuthTokensDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, DateTime RefreshTokenExpiresAt);

public record UserSummaryDto(
    Guid Uuid,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    bool EmailVerified,
    bool MustChangePassword,
    IReadOnlyList<string> Roles);

public record AuthResponseDto(AuthTokensDto Tokens, UserSummaryDto User, string RedirectPath);

public record MeResponseDto(UserSummaryDto User, string RedirectPath);
