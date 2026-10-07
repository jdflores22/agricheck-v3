using System.Text.Json;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

public sealed record EntryMavLibraryCommodityDto(
    bool IsLinkedToMavLibrary,
    Guid? MavHsDetailUuid,
    string HsCode,
    string CommodityName,
    string MavHsLabel,
    string? CategoryDescription,
    decimal? VolumeKg);

internal static class EntryMavCommodityResolver
{
    /// <summary>
    /// Resolves commodity display fields strictly from the MAV HS library (no free-text fallback).
    /// </summary>
    public static async Task<EntryMavLibraryCommodityDto?> ResolveLibraryCommodityAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var detail = await TryResolveMavHsDetailAsync(db, entry, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var volume = entry.Detail?.Quantity > 0 ? entry.Detail.Quantity : (decimal?)null;
        return new EntryMavLibraryCommodityDto(
            true,
            detail.Uuid,
            FormatHsCode(detail.Heading.Category.HsCode, detail.Heading.HeadingNumber),
            detail.Description.Trim(),
            MavHsDisplayHelper.BuildDetailLabel(detail),
            detail.Heading.Category.Description,
            volume);
    }

    public static async Task<AgencyBillingCommodityDto?> ResolveBillingCommodityAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken = default,
        IReadOnlyList<MavHsDetail>? mavDetailsCache = null)
    {
        var formValues = ParseFormValues(entry.FormDataJson);
        var detailUuid = FindGuidFormValue(formValues, "_detail_uuid");
        var hsCodeFromForm = FindFormValue(formValues, "hs_code", "hsCode", "import_commodity_hs_code")
            ?? FindSuffixFormValue(formValues, "_hs_code");
        var commodityNameFromForm = FindSuffixFormValue(formValues, "_commodity_name")
            ?? FindFormValue(formValues, "commodityName", "commodity");
        var originFromForm = FindFormValue(formValues, "originCountry", "origin_country");
        var destinationFromForm = FindFormValue(formValues, "destinationCountry", "destination_country");
        var portFromForm = FindFormValue(formValues, "portOfEntry", "port_of_entry");
        var descriptionFromForm = FindFormValue(formValues, "description");
        var quantityFromForm = ParseDecimal(
            FindFormValue(formValues, "quantity", "txt_volume_weight", "volume", "weight"));
        var unitFromForm = FindFormValue(formValues, "unit");

        var mavDetail = await TryResolveMavHsDetailAsync(
            db,
            entry,
            cancellationToken,
            detailUuid,
            hsCodeFromForm,
            mavDetailsCache);

        var primaryMic = entry.PrimaryMic;
        var latestMic = entry.MicUtilizations
            .OrderByDescending(u => u.UtilizedAt)
            .Select(u => u.Mic)
            .FirstOrDefault();

        var hsCode = mavDetail is not null
            ? FormatHsCode(mavDetail.Heading.Category.HsCode, mavDetail.Heading.HeadingNumber)
            : FirstNonEmpty(hsCodeFromForm, latestMic?.HsCode, primaryMic?.HsCode);

        if (string.IsNullOrWhiteSpace(hsCode) && !string.IsNullOrWhiteSpace(entry.Detail?.CommodityName))
        {
            var commodityLookup = entry.Detail!.CommodityName!.Trim();
            var categories = await db.MavHsCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);
            var category = categories.FirstOrDefault(c =>
                c.Description.Contains(commodityLookup, StringComparison.OrdinalIgnoreCase)
                || commodityLookup.Contains(c.Description, StringComparison.OrdinalIgnoreCase));
            if (category is not null)
            {
                hsCode = category.HsCode;
            }
        }

        var commodityName = FirstNonEmpty(
            mavDetail?.Description,
            commodityNameFromForm,
            entry.Detail?.CommodityName,
            latestMic?.CommodityName,
            primaryMic?.CommodityName);

        if (string.IsNullOrWhiteSpace(commodityName) && string.IsNullOrWhiteSpace(hsCode))
        {
            return entry.Detail is null ? null : MapFromDetailOnly(entry.Detail, hsCode, null);
        }

        var categoryName = mavDetail?.Heading.Category.Description;

        var quantity = entry.Detail?.Quantity > 0
            ? entry.Detail.Quantity
            : quantityFromForm ?? 0m;

        var unit = FirstNonEmpty(entry.Detail?.Unit, unitFromForm) ?? "kg";

        return new AgencyBillingCommodityDto(
            entry.Detail?.CommodityId,
            commodityName,
            null,
            categoryName,
            FirstNonEmpty(entry.Detail?.Description, descriptionFromForm, mavDetail?.Description),
            quantity,
            unit,
            FirstNonEmpty(entry.Detail?.OriginCountry, originFromForm),
            FirstNonEmpty(entry.Detail?.DestinationCountry, destinationFromForm, "Philippines"),
            FirstNonEmpty(entry.Detail?.PortOfEntry, portFromForm),
            hsCode,
            mavDetail is not null ? MavHsDisplayHelper.BuildDetailLabel(mavDetail) : null);
    }

    public static async Task<IReadOnlyList<MavHsDetail>> LoadActiveMavHsDetailsAsync(
        AgriCheckDbContext db,
        CancellationToken cancellationToken = default) =>
        await db.MavHsDetails
            .Include(d => d.Heading).ThenInclude(h => h.Category)
            .AsNoTracking()
            .Where(d => d.IsActive && d.Heading.IsActive && d.Heading.Category.IsActive)
            .ToListAsync(cancellationToken);

    private static async Task<MavHsDetail?> TryResolveMavHsDetailAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken,
        Guid? detailUuidOverride = null,
        string? hsCodeFromFormOverride = null,
        IReadOnlyList<MavHsDetail>? mavDetailsCache = null)
    {
        var formValues = ParseFormValues(entry.FormDataJson);
        var detailUuid = detailUuidOverride ?? FindGuidFormValue(formValues, "_detail_uuid");
        var hsCodeFromForm = hsCodeFromFormOverride
            ?? FindFormValue(formValues, "hs_code", "hsCode")
            ?? FindSuffixFormValue(formValues, "_hs_code");

        if (detailUuid is not null)
        {
            var byUuid = await db.MavHsDetails
                .Include(d => d.Heading).ThenInclude(h => h.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Uuid == detailUuid.Value && d.IsActive, cancellationToken);
            if (byUuid is not null)
            {
                return byUuid;
            }
        }

        if (!string.IsNullOrWhiteSpace(hsCodeFromForm))
        {
            var match = await MatchDetailByHsCodeAsync(db, hsCodeFromForm.Trim(), cancellationToken, mavDetailsCache);
            if (match is not null)
            {
                return match;
            }
        }

        var micHs = entry.MicUtilizations
            .OrderByDescending(u => u.UtilizedAt)
            .Select(u => u.Mic)
            .FirstOrDefault()?.HsCode
            ?? entry.PrimaryMic?.HsCode;

        if (!string.IsNullOrWhiteSpace(micHs))
        {
            return await MatchDetailByHsCodeAsync(db, micHs.Trim(), cancellationToken, mavDetailsCache);
        }

        return null;
    }

    private static async Task<MavHsDetail?> MatchDetailByHsCodeAsync(
        AgriCheckDbContext db,
        string normalizedHs,
        CancellationToken cancellationToken,
        IReadOnlyList<MavHsDetail>? mavDetailsCache = null)
    {
        var compactHs = normalizedHs.Replace(".", string.Empty, StringComparison.Ordinal);
        var mavDetails = mavDetailsCache ?? await LoadActiveMavHsDetailsAsync(db, cancellationToken);

        return mavDetails.FirstOrDefault(d =>
        {
            var dotted = FormatHsCode(d.Heading.Category.HsCode, d.Heading.HeadingNumber);
            var compact = $"{d.Heading.Category.HsCode.Trim()}{d.Heading.HeadingNumber.Trim()}";
            return dotted.Equals(normalizedHs, StringComparison.OrdinalIgnoreCase)
                || compact.Equals(compactHs, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static AgencyBillingCommodityDto MapFromDetailOnly(EntryDetail detail, string? hsCode, string? mavHsLabel) =>
        new(
            detail.CommodityId,
            detail.CommodityName ?? detail.Commodity?.Name,
            detail.Commodity?.Code,
            detail.Commodity?.Category?.Name,
            detail.Description,
            detail.Quantity,
            detail.Unit,
            detail.OriginCountry,
            detail.DestinationCountry,
            detail.PortOfEntry,
            hsCode,
            mavHsLabel);

    private static string FormatHsCode(string categoryHsCode, string headingNumber) =>
        string.IsNullOrWhiteSpace(headingNumber)
            ? categoryHsCode.Trim()
            : $"{categoryHsCode.Trim()}.{headingNumber.Trim()}";

    private static Dictionary<string, string> ParseFormValues(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(formDataJson);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Guid? FindGuidFormValue(IReadOnlyDictionary<string, string> values, string suffix)
    {
        var raw = FindSuffixFormValue(values, suffix);
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string? FindSuffixFormValue(IReadOnlyDictionary<string, string> values, string suffix) =>
        values.FirstOrDefault(pair => pair.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pair.Value)).Value?.Trim();

    private static string? FindFormValue(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static decimal? ParseDecimal(string? raw) =>
        decimal.TryParse(raw, out var parsed) && parsed > 0 ? parsed : null;

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
