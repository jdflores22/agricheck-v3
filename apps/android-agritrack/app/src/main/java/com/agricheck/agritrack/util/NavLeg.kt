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

/** Light route pickup → warehouse — overview only (hidden during turn-by-turn navigation). */
fun shouldShowPickupToWarehousePreview(phase: DeliveryPhase, drivingMode: Boolean): Boolean =
    !drivingMode && tripNavLeg(phase) == TripNavLeg.ToPickup

/** Device GPS when available; otherwise server last known (overview map only — not for geofence while navigating). */
fun resolveDriverLatLng(
    deviceLat: Double?,
    deviceLng: Double?,
    track: ContainerTrackDto?,
    drivingMode: Boolean = false,
): Pair<Double?, Double?> {
    if (deviceLat != null && deviceLng != null) return deviceLat to deviceLng
    if (drivingMode) return null to null
    val lat = track?.currentLatitude
    val lng = track?.currentLongitude
    return lat to lng
}
