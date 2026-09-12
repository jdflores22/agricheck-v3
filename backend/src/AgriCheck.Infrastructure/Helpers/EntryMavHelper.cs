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

    public static async Task ValidateImportMavOnSubmitAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken)
    {
        if (entry.EntryType != EntryType.Import)
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
}
