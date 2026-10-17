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

/** Leg 2: driver confirms physical arrival at warehouse (trip ends when NMIS/Doctor scans delivery QR). */
fun canConfirmWarehouseArrival(phase: DeliveryPhase) =
    phase == DeliveryPhase.InTransit || phase == DeliveryPhase.PickedUp

/** Transit navigation and GPS trail end after doctor handoff scan (UnderInspection) or final release. */
fun isDriverTransitEnded(phase: DeliveryPhase?) =
    phase == DeliveryPhase.UnderInspection || phase == DeliveryPhase.Completed

fun canSubmitEmptyContainerProof(phase: DeliveryPhase?) =
    phase == DeliveryPhase.Completed

/** Max distance from pickup / warehouse to enable confirm actions (matches API `DeliveryStopGeofenceMeters`). */
const val DELIVERY_STOP_GEOFENCE_METERS = 100.0

/** Show "Arrived at warehouse" only when driver is this close (button enables at [DELIVERY_STOP_GEOFENCE_METERS]). */
const val ARRIVE_BUTTON_APPROACH_METERS = 200.0

fun isWithinDeliveryStopGeofence(distanceMeters: Double?): Boolean =
    distanceMeters != null && distanceMeters <= DELIVERY_STOP_GEOFENCE_METERS

fun isWithinArriveButtonApproach(distanceMeters: Double?): Boolean =
    distanceMeters != null && distanceMeters <= ARRIVE_BUTTON_APPROACH_METERS

/** Leg 2: warehouse navigation may start only after a verified pickup at the planned stop. */
fun requiresPickupGeofenceToStartWarehouseNavigation(phase: DeliveryPhase): Boolean =
    phase == DeliveryPhase.PickedUp

/** QR is for warehouse handoff — only after driver confirms arrival at the assigned warehouse. */
fun canShowDeliveryQr(phase: DeliveryPhase, handoffQrAvailable: Boolean = false) =
    !isDriverTransitEnded(phase) &&
        (handoffQrAvailable || phase == DeliveryPhase.AtWarehouse)

fun isDeliveryTripComplete(phase: DeliveryPhase?) = phase == DeliveryPhase.Completed

fun warehouseArrivalConfirmed(track: com.agricheck.agritrack.data.model.ContainerTrackDto?): Boolean {
    val leg = track?.deliverySummary?.firstOrNull { it.leg == "warehouse" } ?: return false
    if (!leg.arrivedAtUtc.isNullOrBlank()) return true
    val label = leg.statusLabel.orEmpty()
    return label.contains("arrived", ignoreCase = true) ||
        label.contains("present delivery", ignoreCase = true)
}

fun com.agricheck.agritrack.data.model.ContainerTrackDto.applyContainerListUpdate(
    container: com.agricheck.agritrack.data.model.ContainerListItemDto,
): com.agricheck.agritrack.data.model.ContainerTrackDto = copy(
    status = container.status,
    handoffQrAvailable = handoffQrAvailable || container.handoffQrAvailable,
    deliveryComplianceHoldActive = container.deliveryComplianceHoldActive,
    deliveryComplianceHoldReason = container.deliveryComplianceHoldReason,
)

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
