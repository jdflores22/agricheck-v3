package com.agricheck.agritrack.util

import com.agricheck.agritrack.BuildConfig
import com.agricheck.agritrack.data.model.ContainerTrackDestinationDto
import com.agricheck.agritrack.data.model.ContainerTrackDto

/**
 * Sample stops + driver start for map/navigation testing (`USE_DEMO_MAP_STOPS` in local.properties).
 * Resets server trail / last GPS (often stuck at warehouse) for local leg-1 testing.
 */
object DemoMapStops {
    const val PICKUP_LAT = 14.452722
    const val PICKUP_LNG = 120.993092
    const val DESTINATION_LAT = 14.459148
    const val DESTINATION_LNG = 120.987489

    /** ~400 m from demo pickup — simulates driver before first leg. */
    const val DRIVER_START_LAT = 14.451200
    const val DRIVER_START_LNG = 120.995800

    /** Local-only status reset so routing targets pickup first (server unchanged). */
    private const val DEMO_LEG1_STATUS = "AwaitingConfirmation"

    fun isEnabled(): Boolean = BuildConfig.USE_DEMO_MAP_STOPS

    fun driverStart(): Pair<Double, Double> = DRIVER_START_LAT to DRIVER_START_LNG

    /**
     * Demo mode: fixed start near pickup (ignores phone/emulator GPS stuck at warehouse).
     * Otherwise: device GPS, then server last known.
     */
    fun resolveDriverLatLng(
        deviceLat: Double?,
        deviceLng: Double?,
        track: ContainerTrackDto?,
    ): Pair<Double?, Double?> {
        if (!isEnabled()) {
            val lat = deviceLat ?: track?.currentLatitude
            val lng = deviceLng ?: track?.currentLongitude
            return lat to lng
        }
        return driverStart()
    }

    fun applyIfEnabled(track: ContainerTrackDto): ContainerTrackDto {
        if (!isEnabled()) return track
        return track.copy(
            status = DEMO_LEG1_STATUS,
            currentLatitude = DRIVER_START_LAT,
            currentLongitude = DRIVER_START_LNG,
            trail = emptyList(),
            pickup = ContainerTrackDestinationDto(
                name = track.pickup.name.ifBlank { "Demo pickup" },
                latitude = PICKUP_LAT,
                longitude = PICKUP_LNG,
            ),
            destination = ContainerTrackDestinationDto(
                name = track.destination.name.ifBlank { "Demo warehouse" },
                latitude = DESTINATION_LAT,
                longitude = DESTINATION_LNG,
            ),
        )
    }
}

/** Active navigation target for the current delivery phase (pickup vs warehouse). */
fun ContainerTrackDto.navigationStop(): ContainerTrackDestinationDto = primaryRouteTarget()
