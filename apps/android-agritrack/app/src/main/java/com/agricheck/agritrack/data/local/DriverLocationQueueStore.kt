package com.agricheck.agritrack.data.local

import android.content.Context
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import java.io.File

@Serializable
data class PendingDriverLocation(
    val containerUuid: String,
    val latitude: Double,
    val longitude: Double,
    val recordedAtEpochMs: Long,
)

@Serializable
private data class PendingDriverLocationFile(val items: List<PendingDriverLocation> = emptyList())

class DriverLocationQueueStore(context: Context) {
    private val file = File(context.filesDir, "pending_driver_locations.json")
    private val json = Json { ignoreUnknownKeys = true }
    private val mutex = Mutex()

    suspend fun enqueue(point: PendingDriverLocation) = mutex.withLock {
        val list = readAll().toMutableList()
        val last = list.lastOrNull()
        if (last != null &&
            last.containerUuid == point.containerUuid &&
            last.latitude == point.latitude &&
            last.longitude == point.longitude
        ) {
            return
        }
        list.add(point)
        writeAll(list)
    }

    suspend fun snapshot(): List<PendingDriverLocation> = mutex.withLock { readAll() }

    suspend fun removeSynced(synced: List<PendingDriverLocation>) = mutex.withLock {
        if (synced.isEmpty()) return
        val syncedSet = synced.toSet()
        val remaining = readAll().filterNot { syncedSet.contains(it) }
        writeAll(remaining)
    }

    suspend fun pendingCount(): Int = mutex.withLock { readAll().size }

    private fun readAll(): List<PendingDriverLocation> {
        if (!file.exists()) return emptyList()
        return runCatching {
            json.decodeFromString<PendingDriverLocationFile>(file.readText()).items
        }.getOrElse { emptyList() }
    }

    private fun writeAll(list: List<PendingDriverLocation>) {
        if (list.isEmpty()) {
            file.delete()
            return
        }
        file.writeText(json.encodeToString(PendingDriverLocationFile(list)))
    }
}
