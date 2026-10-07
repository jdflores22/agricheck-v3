using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Helpers;

/// <summary>
/// Entry filing agency (BAI, BFAR, BPI, …) must align with warehouse CSW lead bureau / RSAS product scope.
/// </summary>
public static class WarehouseEntryAgencyCompatibilityHelper
{
    public static IReadOnlyList<WarehouseCommodityClass> RegulatoryCommodityClasses(string entryAgencyCode)
    {
        var code = NormalizeAgencyCode(entryAgencyCode);
        return code switch
        {
            "BAI" or "NMIS" => new List<WarehouseCommodityClass>
            {
                WarehouseCommodityClass.MeatAndMeatProducts,
                WarehouseCommodityClass.EggsMilkAnimalByProducts,
            },
            "BFAR" => new List<WarehouseCommodityClass> { WarehouseCommodityClass.FishAndAquaticProducts },
            "BPI" => new List<WarehouseCommodityClass> { WarehouseCommodityClass.PlantProducts },
            _ => Array.Empty<WarehouseCommodityClass>(),
        };
    }

    internal static bool IsCompatibleWithEntryAgency(
        WarehouseFacilityScopeResolution facilityScope,
        string entryAgencyCode)
    {
        var code = NormalizeAgencyCode(entryAgencyCode);
        if (string.IsNullOrEmpty(code))
        {
            return true;
        }

        var lead = facilityScope.CswLeadAgencyCode?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(lead))
        {
            if (lead == code)
            {
                return true;
            }

            if (code == "BAI" && lead == "NMIS")
            {
                return true;
            }
        }

        var agencyClasses = RegulatoryCommodityClasses(code);
        if (agencyClasses.Count == 0)
        {
            return true;
        }

        if (facilityScope.AllowedClasses.Count == 0)
        {
            return false;
        }

        return facilityScope.AllowedClasses.Any(agencyClasses.Contains);
    }

    public static string DestinationFilterHint(string entryAgencyCode)
    {
        var code = NormalizeAgencyCode(entryAgencyCode);
        return code switch
        {
            "BAI" => "Meat-only destinations (BAI / NMIS CSW or RSAS meat scope). Fish warehouses are hidden.",
            "NMIS" => "Meat cold-chain destinations (NMIS / BAI scope). Fish warehouses are hidden.",
            "BFAR" => "Fish and aquatic destinations (BFAR CSW or RSAS fish scope). Meat warehouses are hidden.",
            "BPI" => "Plant-product destinations (BPI scope). Meat and fish warehouses are hidden.",
            _ => "Destinations are filtered to match this agency's regulatory scope.",
        };
    }

    private static string NormalizeAgencyCode(string entryAgencyCode) =>
        entryAgencyCode.Trim().ToUpperInvariant();
}
