using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class PhilippineAddressSeeder
{
    private static readonly Dictionary<string, Dictionary<string, string>> ZipCodesByCity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MNL"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ermita"] = "1000",
            ["Malate"] = "1004",
            ["Paco"] = "1007",
            ["Quiapo"] = "1001",
            ["Sampaloc"] = "1008",
        },
        ["QC"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Diliman"] = "1101",
            ["Project 6"] = "1100",
            ["Batasan Hills"] = "1126",
            ["Cubao"] = "1109",
            ["Fairview"] = "1118",
        },
        ["MKT"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Poblacion"] = "1210",
            ["Bel-Air"] = "1209",
            ["San Lorenzo"] = "1223",
            ["Legazpi Village"] = "1229",
            ["Cembo"] = "1214",
        },
        ["PSG"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["San Antonio"] = "1605",
            ["Ugong"] = "1604",
            ["Oranbo"] = "1600",
            ["Kapitolyo"] = "1603",
            ["Manggahan"] = "1611",
        },
        ["TAG"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fort Bonifacio"] = "1634",
            ["Western Bicutan"] = "1632",
            ["Ususan"] = "1637",
            ["Pinagsama"] = "1630",
            ["Tuktukan"] = "1639",
        },
        ["MND"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Addition Hills"] = "1550",
            ["Plainview"] = "1550",
            ["Wack-Wack"] = "1555",
            ["Highway Hills"] = "1554",
            ["Barangka Drive"] = "1550",
        },
        ["MRK"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sta. Elena"] = "1800",
            ["Industrial Valley"] = "1800",
            ["Concepcion Uno"] = "1807",
            ["Parang"] = "1809",
            ["Nangka"] = "1808",
        },
        ["PSY"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Malibay"] = "1300",
            ["San Rafael"] = "1302",
            ["Villamor"] = "1309",
            ["Domestic Road"] = "1301",
            ["Baclaran"] = "1302",
        },
        ["PRQ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BF Homes"] = "1720",
            ["Baclaran"] = "1702",
            ["Don Bosco"] = "1700",
            ["San Dionisio"] = "1700",
            ["Tambo"] = "1701",
        },
        ["MUN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Alabang"] = "1780",
            ["Putatan"] = "1772",
            ["Tunasan"] = "1773",
            ["Cupang"] = "1771",
            ["Ayala Alabang"] = "1780",
        },
        ["CLN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bagong Barrio"] = "1400",
            ["Grace Park"] = "1406",
            ["Camarin"] = "1422",
            ["Deparo"] = "1420",
            ["177"] = "1422",
        },
        ["MLB"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Longos"] = "1472",
            ["Potrero"] = "1475",
            ["Tañong"] = "1470",
            ["Acacia"] = "1474",
            ["Tonsuya"] = "1473",
        },
        ["NVT"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sipac-Almacen"] = "1485",
            ["Tanza"] = "1489",
            ["NBBS Proper"] = "1485",
            ["San Jose"] = "1485",
            ["Tangos"] = "1489",
        },
        ["VLZ"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Gen. T. de Leon"] = "1442",
            ["Malinta"] = "1440",
            ["Marulas"] = "1440",
            ["Karuhatan"] = "1441",
            ["Canumay West"] = "1443",
        },
        ["SJN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Greenhills"] = "1502",
            ["West Crame"] = "1504",
            ["Balong-Bato"] = "1500",
            ["Isabelita"] = "1500",
            ["Onse"] = "1500",
        },
        ["PTR"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["San Roque"] = "1620",
            ["Aguho"] = "1620",
            ["Martires Del 96"] = "1620",
            ["Santo Rosario-Kanluran"] = "1620",
            ["Santo Rosario-Silangan"] = "1620",
        },
    };

    private static readonly (string Name, string ZipCode)[] LasPinasBarangays =
    {
        ("Almanza Uno", "1748"),
        ("Almanza Dos", "1750"),
        ("BF International Village", "1740"),
        ("Daniel Fajardo", "1740"),
        ("Elias Aldana", "1740"),
        ("Ilaya", "1740"),
        ("Manuyo Uno", "1744"),
        ("Manuyo Dos", "1744"),
        ("Pamplona Uno", "1746"),
        ("Pamplona Dos", "1741"),
        ("Pamplona Tres", "1740"),
        ("Pilar", "1746"),
        ("Pulang Lupa Uno", "1742"),
        ("Pulang Lupa Dos", "1742"),
        ("Talon Uno", "1747"),
        ("Talon Dos", "1747"),
        ("Talon Tres", "1747"),
        ("Talon Kuatro", "1747"),
        ("Talon Singko", "1747"),
        ("Zapote", "1742"),
    };

    private static readonly string[] LegacyLasPinasBarangayNames =
    {
        "Pamplona",
        "Talon",
        "Pulang Lupa",
        "CAA",
    };

    private static readonly (string Code, string Name, string[] Barangays)[] Cities =
    {
        ("MNL", "Manila", new[] { "Ermita", "Malate", "Paco", "Quiapo", "Sampaloc" }),
        ("QC", "Quezon City", new[] { "Diliman", "Project 6", "Batasan Hills", "Cubao", "Fairview" }),
        ("MKT", "Makati", new[] { "Poblacion", "Bel-Air", "San Lorenzo", "Legazpi Village", "Cembo" }),
        ("PSG", "Pasig", new[] { "San Antonio", "Ugong", "Oranbo", "Kapitolyo", "Manggahan" }),
        ("TAG", "Taguig", new[] { "Fort Bonifacio", "Western Bicutan", "Ususan", "Pinagsama", "Tuktukan" }),
        ("MND", "Mandaluyong", new[] { "Addition Hills", "Plainview", "Wack-Wack", "Highway Hills", "Barangka Drive" }),
        ("MRK", "Marikina", new[] { "Sta. Elena", "Industrial Valley", "Concepcion Uno", "Parang", "Nangka" }),
        ("PSY", "Pasay", new[] { "Malibay", "San Rafael", "Villamor", "Domestic Road", "Baclaran" }),
        ("PRQ", "Parañaque", new[] { "BF Homes", "Baclaran", "Don Bosco", "San Dionisio", "Tambo" }),
        ("LPN", "Las Piñas", LasPinasBarangays.Select(x => x.Name).ToArray()),
        ("MUN", "Muntinlupa", new[] { "Alabang", "Putatan", "Tunasan", "Cupang", "Ayala Alabang" }),
        ("CLN", "Caloocan", new[] { "Bagong Barrio", "Grace Park", "Camarin", "Deparo", "177" }),
        ("MLB", "Malabon", new[] { "Longos", "Potrero", "Tañong", "Acacia", "Tonsuya" }),
        ("NVT", "Navotas", new[] { "Sipac-Almacen", "Tanza", "NBBS Proper", "San Jose", "Tangos" }),
        ("VLZ", "Valenzuela", new[] { "Gen. T. de Leon", "Malinta", "Marulas", "Karuhatan", "Canumay West" }),
        ("SJN", "San Juan", new[] { "Greenhills", "West Crame", "Balong-Bato", "Isabelita", "Onse" }),
        ("PTR", "Pateros", new[] { "San Roque", "Aguho", "Martires Del 96", "Santo Rosario-Kanluran", "Santo Rosario-Silangan" }),
    };

    public static async Task SeedAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.AddressRegions.AnyAsync(cancellationToken))
        {
            await BackfillZipCodesAsync(db, cancellationToken);
            await SyncLasPinasBarangaysAsync(db, cancellationToken);
            return;
        }

        var ncr = new AddressRegion { Code = "NCR", Name = "National Capital Region (NCR)" };
        var metroManila = new AddressProvince { Code = "MM", Name = "Metro Manila", Region = ncr };

        foreach (var (code, name, barangays) in Cities)
        {
            var city = new AddressCity { Code = code, Name = name, Province = metroManila };
            foreach (var barangayName in barangays)
            {
                city.Barangays.Add(new AddressBarangay
                {
                    Code = Slugify(code, barangayName),
                    Name = barangayName,
                    ZipCode = ResolveZipCode(code, barangayName),
                });
            }

            metroManila.Cities.Add(city);
        }

        ncr.Provinces.Add(metroManila);
        db.AddressRegions.Add(ncr);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SyncLasPinasBarangaysAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var city = await db.AddressCities
            .Include(x => x.Barangays)
            .FirstOrDefaultAsync(x => x.Code == "LPN", cancellationToken);

        if (city is null)
        {
            return;
        }

        var existingByName = city.Barangays.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var (name, zipCode) in LasPinasBarangays)
        {
            if (existingByName.TryGetValue(name, out var existing))
            {
                if (string.IsNullOrEmpty(existing.ZipCode) || existing.ZipCode == "1000")
                {
                    existing.ZipCode = zipCode;
                    changed = true;
                }

                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    changed = true;
                }

                continue;
            }

            city.Barangays.Add(new AddressBarangay
            {
                Code = Slugify("LPN", name),
                Name = name,
                ZipCode = zipCode,
            });
            changed = true;
        }

        foreach (var legacyName in LegacyLasPinasBarangayNames)
        {
            if (existingByName.TryGetValue(legacyName, out var legacyBarangay) && legacyBarangay.IsActive)
            {
                legacyBarangay.IsActive = false;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task BackfillZipCodesAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var barangays = await db.AddressBarangays
            .Include(x => x.City)
            .Where(x => string.IsNullOrEmpty(x.ZipCode))
            .ToListAsync(cancellationToken);

        if (barangays.Count == 0)
        {
            return;
        }

        foreach (var barangay in barangays)
        {
            barangay.ZipCode = ResolveZipCode(barangay.City.Code, barangay.Name);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string ResolveZipCode(string cityCode, string barangayName)
    {
        if (cityCode == "LPN")
        {
            foreach (var (name, zipCode) in LasPinasBarangays)
            {
                if (string.Equals(name, barangayName, StringComparison.OrdinalIgnoreCase))
                {
                    return zipCode;
                }
            }
        }

        if (ZipCodesByCity.TryGetValue(cityCode, out var barangayZipCodes)
            && barangayZipCodes.TryGetValue(barangayName, out var mappedZipCode))
        {
            return mappedZipCode;
        }

        return "1000";
    }

    private static string Slugify(string cityCode, string barangayName)
    {
        var slug = barangayName.ToUpperInvariant()
            .Replace(" ", "_")
            .Replace(".", "")
            .Replace("-", "_")
            .Replace("Ñ", "N");
        return $"{cityCode}_{slug}"[..Math.Min(20, $"{cityCode}_{slug}".Length)];
    }
}
