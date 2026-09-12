using Microsoft.Extensions.Caching.Memory;

namespace AgriCheck.Infrastructure.Auth;

public interface ILoginAttemptService
{
    void RecordFailedAttempt(string email);
    void ResetAttempts(string email);
    bool IsLockedOut(string email);
    int GetRemainingLockoutMinutes(string email);
}

public class LoginAttemptService : ILoginAttemptService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private readonly IMemoryCache _cache;

    public LoginAttemptService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void RecordFailedAttempt(string email)
    {
        var key = Normalize(email);
        var entry = _cache.GetOrCreate(key, _ => new AttemptEntry())!;
        entry.FailedCount++;
        if (entry.FailedCount >= MaxAttempts)
        {
            entry.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
        }

        _cache.Set(key, entry, TimeSpan.FromHours(1));
    }

    public void ResetAttempts(string email) => _cache.Remove(Normalize(email));

    public bool IsLockedOut(string email)
    {
        if (!_cache.TryGetValue(Normalize(email), out AttemptEntry? entry) || entry is null)
        {
            return false;
        }

        return entry.LockedUntil is not null && entry.LockedUntil > DateTime.UtcNow;
    }

    public int GetRemainingLockoutMinutes(string email)
    {
        if (!_cache.TryGetValue(Normalize(email), out AttemptEntry? entry) || entry?.LockedUntil is null)
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling((entry.LockedUntil.Value - DateTime.UtcNow).TotalMinutes));
    }

    private static string Normalize(string email) => $"login:{email.Trim().ToLowerInvariant()}";

    private sealed class AttemptEntry
    {
        public int FailedCount { get; set; }
        public DateTime? LockedUntil { get; set; }
    }
}
