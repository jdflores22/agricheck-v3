package com.agricheck.agritrack.util

import com.agricheck.agritrack.data.model.ContainerTrackDestinationDto
import com.agricheck.agritrack.data.model.ContainerTrackDto

enum class TripNavLeg {
    /** Driver heading to container pickup point */
    ToPickup,
    /** Driver heading to warehouse / destination */
    ToDestination,
}

fun tripNavLeg(phase: DeliveryPhase): TripNavLeg = when (phase) {
    DeliveryPhase.Assigned,
    DeliveryPhase.AwaitingPickup,
    -> TripNavLeg.ToPickup
    else -> TripNavLeg.ToDestination
}

fun isNavigatingToPickup(phase: DeliveryPhase): Boolean = tripNavLeg(phase) == TripNavLeg.ToPickup

/** Strong route: driver GPS → pickup (leg 1) or driver → warehouse (leg 2). */
fun ContainerTrackDto.primaryRouteTarget(): ContainerTrackDestinationDto {
    val phase = deliveryPhase(status)
    if (canConfirmPickup(phase)) return pickup
    return if (tripNavLeg(phase) == TripNavLeg.ToPickup) pickup else destination
}

/** Light route pickup → warehouse; shown until leg 2 (warehouse) navigation. */
fun shouldShowPickupToWarehousePreview(phase: DeliveryPhase): Boolean =
    tripNavLeg(phase) == TripNavLeg.ToPickup
