namespace AgriCheck.Infrastructure.Helpers;

internal static class ShipmentQuantityHelper
{
    public static decimal ToKg(decimal quantity, string? unit)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        var u = (unit ?? "kg").Trim().ToUpperInvariant();
        return u switch
        {
            "MT" or "M/T" or "TON" or "TONNE" or "TONS" or "MTON" => quantity * 1000m,
            "G" or "GRAM" or "GRAMS" => quantity / 1000m,
            _ => quantity,
        };
    }
}
