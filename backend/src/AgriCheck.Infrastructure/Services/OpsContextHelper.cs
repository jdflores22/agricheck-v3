using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;

namespace AgriCheck.Infrastructure.Services;

internal static class OpsContextHelper
{
    public static async Task<User> RequireWarehouseStaffAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
        if (!currentUser.IsInRole("ROLE_WAREHOUSE_STAFF") && !currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Warehouse staff access required.");
        }

        return user;
    }

    public static async Task<User> RequireDriverAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
        if (!currentUser.IsInRole("ROLE_DRIVER") && !currentUser.IsInRole("ROLE_OPERATOR") && !currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Driver access required.");
        }

        return user;
    }

    public static async Task<User> RequireMobileUserAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        return await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
    }
}

internal static class OpsDtoMapper
{
    public static ContainerListItemDto MapContainer(Container c) => MapContainer(c, includeAssignment: false);

    public static ContainerListItemDto MapContainer(Container c, bool includeAssignment)
    {
        var last = c.Locations.OrderByDescending(l => l.RecordedAt).FirstOrDefault();
        Guid? driverUuid = null;
        string? driverName = null;
        string? vehiclePlate = null;

        if (includeAssignment)
        {
            if (c.AssignedDriver is not null)
            {
                driverUuid = c.AssignedDriver.Uuid;
                driverName = FormatUserName(c.AssignedDriver.Profile, c.AssignedDriver.Email);
            }

            vehiclePlate = c.AssignedOperatorVehicle?.PlateNumber;
        }

        return new ContainerListItemDto(
            c.Uuid,
            c.ContainerNumber,
            c.Entry.Uuid,
            c.Entry.ReferenceNo,
            c.Status.ToString(),
            c.DepartureTime,
            c.ArrivalTime,
            last?.Latitude,
            last?.Longitude,
            last?.RecordedAt,
            driverUuid,
            driverName,
            vehiclePlate);
    }

    public static string FormatUserName(UserProfile? profile, string email)
    {
        if (profile is null)
        {
            return email;
        }

        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? email : name;
    }
}
