package com.agricheck.agritrack.location

import android.content.Context
import android.location.Location
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.data.local.PendingDriverLocation
import com.agricheck.agritrack.data.model.RecordContainerLocationPointRequest
import com.agricheck.agritrack.util.GeoUtils
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import java.time.Instant
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

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

    /**
     * Persists GPS on device first (works with **no internet**). Upload runs when online.
     * File: `files/pending_driver_locations.json`
     */
    suspend fun onLocationSample(containerUuid: String, location: Location) {
        val now = System.currentTimeMillis()
        val lat = location.latitude
        val lng = location.longitude
        val offline = !context.hasInternetConnectivity()

        val localInterval = if (offline) {
            DriverTrackingPolicy.OFFLINE_LOCAL_LOG_MIN_INTERVAL_MS
        } else {
            DriverTrackingPolicy.LOCAL_LOG_MIN_INTERVAL_MS
        }
        val localMove = if (offline) {
            DriverTrackingPolicy.OFFLINE_LOCAL_LOG_MIN_MOVE_METERS
        } else {
            DriverTrackingPolicy.LOCAL_LOG_MIN_MOVE_METERS
        }

        val movedForLocal = lastLocalLat == null || lastLocalLng == null ||
            GeoUtils.distanceMeters(lastLocalLat!!, lastLocalLng!!, lat, lng) >= localMove
        val localDue = now - lastLocalLogAtMs >= localInterval
        if (movedForLocal || localDue) {
            saveCoordinateOnDevice(containerUuid, lat, lng, now, offline)
            lastLocalLogAtMs = now
            lastLocalLat = lat
            lastLocalLng = lng
        }

        val movedForServer = lastServerLat == null || lastServerLng == null ||
            GeoUtils.distanceMeters(lastServerLat!!, lastServerLng!!, lat, lng) >= DriverTrackingPolicy.SERVER_UPLOAD_MIN_MOVE_METERS
        val serverDue = now - lastServerUploadAtMs >= DriverTrackingPolicy.SERVER_UPLOAD_INTERVAL_MS
        if (movedForServer || serverDue || offline) {
            if (!offline) {
                flushPendingUploads()
            }
        }
    }

    private suspend fun saveCoordinateOnDevice(
        containerUuid: String,
        latitude: Double,
        longitude: Double,
        recordedAtEpochMs: Long,
        capturedOffline: Boolean,
    ) {
        queue.enqueue(
            PendingDriverLocation(
                containerUuid = containerUuid,
                latitude = latitude,
                longitude = longitude,
                recordedAtEpochMs = recordedAtEpochMs,
                capturedOffline = capturedOffline,
            ),
        )
    }

    suspend fun flushPendingUploads(): Int = uploadGate.withLock {
        if (!context.hasInternetConnectivity()) return 0
        val pending = queue.snapshot()
        if (pending.isEmpty()) return 0

        val synced = mutableListOf<PendingDriverLocation>()
        val byContainer = pending.groupBy { it.containerUuid }
        for ((uuid, points) in byContainer) {
            val chunks = points.chunked(BATCH_SIZE)
            for (chunk in chunks) {
                val batchOk = runCatching {
                    app.container.driverRepository.recordLocationsBatch(
                        uuid,
                        chunk.map { point ->
                            RecordContainerLocationPointRequest(
                                latitude = point.latitude,
                                longitude = point.longitude,
                                recordedAt = epochToIso(point.recordedAtEpochMs),
                                capturedOffline = point.capturedOffline,
                            )
                        },
                    )
                }.isSuccess
                if (!batchOk) {
                    val singleOk = uploadChunkOneByOne(uuid, chunk)
                    if (!singleOk) break
                    synced.addAll(chunk)
                } else {
                    synced.addAll(chunk)
                }
                val last = chunk.last()
                lastServerUploadAtMs = System.currentTimeMillis()
                lastServerLat = last.latitude
                lastServerLng = last.longitude
            }
        }
        queue.removeSynced(synced)
        synced.size
    }

    private suspend fun uploadChunkOneByOne(containerUuid: String, chunk: List<PendingDriverLocation>): Boolean {
        for (point in chunk) {
            val ok = runCatching {
                app.container.driverRepository.recordLocation(
                    containerUuid,
                    point.latitude,
                    point.longitude,
                    epochToIso(point.recordedAtEpochMs),
                )
            }.isSuccess
            if (!ok) return false
        }
        return true
    }

    private fun epochToIso(epochMs: Long): String =
        DateTimeFormatter.ISO_INSTANT.format(Instant.ofEpochMilli(epochMs).atOffset(ZoneOffset.UTC))

    private companion object {
        const val BATCH_SIZE = 40
    }
}
