package com.agricheck.agritrack.data.api

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

@Serializable
private data class OsrmResponse(
    val routes: List<OsrmRoute>? = null,
)

@Serializable
private data class OsrmRoute(
    val distance: Double = 0.0,
    val duration: Double = 0.0,
    val geometry: OsrmGeometry? = null,
)

@Serializable
private data class OsrmGeometry(
    val coordinates: List<List<Double>>? = null,
)

data class OsrmRouteResult(
    val points: List<Pair<Double, Double>>,
    val distanceMeters: Double,
    val durationSeconds: Double,
)

object OsrmClient {
    private val json = Json { ignoreUnknownKeys = true }
    private val client = OkHttpClient.Builder()
        .connectTimeout(20, TimeUnit.SECONDS)
        .readTimeout(20, TimeUnit.SECONDS)
        .build()

    suspend fun fetchDrivingRoute(
        fromLat: Double,
        fromLng: Double,
        toLat: Double,
        toLng: Double,
    ): OsrmRouteResult? {
        val url =
            "https://router.project-osrm.org/route/v1/driving/$fromLng,$fromLat;$toLng,$toLat" +
                "?overview=full&geometries=geojson"
        val request = Request.Builder().url(url).get().build()
        val body = client.newCall(request).execute().body?.string() ?: return null
        val payload = json.decodeFromString<OsrmResponse>(body)
        val route = payload.routes?.firstOrNull() ?: return null
        val coords = route.geometry?.coordinates ?: return null
        val points = coords.mapNotNull { pair ->
            if (pair.size >= 2) pair[1] to pair[0] else null
        }
        if (points.isEmpty()) return null
        return OsrmRouteResult(points, route.distance, route.duration)
    }

    fun formatDistance(meters: Double): String =
        if (meters >= 1000) "${"%.1f".format(meters / 1000)} km" else "${meters.toInt()} m"

    fun formatDuration(seconds: Double): String {
        val minutes = (seconds / 60).toInt()
        return if (minutes < 60) "$minutes min" else "${minutes / 60}h ${minutes % 60}m"
    }
}
