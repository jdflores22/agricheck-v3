using System.Text.Json;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class EntryMavCommodityResolver
{
    public static async Task<AgencyBillingCommodityDto?> ResolveBillingCommodityAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var formValues = ParseFormValues(entry.FormDataJson);
        var detailUuid = FindGuidFormValue(formValues, "_detail_uuid");
        var hsCodeFromForm = FindFormValue(formValues, "hs_code", "hsCode")
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

        MavHsDetail? mavDetail = null;
        if (detailUuid is not null)
        {
            mavDetail = await db.MavHsDetails
                .Include(d => d.Heading).ThenInclude(h => h.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Uuid == detailUuid.Value && d.IsActive, cancellationToken);
        }

        if (mavDetail is null && !string.IsNullOrWhiteSpace(hsCodeFromForm))
        {
            var normalizedHs = hsCodeFromForm.Trim();
            var compactHs = normalizedHs.Replace(".", string.Empty, StringComparison.Ordinal);
            var mavDetails = await db.MavHsDetails
                .Include(d => d.Heading).ThenInclude(h => h.Category)
                .AsNoTracking()
                .Where(d => d.IsActive && d.Heading.IsActive && d.Heading.Category.IsActive)
                .ToListAsync(cancellationToken);

            mavDetail = mavDetails.FirstOrDefault(d =>
            {
                var dotted = FormatHsCode(d.Heading.Category.HsCode, d.Heading.HeadingNumber);
                var compact = $"{d.Heading.Category.HsCode.Trim()}{d.Heading.HeadingNumber.Trim()}";
                return dotted.Equals(normalizedHs, StringComparison.OrdinalIgnoreCase)
                    || compact.Equals(compactHs, StringComparison.OrdinalIgnoreCase);
            });
        }

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
            entry.Detail?.Commodity?.Name,
            latestMic?.CommodityName,
            primaryMic?.CommodityName);

        if (string.IsNullOrWhiteSpace(commodityName) && string.IsNullOrWhiteSpace(hsCode))
        {
            return entry.Detail is null ? null : MapFromDetailOnly(entry.Detail, hsCode, null);
        }

        var categoryName = FirstNonEmpty(
            mavDetail?.Heading.Category.Description,
            entry.Detail?.Commodity?.Category?.Name);

        var quantity = entry.Detail?.Quantity > 0
            ? entry.Detail.Quantity
            : quantityFromForm ?? 0m;

        var unit = FirstNonEmpty(entry.Detail?.Unit, unitFromForm) ?? "kg";

        return new AgencyBillingCommodityDto(
            entry.Detail?.CommodityId ?? entry.Detail?.Commodity?.Id,
            commodityName,
            entry.Detail?.Commodity?.Code,
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
