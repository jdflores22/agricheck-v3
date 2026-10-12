package com.agricheck.agritrack.location

/**
 * Driver trail: smooth map UI (MapScreen) + durable local log + server sync every few minutes.
 */
object DriverTrackingPolicy {
    /** Upload to API when this long since last successful sync (3–5 min target). */
    const val SERVER_UPLOAD_INTERVAL_MS = 4 * 60 * 1000L

    /** Also upload if the driver moved at least this far since last server point. */
    const val SERVER_UPLOAD_MIN_MOVE_METERS = 80.0

    /** Foreground service GPS while navigating. */
    const val SERVICE_INTERVAL_NAV_MS = 5_000L

    const val SERVICE_MIN_DISTANCE_NAV_METERS = 5f

    /** Foreground service GPS while assigned but not in turn-by-turn nav. */
    const val SERVICE_INTERVAL_IDLE_MS = 30_000L

    const val SERVICE_MIN_DISTANCE_IDLE_METERS = 25f

    /** Persist locally at least this often while moving (offline trail). */
    const val LOCAL_LOG_MIN_INTERVAL_MS = 30_000L

    const val LOCAL_LOG_MIN_MOVE_METERS = 15.0
}
