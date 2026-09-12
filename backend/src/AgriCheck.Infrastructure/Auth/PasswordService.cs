namespace AgriCheck.Infrastructure.Auth;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
    bool ValidateStrength(string password, out string? error);
}

public class PasswordService : IPasswordService
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public bool ValidateStrength(string password, out string? error)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            error = "Password must be at least 8 characters.";
            return false;
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
        {
            error = "Password must include upper, lower, and numeric characters.";
            return false;
        }

        error = null;
        return true;
    }
}
