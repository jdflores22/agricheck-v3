using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public interface IEntryCertificateGenerationService
{
    Task<CertificateTemplateVersion?> ResolveTemplateVersionAsync(Entry entry, long? templateId = null, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(
        Entry entry,
        string certificateNumber,
        string title,
        string verificationCode,
        string verifyUrl,
        string qrData,
        DateTime issuedAt,
        DateTime? expiresAt,
        long issuedByUserId,
        long? templateId = null,
        CancellationToken cancellationToken = default);
}

public class EntryCertificateGenerationService : IEntryCertificateGenerationService
{
    private readonly AgriCheckDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly string _storageRoot;
    private readonly ILogger<EntryCertificateGenerationService> _logger;

    public EntryCertificateGenerationService(
        AgriCheckDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<EntryCertificateGenerationService> logger)
    {
        _db = db;
        _configuration = configuration;
        _storageRoot = UploadStorage.ResolveRoot(configuration, environment);
        _logger = logger;
    }

    public async Task<CertificateTemplateVersion?> ResolveTemplateVersionAsync(
        Entry entry,
        long? templateId = null,
        CancellationToken cancellationToken = default)
    {
        CertificateTemplate? template;
        if (templateId is not null)
        {
            template = await _db.CertificateTemplates
                .Include(t => t.Versions).ThenInclude(v => v.Elements)
                .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        }
        else
        {
            var processType = entry.EntryType == EntryType.Export
                ? CertificateProcessType.ExportEntry
                : CertificateProcessType.ImportEntry;

            template = await _db.CertificateProcessAssignments
                .Include(a => a.Template).ThenInclude(t => t!.Versions).ThenInclude(v => v.Elements)
                .Where(a => a.AgencyId == entry.AgencyId &&
                            a.ProcessType == processType &&
                            a.IsActive &&
                            a.Template.IsActive)
                .Select(a => a.Template)
                .FirstOrDefaultAsync(cancellationToken);

            template ??= await _db.CertificateTemplates
                .Include(t => t.Versions).ThenInclude(v => v.Elements)
                .Where(t => t.IsActive && t.AgencyId == entry.AgencyId)
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (template is null)
        {
            return null;
        }

        return template.Versions
                .Where(v => v.IsPublished && v.Elements.Count > 0)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault()
            ?? template.Versions
                .Where(v => v.Elements.Count > 0)
                .MaxBy(v => v.VersionNumber);
    }

    public async Task<byte[]> GeneratePdfAsync(
        Entry entry,
        string certificateNumber,
        string title,
        string verificationCode,
        string verifyUrl,
        string qrData,
        DateTime issuedAt,
        DateTime? expiresAt,
        long issuedByUserId,
        long? templateId = null,
        CancellationToken cancellationToken = default)
    {
        var entryUser = entry.User ?? await _db.Users
            .Include(u => u.Profile)
            .FirstAsync(u => u.Id == entry.UserId, cancellationToken);

        if (entry.Agency is null)
        {
            entry.Agency = await _db.Agencies.FirstAsync(a => a.Id == entry.AgencyId, cancellationToken);
        }

        if (entry.Detail is null)
        {
            entry.Detail = await _db.EntryDetails.FirstOrDefaultAsync(d => d.EntryId == entry.Id, cancellationToken);
        }

        var billing = await _db.AgencyBillings
            .Include(b => b.VerifiedBy).ThenInclude(u => u!.Profile)
            .Where(b => b.EntryId == entry.Id && b.Status == AgencyBillingStatus.Paid)
            .OrderByDescending(b => b.PaidAt)
            .FirstOrDefaultAsync(cancellationToken);

        var officer = await _db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == issuedByUserId, cancellationToken);

        var variables = EntryCertificateVariableBuilder.Build(
            entry,
            entryUser,
            certificateNumber,
            issuedAt,
            expiresAt,
            officer,
            billing);

        var version = await ResolveTemplateVersionAsync(entry, templateId, cancellationToken);
        var elements = version?.Elements.OrderBy(e => e.SortOrder).ToList() ?? new List<CertificateElement>();

        if (version is not null && elements.Count > 0)
        {
            try
            {
                return CertificateTemplatePdfGenerator.Generate(
                    version,
                    elements,
                    variables,
                    verifyUrl,
                    qrData,
                    _storageRoot);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Template PDF generation failed for entry {Reference}; using fallback layout",
                    entry.ReferenceNo);
            }
        }
        else
        {
            _logger.LogWarning(
                "No published entry certificate template with elements for agency {AgencyId}; using fallback layout",
                entry.AgencyId);
        }

        var holderName = entryUser.Profile is not null
            ? $"{entryUser.Profile.FirstName} {entryUser.Profile.LastName}".Trim()
            : entryUser.Email;

        return CertificatePdfGenerator.Generate(
            title,
            certificateNumber,
            holderName,
            verificationCode,
            verifyUrl,
            qrData);
    }
}
