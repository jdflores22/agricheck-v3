using AgriCheck.Application.MavPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public class MavNotificationService : IMavNotificationService
{
    private static readonly string[] StaffRoleNames =
    {
        "ROLE_MAV_ADMIN",
        "ROLE_MAV_EVALUATOR",
        "ROLE_MAV_SECRETARY",
        "ROLE_ADMIN"
    };

    private readonly AgriCheckDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<MavNotificationService> _logger;

    public MavNotificationService(
        AgriCheckDbContext db,
        INotificationService notifications,
        ILogger<MavNotificationService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task NotifyApplicationSubmittedAsync(long importerId, Guid applicationUuid, string referenceNumber, string commodityName, CancellationToken cancellationToken = default)
    {
        var staffIds = await GetStaffUserIdsAsync(cancellationToken);
        foreach (var staffId in staffIds)
        {
            await NotifyIfEnabledAsync(
                staffId,
                p => p.ApplicationSubmitted,
                "mav_application_submitted",
                "New MAV application submitted",
                $"{referenceNumber} — {commodityName} is awaiting review.",
                "MavApplication",
                applicationUuid.ToString(),
                cancellationToken);
        }
    }

    public async Task NotifyApplicationApprovedAsync(long importerId, Guid applicationUuid, string referenceNumber, string licenseNumber, decimal allocatedVolume, CancellationToken cancellationToken = default)
    {
        await NotifyIfEnabledAsync(
            importerId,
            p => p.ApplicationApproved,
            "mav_application_approved",
            "MAV application approved",
            $"{referenceNumber} was approved. License {licenseNumber} issued for {allocatedVolume:0.###} MT.",
            "MavApplication",
            applicationUuid.ToString(),
            cancellationToken);

        await NotifyIfEnabledAsync(
            importerId,
            p => p.LicenseIssued,
            "mav_license_issued",
            "MAV license issued",
            $"License {licenseNumber} is now active for {allocatedVolume:0.###} MT.",
            "MavLicense",
            applicationUuid.ToString(),
            cancellationToken);
    }

    public async Task NotifyApplicationRejectedAsync(long importerId, Guid applicationUuid, string referenceNumber, string reason, CancellationToken cancellationToken = default)
    {
        await NotifyIfEnabledAsync(
            importerId,
            p => p.ApplicationRejected,
            "mav_application_rejected",
            "MAV application rejected",
            $"{referenceNumber} was rejected. Reason: {reason}",
            "MavApplication",
            applicationUuid.ToString(),
            cancellationToken);
    }

    public async Task NotifyMicIssuedAsync(long importerId, Guid micUuid, string certificateNumber, decimal volume, CancellationToken cancellationToken = default)
    {
        await NotifyIfEnabledAsync(
            importerId,
            p => p.MicIssued,
            "mav_mic_issued",
            "Import certificate issued",
            $"MIC {certificateNumber} issued for {volume:0.###} MT.",
            "MavImportCertificate",
            micUuid.ToString(),
            cancellationToken);
    }

    public async Task SendComplianceRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var licenseThreshold = now.AddDays(30);
        var micThreshold = now.AddDays(7);

        var expiringLicenses = await _db.MavLicenses
            .Where(l => l.Status == MavLicenseStatus.Active && l.ExpiresAt >= now && l.ExpiresAt <= licenseThreshold)
            .Select(l => new { l.Uuid, l.ImporterId, l.LicenseNumber, l.ExpiresAt, l.CommodityName })
            .ToListAsync(cancellationToken);

        foreach (var license in expiringLicenses)
        {
            var days = Math.Max(0, (int)Math.Ceiling((license.ExpiresAt - now).TotalDays));
            await NotifyIfEnabledAsync(
                license.ImporterId,
                p => p.LicenseExpiring,
                "mav_license_expiring",
                "MAV license expiring soon",
                $"License {license.LicenseNumber} ({license.CommodityName}) expires in {days} day(s).",
                "MavLicense",
                license.Uuid.ToString(),
                cancellationToken,
                dedupeDays: 7);
        }

        var expiringMics = await _db.MavImportCertificates
            .Where(m => m.Status != MavImportCertificateStatus.Expired && m.ExpiresAt >= now && m.ExpiresAt <= micThreshold)
            .Select(m => new { m.Uuid, m.ImporterId, m.CertificateNumber, m.ExpiresAt, m.CommodityName })
            .ToListAsync(cancellationToken);

        foreach (var mic in expiringMics)
        {
            var days = Math.Max(0, (int)Math.Ceiling((mic.ExpiresAt - now).TotalDays));
            await NotifyIfEnabledAsync(
                mic.ImporterId,
                p => p.MicExpiring,
                "mav_mic_expiring",
                "Import certificate expiring soon",
                $"MIC {mic.CertificateNumber} ({mic.CommodityName}) expires in {days} day(s).",
                "MavImportCertificate",
                mic.Uuid.ToString(),
                cancellationToken,
                dedupeDays: 3);
        }

        _logger.LogInformation("MAV compliance reminders processed: {LicenseCount} licenses, {MicCount} MICs", expiringLicenses.Count, expiringMics.Count);
    }

    private async Task NotifyIfEnabledAsync(
        long userId,
        Func<MavNotificationPreference, bool> prefSelector,
        string type,
        string title,
        string message,
        string entityType,
        string entityUuid,
        CancellationToken cancellationToken,
        int dedupeDays = 1)
    {
        var prefs = await GetOrCreateMavPreferencesAsync(userId, cancellationToken);
        if (!prefSelector(prefs))
        {
            return;
        }

        var dedupeSince = DateTime.UtcNow.AddDays(-dedupeDays);
        var alreadySent = await _db.Notifications.AnyAsync(
            n => n.UserId == userId
                 && n.Type == type
                 && n.RelatedEntityUuid == entityUuid
                 && n.CreatedAt >= dedupeSince,
            cancellationToken);
        if (alreadySent)
        {
            return;
        }

        await _notifications.NotifyAsync(userId, type, title, message, entityType, entityUuid, cancellationToken);
    }

    private async Task<MavNotificationPreference> GetOrCreateMavPreferencesAsync(long userId, CancellationToken cancellationToken)
    {
        var prefs = await _db.MavNotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (prefs is not null)
        {
            return prefs;
        }

        prefs = new MavNotificationPreference { UserId = userId };
        _db.MavNotificationPreferences.Add(prefs);
        await _db.SaveChangesAsync(cancellationToken);
        return prefs;
    }

    private async Task<IReadOnlyList<long>> GetStaffUserIdsAsync(CancellationToken cancellationToken) =>
        await _db.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => StaffRoleNames.Contains(ur.Role.Name))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
}
