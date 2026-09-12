using System.Text.Json;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public interface IEntryCertificateAutoIssueService
{
    Task<bool> TryIssueForEntryAsync(Entry entry, long issuedByUserId, CancellationToken cancellationToken = default);
}

public class EntryCertificateAutoIssueService : IEntryCertificateAutoIssueService
{
    private readonly AgriCheckDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notifications;
    private readonly ILogger<EntryCertificateAutoIssueService> _logger;

    public EntryCertificateAutoIssueService(
        AgriCheckDbContext db,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        INotificationService notifications,
        ILogger<EntryCertificateAutoIssueService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<bool> TryIssueForEntryAsync(Entry entry, long issuedByUserId, CancellationToken cancellationToken = default)
    {
        if (await _db.Certificates.AnyAsync(
                c => c.EntryId == entry.Id && c.Status == CertificateStatus.Active,
                cancellationToken))
        {
            return false;
        }

        var template = await _db.CertificateProcessAssignments
            .Include(a => a.Template).ThenInclude(t => t!.Versions)
            .Where(a => a.AgencyId == entry.AgencyId &&
                        a.ProcessType == CertificateProcessType.ImportEntry &&
                        a.IsActive)
            .Select(a => a.Template)
            .FirstOrDefaultAsync(cancellationToken);

        var version = template?.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault(v => v.IsPublished)
            ?? template?.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        var verificationCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        var publicBase = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5173";
        var verifyUrl = $"{publicBase.TrimEnd('/')}/verify?code={verificationCode}";
        var qrData = QrCodeGenerator.ToBase64Png(verifyUrl);
        var holderName = entry.User?.Profile is not null
            ? $"{entry.User.Profile.FirstName} {entry.User.Profile.LastName}"
            : entry.User?.Email ?? "Client";
        var title = $"Import Certificate - {entry.ReferenceNo}";

        var certificate = new Certificate
        {
            Uuid = Guid.NewGuid(),
            UserId = entry.UserId,
            EntryId = entry.Id,
            AgencyId = entry.AgencyId,
            TemplateVersionId = version?.Id,
            IssuedByUserId = issuedByUserId,
            CertificateNumber = await ReferenceNumberGenerator.CertificateAsync(_db, cancellationToken),
            VerificationCode = verificationCode,
            Title = title,
            Status = CertificateStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            QrCodeData = qrData,
            SummaryJson = JsonSerializer.Serialize(new
            {
                entry.ReferenceNo,
                entry.Agency?.Code,
                entry.Detail?.CommodityName,
                holderName
            })
        };

        var pdfBytes = CertificatePdfGenerator.Generate(
            title, certificate.CertificateNumber, holderName, verificationCode, verifyUrl, qrData);
        await using var pdfStream = new MemoryStream(pdfBytes);
        var (storedFileName, _) = await _fileStorage.SaveAsync(
            pdfStream, $"certificates/{certificate.Uuid}", $"{certificate.CertificateNumber}.pdf", cancellationToken);
        certificate.PdfStoredFileName = storedFileName;

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "certificate_issued",
            Title = "Certificate issued",
            Description = $"Certificate {certificate.CertificateNumber} issued automatically.",
            ActorUserId = issuedByUserId
        });

        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            entry.UserId,
            "certificate_issued",
            "Certificate issued",
            $"Certificate {certificate.CertificateNumber} has been issued for entry {entry.ReferenceNo}.",
            "Certificate",
            certificate.Uuid.ToString(),
            cancellationToken);

        _logger.LogInformation("Auto-issued certificate {Number} for entry {Reference}", certificate.CertificateNumber, entry.ReferenceNo);
        return true;
    }
}
