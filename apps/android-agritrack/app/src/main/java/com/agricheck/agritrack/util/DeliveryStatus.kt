package com.agricheck.agritrack.util

import com.agricheck.agritrack.data.model.ContainerListItemDto

enum class DeliveryPhase {
    Assigned,
    AwaitingPickup,
    PickedUp,
    InTransit,
    AtWarehouse,
    UnderInspection,
    Completed,
}

fun deliveryPhase(status: String): DeliveryPhase = when (status) {
    "PickedUp" -> DeliveryPhase.PickedUp
    "InTransit" -> DeliveryPhase.InTransit
    "AtWarehouse" -> DeliveryPhase.AtWarehouse
    "UnderInspection" -> DeliveryPhase.UnderInspection
    "Inspected", "Released" -> DeliveryPhase.Completed
    "Assigned", "ReadyForTransport", "AwaitingConfirmation" -> DeliveryPhase.AwaitingPickup
    else -> DeliveryPhase.Assigned
}

fun deliveryPhaseLabel(phase: DeliveryPhase): String =
    com.agricheck.agritrack.ui.strings.UiStrings.deliveryPhaseLabel(phase)

fun canConfirmPickup(phase: DeliveryPhase) =
    phase == DeliveryPhase.AwaitingPickup || phase == DeliveryPhase.Assigned

fun canStartTransit(phase: DeliveryPhase) = phase == DeliveryPhase.PickedUp

/** Leg 2: driver confirms physical arrival at warehouse (trip ends only when NMIS/Doctor scans QR). */
fun canConfirmWarehouseArrival(phase: DeliveryPhase) =
    phase == DeliveryPhase.InTransit || phase == DeliveryPhase.PickedUp

const val DELIVERY_STOP_GEOFENCE_METERS = 500.0

fun canShowDeliveryQr(phase: DeliveryPhase) =
    phase == DeliveryPhase.PickedUp ||
        phase == DeliveryPhase.InTransit ||
        phase == DeliveryPhase.AtWarehouse ||
        phase == DeliveryPhase.UnderInspection

fun pickActiveDelivery(containers: List<ContainerListItemDto>): ContainerListItemDto? {
    containers.firstOrNull { deliveryPhase(it.status) == DeliveryPhase.InTransit }?.let { return it }
    containers.firstOrNull { deliveryPhase(it.status) == DeliveryPhase.PickedUp }?.let { return it }
    containers.firstOrNull {
        val p = deliveryPhase(it.status)
        p == DeliveryPhase.UnderInspection || p == DeliveryPhase.AtWarehouse
    }?.let { return it }
    return containers.firstOrNull {
        val p = deliveryPhase(it.status)
        p == DeliveryPhase.AwaitingPickup || p == DeliveryPhase.Assigned
    }
}
