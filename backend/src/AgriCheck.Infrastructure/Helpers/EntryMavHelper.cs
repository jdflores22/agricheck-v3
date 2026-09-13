using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class EntryMavHelper
{
    public const string MavCertificateDocumentType = "mav_certificate";

    public static string? NormalizeMavNo(string? mavNo) =>
        string.IsNullOrWhiteSpace(mavNo) ? null : mavNo.Trim().ToUpperInvariant();

    public static EntryMavInfoDto? MapMavInfo(Entry entry)
    {
        if (entry.EntryType != EntryType.Import)
        {
            return null;
        }

        var mavFile = entry.Files
            .Where(f => string.Equals(f.DocumentType, MavCertificateDocumentType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefault();

        var utilizations = entry.MicUtilizations
            .OrderByDescending(u => u.UtilizedAt)
            .Select(u => new EntryMicUtilizationDto(
                u.Mic.Uuid,
                u.Mic.CertificateNumber,
                u.Volume,
                u.UtilizedAt,
                u.Mic.HsCode,
                u.Mic.CommodityName))
            .ToList();

        return new EntryMavInfoDto(
            entry.MavNo,
            entry.ImportTrack.ToString(),
            entry.MavDocumentStatus.ToString(),
            entry.MavRemarks,
            mavFile?.Uuid,
            mavFile?.OriginalFileName,
            utilizations,
            utilizations.Sum(u => u.Volume),
            entry.Detail?.Quantity);
    }

    public static EntryMavDocumentStatus MapEvaluationToMavStatus(EvaluationDecision decision) => decision switch
    {
        EvaluationDecision.Approved => EntryMavDocumentStatus.Approved,
        EvaluationDecision.Rejected => EntryMavDocumentStatus.Rejected,
        EvaluationDecision.RevisionRequired => EntryMavDocumentStatus.RevisionRequired,
        _ => EntryMavDocumentStatus.PendingReview
    };

    public static async Task EnsureMavNoAvailableAsync(
        AgriCheckDbContext db,
        string? mavNo,
        long? excludeEntryId,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeMavNo(mavNo);
        if (normalized is null)
        {
            throw new ClientPortalException("MAV_NO_REQUIRED", "MAV No. is required for import entries.");
        }

        var taken = await db.Entries.AnyAsync(
            e => e.MavNo == normalized && (excludeEntryId == null || e.Id != excludeEntryId),
            cancellationToken);

        if (taken)
        {
            throw new ClientPortalException("MAV_NO_TAKEN", "MAV No. is already used by another entry.");
        }
    }

    public static EntryImportTrack ResolveImportTrack(
        string? importTrack,
        string? formDataJson = null,
        EntryImportTrack? current = null)
    {
        if (Enum.TryParse<EntryImportTrack>(importTrack, true, out var parsed) && parsed is EntryImportTrack.Mav or EntryImportTrack.Regular)
        {
            return parsed;
        }

        var fromForm = ReadFormDataValue(formDataJson, "import_track");
        if (Enum.TryParse<EntryImportTrack>(fromForm, true, out parsed) && parsed is EntryImportTrack.Mav or EntryImportTrack.Regular)
        {
            return parsed;
        }

        return current ?? EntryImportTrack.Regular;
    }

    public static void EnsureImportTrackChangeAllowed(Entry entry, EntryImportTrack next)
    {
        if (next == EntryImportTrack.Regular && entry.PrimaryMicId is not null)
        {
            throw new ClientPortalException(
                "MAV_TRACK_LOCKED",
                "This entry already has MIC utilization. Keep the MAV import track.");
        }
    }

    public static bool IsMavTrack(Entry entry) =>
        entry.EntryType == EntryType.Import && entry.ImportTrack == EntryImportTrack.Mav;

    public static async Task ValidateImportMavOnSubmitAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken)
    {
        if (entry.EntryType != EntryType.Import || entry.ImportTrack != EntryImportTrack.Mav)
        {
            return;
        }

        if (NormalizeMavNo(entry.MavNo) is null)
        {
            throw new ClientPortalException("MAV_NO_REQUIRED", "Provide a MAV No. before submitting this import entry.");
        }

        var hasMavCertificate = entry.Files.Any(f =>
            string.Equals(f.DocumentType, MavCertificateDocumentType, StringComparison.OrdinalIgnoreCase));

        if (!hasMavCertificate)
        {
            throw new ClientPortalException("MAV_CERT_REQUIRED", "Upload the MAV certificate before submitting.");
        }

        var utilizedVolume = await db.MicUtilizations
            .Where(u => u.EntryId == entry.Id)
            .SumAsync(u => (decimal?)u.Volume, cancellationToken) ?? 0m;

        var requiredVolume = entry.Detail?.Quantity ?? 0m;
        if (requiredVolume <= 0)
        {
            throw new ClientPortalException("ENTRY_INCOMPLETE", "Entry quantity is required before submitting.");
        }

        if (utilizedVolume < requiredVolume)
        {
            throw new ClientPortalException(
                "MIC_UTILIZATION_REQUIRED",
                $"Link an active MIC and utilize at least {requiredVolume:0.###} volume before submitting. Currently utilized: {utilizedVolume:0.###}.");
        }
    }

    public static async Task<bool> ImporterHasMavAccessAsync(
        AgriCheckDbContext db,
        long agencyId,
        long importerId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var hsPrefixes = await db.MavHsCategories
            .Where(c => c.IsActive && c.AgencyId == agencyId)
            .Select(c => c.HsCode)
            .ToListAsync(cancellationToken);

        var licenseHsCodes = await db.MavLicenses
            .Where(l => l.ImporterId == importerId && l.Status == MavLicenseStatus.Active && l.ExpiresAt >= now)
            .Select(l => l.HsCode)
            .ToListAsync(cancellationToken);
        if (MatchesAgencyHs(licenseHsCodes, hsPrefixes))
        {
            return true;
        }

        var micHsCodes = await db.MavImportCertificates
            .Where(m =>
                m.ImporterId == importerId
                && m.Status != MavImportCertificateStatus.Expired
                && m.ExpiresAt >= now
                && m.AuthorizedVolume > m.UtilizedVolume)
            .Select(m => m.HsCode)
            .ToListAsync(cancellationToken);

        return MatchesAgencyHs(micHsCodes, hsPrefixes);
    }

    private static bool MatchesAgencyHs(IReadOnlyCollection<string> hsCodes, IReadOnlyCollection<string> hsPrefixes)
    {
        if (hsCodes.Count == 0)
        {
            return false;
        }

        if (hsPrefixes.Count == 0)
        {
            return true;
        }

        return hsCodes.Any(hs => hsPrefixes.Any(prefix => hs.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? ReadFormDataValue(string? formDataJson, string key)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(formDataJson);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            return document.RootElement.TryGetProperty(key, out var value) && value.ValueKind == System.Text.Json.JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
