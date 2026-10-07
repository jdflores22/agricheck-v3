using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal sealed record WarehouseFacilityScopeResolution(
    IReadOnlyList<WarehouseCommodityClass> AllowedClasses,
    string Source,
    string? CswLeadAgencyCode,
    string DisplayHint);

internal static class WarehouseFacilityProductScopeHelper
{
    public static async Task<WarehouseFacilityScopeResolution> ResolveAsync(
        AgriCheckDbContext db,
        long warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        var registration = await db.FacilityRegistrations.AsNoTracking()
            .Include(r => r.Accreditations)
            .ThenInclude(a => a.LeadAgency)
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseFacilityId, cancellationToken);

        var csw = registration?.Accreditations
            .OrderByDescending(a => a.AccreditedAt)
            .FirstOrDefault();

        var leadCode = csw?.LeadAgency?.Code?.Trim().ToUpperInvariant();

        var fromRsas = WarehouseCommodityClassHelper.ParseClassListJson(registration?.ProductsJson);
        if (fromRsas.Count > 0)
        {
            EnsureScopeSetIsValid(fromRsas);
            return new WarehouseFacilityScopeResolution(
                fromRsas,
                "RSAS",
                leadCode,
                BuildDisplayHint(fromRsas, leadCode));
        }

        var inferred = InferFromLeadAgency(leadCode);
        if (inferred.Count > 0)
        {
            return new WarehouseFacilityScopeResolution(
                inferred,
                "CSW_TRA",
                leadCode,
                BuildDisplayHint(inferred, leadCode));
        }

        return new WarehouseFacilityScopeResolution(
            Array.Empty<WarehouseCommodityClass>(),
            "UNDEFINED",
            leadCode,
            "Register RSAS product scope or record CSW with lead bureau (BAI = meat, BFAR = fish).");
    }

    public static void ValidateContractScope(
        IReadOnlyList<WarehouseCommodityClass> contractClasses,
        WarehouseFacilityScopeResolution facilityScope)
    {
        if (contractClasses.Count == 0)
        {
            throw new ClientPortalException(
                "CONTRACT_SCOPE_REQUIRED",
                "Select at least one product class allowed on this storage contract.");
        }

        EnsureScopeSetIsValid(contractClasses);

        if (facilityScope.AllowedClasses.Count == 0)
        {
            throw new ClientPortalException(
                "FACILITY_SCOPE_UNDEFINED",
                "This warehouse has no registered product scope yet. Complete RSAS (commodity classes) or DA CSW (lead bureau) before allocating importer contracts.");
        }

        foreach (var contractClass in contractClasses)
        {
            if (!facilityScope.AllowedClasses.Contains(contractClass))
            {
                var label = contractClass.ToString();
                throw new ClientPortalException(
                    "CONTRACT_SCOPE_EXCEEDS_FACILITY",
                    $"{label} is not approved for this warehouse. {facilityScope.DisplayHint} Meat and fish cannot be mixed in one facility.");
            }
        }
    }

    public static void ValidateRsasProductsJson(IReadOnlyList<WarehouseCommodityClass> classes)
    {
        if (classes.Count == 0)
        {
            return;
        }

        EnsureScopeSetIsValid(classes);
    }

    private static void EnsureScopeSetIsValid(IReadOnlyList<WarehouseCommodityClass> classes)
    {
        if (ContainsSegregationConflict(classes))
        {
            throw new ClientPortalException(
                "FACILITY_SEGREGATION_VIOLATION",
                "Meat (including pork) and fish cannot be registered or stored in the same warehouse due to cold-chain segregation rules. Choose one primary class family per facility.");
        }
    }

    private static bool ContainsSegregationConflict(IReadOnlyList<WarehouseCommodityClass> classes)
    {
        var hasMeat = classes.Contains(WarehouseCommodityClass.MeatAndMeatProducts);
        var hasFish = classes.Contains(WarehouseCommodityClass.FishAndAquaticProducts);
        return hasMeat && hasFish;
    }

    private static List<WarehouseCommodityClass> InferFromLeadAgency(string? leadAgencyCode)
    {
        if (string.IsNullOrWhiteSpace(leadAgencyCode))
        {
            return new List<WarehouseCommodityClass>();
        }

        return leadAgencyCode switch
        {
            "BAI" or "NMIS" => new List<WarehouseCommodityClass> { WarehouseCommodityClass.MeatAndMeatProducts },
            "BFAR" => new List<WarehouseCommodityClass> { WarehouseCommodityClass.FishAndAquaticProducts },
            _ => new List<WarehouseCommodityClass>(),
        };
    }

    private static string BuildDisplayHint(IReadOnlyList<WarehouseCommodityClass> classes, string? leadCode)
    {
        if (classes.Count == 1 && classes[0] == WarehouseCommodityClass.MeatAndMeatProducts)
        {
            return leadCode is "BAI" or "NMIS"
                ? $"Meat-only warehouse ({leadCode} CSW). Fish and aquatic products are not allowed."
                : "Meat-only warehouse. Fish and aquatic products are not allowed.";
        }

        if (classes.Count == 1 && classes[0] == WarehouseCommodityClass.FishAndAquaticProducts)
        {
            return leadCode == "BFAR"
                ? "Fish-only warehouse (BFAR CSW). Meat products (pork, poultry, etc.) are not allowed."
                : "Fish-only warehouse. Meat products are not allowed.";
        }

        var names = string.Join(", ", classes);
        return $"Approved scope: {names}. Meat and fish cannot be combined.";
    }
}
