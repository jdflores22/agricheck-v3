using System.Text.Json;
using AgriCheck.Domain.Enums;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class WarehouseCommodityClassHelper
{
    public static WarehouseCommodityClass? FromHsCategoryCode(string? hsCode)
    {
        if (string.IsNullOrWhiteSpace(hsCode))
        {
            return null;
        }

        var digits = new string(hsCode.Trim().TakeWhile(char.IsDigit).ToArray());
        if (digits.Length < 2 || !int.TryParse(digits[..2], out var chapter))
        {
            return null;
        }

        return chapter switch
        {
            1 or 2 => WarehouseCommodityClass.MeatAndMeatProducts,
            3 => WarehouseCommodityClass.FishAndAquaticProducts,
            4 => WarehouseCommodityClass.EggsMilkAnimalByProducts,
            6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 => WarehouseCommodityClass.PlantProducts,
            _ => WarehouseCommodityClass.OtherAgri,
        };
    }

    public static bool ClassesSegregationConflict(WarehouseCommodityClass a, WarehouseCommodityClass b)
    {
        if (a == b)
        {
            return false;
        }

        var meat = WarehouseCommodityClass.MeatAndMeatProducts;
        var fish = WarehouseCommodityClass.FishAndAquaticProducts;
        return (a == meat && b == fish) || (a == fish && b == meat);
    }

    public static IReadOnlyList<WarehouseCommodityClass> ParseClassListJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<WarehouseCommodityClass>();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<WarehouseCommodityClass>();
            }

            var list = new List<WarehouseCommodityClass>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String
                    && Enum.TryParse<WarehouseCommodityClass>(el.GetString(), true, out var parsed))
                {
                    list.Add(parsed);
                }
            }

            return list;
        }
        catch (JsonException)
        {
            return Array.Empty<WarehouseCommodityClass>();
        }
    }

    public static IReadOnlyList<string> FormatClassLabels(IReadOnlyList<WarehouseCommodityClass> classes) =>
        classes.Select(c => c.ToString()).ToList();

    public static async Task<WarehouseCommodityClass?> ResolveFromFormDataAsync(
        AgriCheckDbContext db,
        string? formDataJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return null;
        }

        var formValues = EntryFormJsonHelper.ParseFlatValues(formDataJson);
        var detailUuid = EntryFormJsonHelper.FindGuid(formValues, "_detail_uuid")
            ?? EntryFormJsonHelper.FindGuidSuffix(formValues, "_detail_uuid");
        var hsCode = EntryFormJsonHelper.FindValue(formValues, "hs_code", "hsCode")
            ?? EntryFormJsonHelper.FindSuffix(formValues, "_hs_code");

        if (detailUuid is not null)
        {
            var byUuid = await db.MavHsDetails
                .Include(d => d.Heading).ThenInclude(h => h.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Uuid == detailUuid.Value && d.IsActive, cancellationToken);
            if (byUuid is not null)
            {
                return FromHsCategoryCode(byUuid.Heading.Category.HsCode);
            }
        }

        if (!string.IsNullOrWhiteSpace(hsCode))
        {
            var detail = await MatchMavDetailByHsCodeAsync(db, hsCode.Trim(), cancellationToken);
            if (detail is not null)
            {
                return FromHsCategoryCode(detail.Heading.Category.HsCode);
            }

            return FromHsCategoryCode(hsCode);
        }

        return null;
    }

    public static async Task<WarehouseCommodityClass?> ResolveFromEntryAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken)
    {
        var fromForm = await ResolveFromFormDataAsync(db, entry.FormDataJson, cancellationToken);
        if (fromForm is not null)
        {
            return fromForm;
        }

        await db.Entry(entry).Collection(e => e.MicUtilizations).LoadAsync(cancellationToken);
        if (entry.PrimaryMicId is not null)
        {
            await db.Entry(entry).Reference(e => e.PrimaryMic).LoadAsync(cancellationToken);
        }

        var micHs = entry.MicUtilizations
            .OrderByDescending(u => u.UtilizedAt)
            .Select(u => u.Mic.HsCode)
            .FirstOrDefault()
            ?? entry.PrimaryMic?.HsCode;

        return FromHsCategoryCode(micHs);
    }

    private static async Task<MavHsDetail?> MatchMavDetailByHsCodeAsync(
        AgriCheckDbContext db,
        string normalizedHs,
        CancellationToken cancellationToken)
    {
        var compactHs = normalizedHs.Replace(".", string.Empty, StringComparison.Ordinal);
        var mavDetails = await db.MavHsDetails
            .Include(d => d.Heading).ThenInclude(h => h.Category)
            .AsNoTracking()
            .Where(d => d.IsActive && d.Heading.IsActive && d.Heading.Category.IsActive)
            .ToListAsync(cancellationToken);

        return mavDetails.FirstOrDefault(d =>
        {
            var category = d.Heading.Category.HsCode.Trim();
            var heading = d.Heading.HeadingNumber.Trim();
            var dotted = string.IsNullOrWhiteSpace(heading) ? category : $"{category}.{heading}";
            var compact = $"{category}{heading}";
            return dotted.Equals(normalizedHs, StringComparison.OrdinalIgnoreCase)
                || compact.Equals(compactHs, StringComparison.OrdinalIgnoreCase);
        });
    }
}
