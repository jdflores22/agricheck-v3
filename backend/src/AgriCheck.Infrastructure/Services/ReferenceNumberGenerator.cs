using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public static class ReferenceNumberGenerator
{
    public static async Task<string> NextAsync(AgriCheckDbContext db, string prefix, Func<string, Task<bool>> existsCheck, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            if (!await existsCheck(candidate))
            {
                return candidate;
            }
        }

        return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N[..6].ToUpperInvariant()}";
    }

    public static Task<string> EntryAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "ENT", n => db.Entries.AnyAsync(e => e.ReferenceNo == n, ct), ct);

    public static Task<string> BillAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "BILL", n => db.ClientBills.AnyAsync(b => b.BillNumber == n, ct), ct);

    public static Task<string> BookingAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "WB", n => db.WarehouseBookings.AnyAsync(b => b.BookingNumber == n, ct), ct);

    public static Task<string> CertificateAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "CERT", n => db.Certificates.AnyAsync(c => c.CertificateNumber == n, ct), ct);

    public static Task<string> AccreditationCertificateAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "CERT-ACC", n => db.Certificates.AnyAsync(c => c.CertificateNumber == n, ct), ct);

    public static Task<string> AgencyBillAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "ABILL", n => db.AgencyBillings.AnyAsync(b => b.BillNumber == n, ct), ct);

    public static Task<string> MavApplicationAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "MAV-APP", n => db.MavApplications.AnyAsync(a => a.ReferenceNumber == n, ct), ct);

    public static Task<string> MavLicenseAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "MAV-LIC", n => db.MavLicenses.AnyAsync(l => l.LicenseNumber == n, ct), ct);

    public static Task<string> MavMicAsync(AgriCheckDbContext db, CancellationToken ct) =>
        NextAsync(db, "MIC", n => db.MavImportCertificates.AnyAsync(c => c.CertificateNumber == n, ct), ct);

    public static async Task<string> ContainerAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CONT-{year}-";

        var lastNumber = await db.Containers
            .Where(c => c.ContainerNumber.StartsWith(prefix))
            .OrderByDescending(c => c.ContainerNumber)
            .Select(c => c.ContainerNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber) && lastNumber.Length >= prefix.Length + 4)
        {
            var suffix = lastNumber[^4..];
            if (int.TryParse(suffix, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var candidate = $"{prefix}{sequence + attempt:D4}";
            if (!await db.Containers.AnyAsync(c => c.ContainerNumber == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"{prefix}{Guid.NewGuid():N[..4].ToUpperInvariant()}";
    }
}
