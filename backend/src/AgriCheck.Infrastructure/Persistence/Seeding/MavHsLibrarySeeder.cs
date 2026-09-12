using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

internal sealed record MavHsLibrarySeedDetail(string Description, string? Notes = null);

internal sealed record MavHsLibrarySeedHeading(
    string HeadingNumber,
    string Description,
    IReadOnlyList<MavHsLibrarySeedDetail> Details,
    string? Notes = null);

internal sealed record MavHsLibrarySeedCategory(
    string HsCode,
    string Description,
    string AgencyCode,
    string? Notes,
    IReadOnlyList<MavHsLibrarySeedHeading> Headings);

/// <summary>
/// Philippine DA MAV HS library — official WTO AoA TRQ products (BAI/BPI) plus BFAR fishery entries
/// commonly used alongside SPS import clearance. Sources: WTO ILC PH11, RA 8178, DA AC 06 s2022.
/// </summary>
internal static class MavHsLibrarySeeder
{
    public static async Task SeedAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var agencies = await db.Agencies.ToDictionaryAsync(a => a.Code, a => a.Id, cancellationToken);
        var createdCategories = 0;
        var createdHeadings = 0;
        var createdDetails = 0;
        var updatedCategories = 0;

        foreach (var definition in GetDefinitions())
        {
            if (!agencies.TryGetValue(definition.AgencyCode, out var agencyId))
            {
                logger.LogWarning("Skipping MAV HS category {HsCode}; agency {Agency} not found.", definition.HsCode, definition.AgencyCode);
                continue;
            }

            var category = await db.MavHsCategories
                .Include(c => c.Headings)
                .ThenInclude(h => h.Details)
                .FirstOrDefaultAsync(c => c.HsCode == definition.HsCode, cancellationToken);

            if (category is null)
            {
                category = BuildCategory(definition, agencyId);
                db.MavHsCategories.Add(category);
                createdCategories++;
                createdHeadings += definition.Headings.Count;
                createdDetails += definition.Headings.Sum(h => h.Details.Count);
                continue;
            }

            if (category.AgencyId != agencyId || category.Notes != definition.Notes)
            {
                category.AgencyId = agencyId;
                if (string.IsNullOrWhiteSpace(category.Notes))
                {
                    category.Notes = definition.Notes;
                }

                updatedCategories++;
            }

            foreach (var headingDef in definition.Headings)
            {
                var heading = category.Headings.FirstOrDefault(h => h.HeadingNumber == headingDef.HeadingNumber);
                if (heading is null)
                {
                    category.Headings.Add(BuildHeading(headingDef));
                    createdHeadings++;
                    createdDetails += headingDef.Details.Count;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(heading.Notes))
                {
                    heading.Notes = headingDef.Notes;
                }

                foreach (var detailDef in headingDef.Details)
                {
                    if (heading.Details.Any(d => d.Description == detailDef.Description))
                    {
                        continue;
                    }

                    heading.Details.Add(BuildDetail(detailDef));
                    createdDetails++;
                }
            }
        }

        if (createdCategories + createdHeadings + createdDetails + updatedCategories > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "MAV HS library sync: {Categories} new categories, {Headings} new headings, {Details} new commodities, {Updated} categories updated.",
            createdCategories,
            createdHeadings,
            createdDetails,
            updatedCategories);
    }

    private static MavHsCategory BuildCategory(MavHsLibrarySeedCategory definition, long agencyId)
    {
        var category = new MavHsCategory
        {
            Uuid = Guid.NewGuid(),
            HsCode = definition.HsCode,
            Description = definition.Description,
            Notes = definition.Notes,
            AgencyId = agencyId
        };

        foreach (var heading in definition.Headings)
        {
            category.Headings.Add(BuildHeading(heading));
        }

        return category;
    }

    private static MavHsHeading BuildHeading(MavHsLibrarySeedHeading definition)
    {
        var heading = new MavHsHeading
        {
            Uuid = Guid.NewGuid(),
            HeadingNumber = definition.HeadingNumber,
            Description = definition.Description,
            Notes = definition.Notes
        };

        foreach (var detail in definition.Details)
        {
            heading.Details.Add(BuildDetail(detail));
        }

        return heading;
    }

    private static MavHsDetail BuildDetail(MavHsLibrarySeedDetail definition) =>
        new()
        {
            Uuid = Guid.NewGuid(),
            Description = definition.Description,
            Notes = definition.Notes
        };

    private static IReadOnlyList<MavHsLibrarySeedCategory> GetDefinitions() =>
        MavHsLibraryCatalog.Build();
}
