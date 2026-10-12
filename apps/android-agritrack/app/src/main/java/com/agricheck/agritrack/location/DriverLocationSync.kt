package com.agricheck.agritrack.location

import android.content.Context
import android.location.Location
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.data.local.PendingDriverLocation
import com.agricheck.agritrack.util.GeoUtils
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

class DriverLocationSync(private val context: Context) {
    private val app = context.applicationContext as AgriTrackApp
    private val queue = app.container.driverLocationQueueStore
    private val uploadGate = Mutex()
    private var lastServerUploadAtMs: Long = 0L
    private var lastServerLat: Double? = null
    private var lastServerLng: Double? = null
    private var lastLocalLogAtMs: Long = 0L
    private var lastLocalLat: Double? = null
    private var lastLocalLng: Double? = null

    suspend fun onLocationSample(containerUuid: String, location: Location) {
        val now = System.currentTimeMillis()
        val lat = location.latitude
        val lng = location.longitude

        val movedForLocal = lastLocalLat == null || lastLocalLng == null ||
            GeoUtils.distanceMeters(lastLocalLat!!, lastLocalLng!!, lat, lng) >= DriverTrackingPolicy.LOCAL_LOG_MIN_MOVE_METERS
        val localDue = now - lastLocalLogAtMs >= DriverTrackingPolicy.LOCAL_LOG_MIN_INTERVAL_MS
        if (movedForLocal || localDue) {
            queue.enqueue(
                PendingDriverLocation(
                    containerUuid = containerUuid,
                    latitude = lat,
                    longitude = lng,
                    recordedAtEpochMs = now,
                ),
            )
            lastLocalLogAtMs = now
            lastLocalLat = lat
            lastLocalLng = lng
        }

        val movedForServer = lastServerLat == null || lastServerLng == null ||
            GeoUtils.distanceMeters(lastServerLat!!, lastServerLng!!, lat, lng) >= DriverTrackingPolicy.SERVER_UPLOAD_MIN_MOVE_METERS
        val serverDue = now - lastServerUploadAtMs >= DriverTrackingPolicy.SERVER_UPLOAD_INTERVAL_MS
        if (movedForServer || serverDue) {
            flushPendingUploads()
        }
    }

    suspend fun flushPendingUploads(): Int = uploadGate.withLock {
        if (!context.hasInternetConnectivity()) return 0
        val pending = queue.snapshot()
        if (pending.isEmpty()) return 0
        val synced = mutableListOf<PendingDriverLocation>()
        for (point in pending) {
            val ok = runCatching {
                app.container.driverRepository.recordLocation(
                    point.containerUuid,
                    point.latitude,
                    point.longitude,
                )
            }.isSuccess
            if (!ok) break
            synced.add(point)
            lastServerUploadAtMs = System.currentTimeMillis()
            lastServerLat = point.latitude
            lastServerLng = point.longitude
        }
        queue.removeSynced(synced)
        synced.size
    }
}
