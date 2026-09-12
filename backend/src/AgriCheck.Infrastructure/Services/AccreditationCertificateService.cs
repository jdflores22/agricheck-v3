using System.Text.Json;
using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public class AccreditationCertificateService : IAccreditationCertificateService
{
    private readonly AgriCheckDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly INotificationService _notifications;
    private readonly ILogger<AccreditationCertificateService> _logger;

    public AccreditationCertificateService(
        AgriCheckDbContext db,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        IHostEnvironment environment,
        INotificationService notifications,
        ILogger<AccreditationCertificateService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _environment = environment;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<AccreditationCertificateIssueResult> TryIssueForApprovedSubmissionAsync(
        AccreditationSubmission submission,
        long issuedByUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await AccreditationCertificateLookup.FindForSubmissionAsync(
            _db,
            submission.Uuid,
            submission.UserId,
            cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation("Accreditation certificate already exists for submission {Uuid}", submission.Uuid);
            return new AccreditationCertificateIssueResult(
                true,
                existing.Uuid,
                existing.CertificateNumber,
                "Certificate already issued for this application.");
        }

        return await IssueNewCertificateAsync(submission, issuedByUserId, cancellationToken);
    }

    public async Task<AccreditationCertificateIssueResult> RegenerateForApprovedSubmissionAsync(
        Guid submissionUuid,
        long issuedByUserId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _db.AccreditationSubmissions
            .FirstOrDefaultAsync(s => s.Uuid == submissionUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Accreditation submission not found.");

        if (submission.Status != AccreditationSubmissionStatus.Approved)
        {
            throw new ClientPortalException("NOT_APPROVED", "Only approved accreditation submissions can have certificates regenerated.");
        }

        var submissionToken = submission.Uuid.ToString();
        var existingCerts = await _db.Certificates
            .Where(c =>
                c.UserId == submission.UserId &&
                c.Status == CertificateStatus.Active &&
                c.SummaryJson != null &&
                c.SummaryJson.Contains(submissionToken))
            .ToListAsync(cancellationToken);

        foreach (var cert in existingCerts)
        {
            cert.Status = CertificateStatus.Revoked;
            cert.RevokedAt = DateTime.UtcNow;
            cert.RevokedReason = "Superseded by regenerated certificate";
        }

        if (existingCerts.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Revoked {Count} existing certificate(s) before regeneration for submission {Uuid}",
                existingCerts.Count,
                submission.Uuid);
        }

        return await IssueNewCertificateAsync(
            submission,
            issuedByUserId,
            cancellationToken,
            "Accreditation certificate regenerated successfully.");
    }

    private async Task<AccreditationCertificateIssueResult> IssueNewCertificateAsync(
        AccreditationSubmission submission,
        long issuedByUserId,
        CancellationToken cancellationToken,
        string successMessage = "Accreditation certificate issued successfully.")
    {
        var template = await ResolveTemplateAsync(cancellationToken);
        if (template is null)
        {
            _logger.LogWarning("No active accreditation certificate template found for submission {Uuid}", submission.Uuid);
            return new AccreditationCertificateIssueResult(
                false,
                null,
                null,
                "No active accreditation certificate template is configured.");
        }

        var version = template.Versions
            .Where(v => v.IsPublished && v.Elements.Count > 0)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault()
            ?? template.Versions
                .Where(v => v.Elements.Count > 0)
                .MaxBy(v => v.VersionNumber);

        if (version is null)
        {
            _logger.LogWarning("Accreditation template {TemplateId} has no usable version", template.Id);
            return new AccreditationCertificateIssueResult(
                false,
                null,
                null,
                "The accreditation certificate template has no published version.");
        }

        var validityDays = await SystemSettingsReader.GetIntAsync(_db, "accreditation_validity_days", 365, cancellationToken);
        var submissionUser = await _db.Users
            .Include(u => u.Profile)
            .FirstAsync(u => u.Id == submission.UserId, cancellationToken);

        var officer = await _db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == issuedByUserId, cancellationToken);

        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddDays(validityDays);
        var variables = BuildVariables(submission, submissionUser, officer, issuedAt, expiresAt);
        var verificationCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        var publicBase = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5173";
        var verifyUrl = $"{publicBase.TrimEnd('/')}/verify?code={verificationCode}";
        var qrData = QrCodeGenerator.ToBase64Png(verifyUrl);
        var certificateNumber = await ReferenceNumberGenerator.AccreditationCertificateAsync(_db, cancellationToken);
        variables["certificate"] = new Dictionary<string, object?>
        {
            ["number"] = certificateNumber,
            ["issued_at"] = issuedAt.ToString("yyyy-MM-dd"),
            ["expires_at"] = expiresAt.ToString("yyyy-MM-dd"),
        };

        var holderName = submission.CompanyName;
        if (string.IsNullOrWhiteSpace(holderName) && submissionUser.Profile is not null)
        {
            holderName = !string.IsNullOrWhiteSpace(submissionUser.Profile.CompanyName)
                ? submissionUser.Profile.CompanyName
                : $"{submissionUser.Profile.FirstName} {submissionUser.Profile.LastName}".Trim();
        }

        holderName ??= submissionUser.Email;
        var title = $"Accreditation Certificate - {holderName}";
        var storageRoot = Path.Combine(_environment.ContentRootPath, "storage");
        var elements = version.Elements.OrderBy(e => e.SortOrder).ToList();

        byte[] pdfBytes;
        try
        {
            pdfBytes = elements.Count > 0
                ? CertificateTemplatePdfGenerator.Generate(version, elements, variables, verifyUrl, qrData, storageRoot)
                : CertificatePdfGenerator.Generate(title, certificateNumber, holderName, verificationCode, verifyUrl, qrData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template PDF generation failed for submission {Uuid}; using fallback layout", submission.Uuid);
            pdfBytes = CertificatePdfGenerator.Generate(title, certificateNumber, holderName, verificationCode, verifyUrl, qrData);
        }

        var certificate = new Certificate
        {
            Uuid = Guid.NewGuid(),
            UserId = submission.UserId,
            AgencyId = null,
            TemplateVersionId = version.Id,
            IssuedByUserId = issuedByUserId,
            CertificateNumber = certificateNumber,
            VerificationCode = verificationCode,
            Title = title,
            Status = CertificateStatus.Active,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            QrCodeData = qrData,
            SummaryJson = JsonSerializer.Serialize(new
            {
                processType = CertificateProcessType.Accreditation.ToString(),
                accreditationSubmissionUuid = submission.Uuid,
                accreditationNumber = submission.AccreditationNumber,
                companyName = holderName,
            })
        };

        await using var pdfStream = new MemoryStream(pdfBytes);
        var (storedFileName, _) = await _fileStorage.SaveAsync(
            pdfStream,
            $"certificates/{certificate.Uuid}",
            $"{certificate.CertificateNumber}.pdf",
            cancellationToken);
        certificate.PdfStoredFileName = storedFileName;

        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            submission.UserId,
            "accreditation_certificate_issued",
            "Accreditation certificate issued",
            $"Your accreditation certificate ({certificate.CertificateNumber}) for {holderName} is now available.",
            "Certificate",
            certificate.Uuid.ToString(),
            cancellationToken);

        _logger.LogInformation(
            "Issued accreditation certificate {CertificateNumber} for submission {SubmissionUuid}",
            certificate.CertificateNumber,
            submission.Uuid);

        return new AccreditationCertificateIssueResult(
            true,
            certificate.Uuid,
            certificate.CertificateNumber,
            successMessage);
    }

    private async Task<CertificateTemplate?> ResolveTemplateAsync(CancellationToken cancellationToken)
    {
        var assignment = await _db.CertificateProcessAssignments
            .Include(a => a.Template)
                .ThenInclude(t => t.Versions)
                    .ThenInclude(v => v.Elements)
            .Where(a => a.ProcessType == CertificateProcessType.Accreditation && a.IsActive && a.Template.IsActive)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return assignment?.Template;
    }

    private static Dictionary<string, object?> BuildVariables(
        AccreditationSubmission submission,
        User submissionUser,
        User? officer,
        DateTime issuedAt,
        DateTime expiresAt)
    {
        var formData = ParseFormData(submission.FormDataJson);
        var companyName = submission.CompanyName;
        if (string.IsNullOrWhiteSpace(companyName))
        {
            companyName = submissionUser.Profile?.CompanyName
                ?? GetFormValue(formData, "company_name", "business_name", "txt_company_name")
                ?? "N/A";
        }

        var companyAddress = AccreditationFormVariableResolver.ResolveAddress(formData, submissionUser.Profile?.Address);

        var registrationNumber = GetFormValue(formData, "registration_number", "txt_registration_number") ?? "N/A";
        var tin = AccreditationFormVariableResolver.ResolveTin(formData);
        var businessType = GetFormValue(
            formData,
            "business_type",
            "txt_business_type",
            "type_of_business",
            "company_type")
            ?? "N/A";
        var natureOfBusiness = AccreditationFormVariableResolver.ResolveNatureOfBusiness(formData);

        var accreditationType = submission.SubmissionType switch
        {
            "RENEWAL" => "Renewal Accreditation",
            "UPGRADE" => "Upgrade Accreditation",
            _ => "New Accreditation",
        };

        var officerName = officer?.Profile is not null
            ? $"{officer.Profile.FirstName} {officer.Profile.LastName}".Trim()
            : "N/A";

        return new Dictionary<string, object?>
        {
            ["company"] = new Dictionary<string, object?>
            {
                ["name"] = companyName,
                ["address"] = companyAddress,
                ["registration_number"] = registrationNumber,
                ["tin"] = tin,
                ["business_type"] = businessType,
                ["nature_of_business"] = natureOfBusiness,
            },
            ["accreditation"] = new Dictionary<string, object?>
            {
                ["type"] = accreditationType,
                ["number"] = submission.AccreditationNumber ?? "N/A",
                ["status"] = submission.Status.ToString(),
            },
            ["officer"] = new Dictionary<string, object?>
            {
                ["name"] = officerName,
                ["title"] = "DA Designated Registration Officer",
            },
            ["date"] = new Dictionary<string, object?>
            {
                ["issued"] = issuedAt.ToString("MMMM dd, yyyy"),
                ["expires"] = expiresAt.ToString("MMMM dd, yyyy"),
            },
        };
    }

    private static Dictionary<string, string> ParseFormData(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(formDataJson)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? GetFormValue(IReadOnlyDictionary<string, string> formData, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (formData.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
