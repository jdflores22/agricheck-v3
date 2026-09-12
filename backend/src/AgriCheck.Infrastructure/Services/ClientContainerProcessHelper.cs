using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Services;

internal static class ClientContainerProcessHelper
{
    private static readonly ContainerStatus[] StatusOrder =
    {
        ContainerStatus.Pending,
        ContainerStatus.ReadyForTransport,
        ContainerStatus.AwaitingConfirmation,
        ContainerStatus.Assigned,
        ContainerStatus.InTransit,
        ContainerStatus.UnderInspection,
        ContainerStatus.Inspected,
        ContainerStatus.AtWarehouse,
        ContainerStatus.Released
    };

    public static (bool CanBookWarehouse, string? WarehouseBookingBlockedReason) EvaluateBookingEligibility(
        Container container,
        Entry entry)
    {
        if (entry.Status == EntryStatus.Draft)
        {
            return (false, "Submit your entry before booking warehouse storage for this container.");
        }

        return container.Status switch
        {
            ContainerStatus.AtWarehouse or ContainerStatus.Released => (true, null),
            _ => (false, "Warehouse booking opens once the container is received and processed at the warehouse.")
        };
    }

    public static IReadOnlyList<ClientContainerProcessStepDto> BuildProcessSteps(Container container)
    {
        var definitions = new (string Key, string Label, string Description, DateTime? CompletedAt)[]
        {
            ("Registered", "Registered", "Container recorded on your entry submission.", container.CreatedAt),
            ("ReadyForTransport", "Ready for transport", "Inspection photos approved; awaiting transport tagging.", null),
            ("AwaitingConfirmation", "Awaiting confirmation", "Tagged for transport; waiting for operator claim.", null),
            ("Assigned", "Assigned", "Operator claimed and driver assigned.", container.Status >= ContainerStatus.Assigned ? container.UpdatedAt : null),
            ("InTransit", "In transit", "Container en route to the warehouse.", container.DepartureTime),
            ("UnderInspection", "Under inspection", "Doctor inspection in progress at warehouse.", null),
            ("Inspected", "Inspected", "Doctor approved; ready for warehouse receive.", null),
            ("AtWarehouse", "At warehouse", "Received into warehouse inventory.", container.ArrivalTime),
            ("Released", "Released", "Cleared for release from warehouse storage.", container.Status == ContainerStatus.Released ? container.UpdatedAt : null)
        };

        var currentIndex = Array.IndexOf(StatusOrder, container.Status);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var steps = new List<ClientContainerProcessStepDto>(definitions.Length);
        for (var i = 0; i < definitions.Length; i++)
        {
            var (key, label, description, completedAt) = definitions[i];
            var state = i < currentIndex ? "complete" : i == currentIndex ? "active" : "pending";
            steps.Add(new ClientContainerProcessStepDto(key, label, description, state, completedAt));
        }

        return steps;
    }

    public static ClientContainerWarehouseInfoDto? MapWarehouseInfo(Container container)
    {
        var inventory = container.Inventories
            .OrderByDescending(i => i.ReceivedAt)
            .FirstOrDefault();

        if (inventory is null)
        {
            return null;
        }

        return new ClientContainerWarehouseInfoDto(
            inventory.WarehouseFacility?.Name,
            inventory.LocationCode,
            inventory.ReceivedAt,
            inventory.Status.ToString());
    }
}
