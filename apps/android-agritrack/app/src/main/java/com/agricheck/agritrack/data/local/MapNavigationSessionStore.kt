package com.agricheck.agritrack.data.local

/**
 * In-memory navigation UI state so leaving Map (QR, back, tabs) does not reset driving mode.
 */
data class MapNavigationSession(
    val containerUuid: String,
    val drivingMode: Boolean,
    val northUp: Boolean,
    val voiceEnabled: Boolean,
)

class MapNavigationSessionStore {
    @Volatile
    private var current: MapNavigationSession? = null

    fun get(containerUuid: String): MapNavigationSession? =
        current?.takeIf { it.containerUuid == containerUuid }

    fun isActiveDriving(containerUuid: String): Boolean =
        get(containerUuid)?.drivingMode == true

    fun update(
        containerUuid: String,
        drivingMode: Boolean,
        northUp: Boolean,
        voiceEnabled: Boolean,
    ) {
        if (drivingMode) {
            current = MapNavigationSession(containerUuid, true, northUp, voiceEnabled)
        } else if (current?.containerUuid == containerUuid) {
            current = null
        }
    }

    fun clear(containerUuid: String) {
        if (current?.containerUuid == containerUuid) {
            current = null
        }
    }
}
