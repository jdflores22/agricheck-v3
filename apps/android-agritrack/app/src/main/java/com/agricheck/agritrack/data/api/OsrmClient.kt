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
    val legs: List<OsrmLeg>? = null,
)

@Serializable
private data class OsrmLeg(
    val steps: List<OsrmStep>? = null,
)

@Serializable
private data class OsrmStep(
    val name: String? = null,
    val distance: Double = 0.0,
    val duration: Double = 0.0,
    val maneuver: OsrmManeuver? = null,
)

@Serializable
private data class OsrmManeuver(
    val type: String? = null,
    val modifier: String? = null,
    val location: List<Double>? = null,
)

@Serializable
private data class OsrmGeometry(
    val coordinates: List<List<Double>>? = null,
)

data class DrivingRouteStep(
    val streetName: String?,
    val distanceMeters: Double,
    val durationSeconds: Double,
    val maneuverType: String?,
    val maneuverModifier: String?,
    val maneuverLatitude: Double?,
    val maneuverLongitude: Double?,
)

data class OsrmRouteResult(
    val points: List<Pair<Double, Double>>,
    val distanceMeters: Double,
    val durationSeconds: Double,
    val steps: List<DrivingRouteStep> = emptyList(),
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
                "?overview=full&geometries=geojson&steps=true"
        val request = Request.Builder().url(url).get().build()
        val body = client.newCall(request).execute().body?.string() ?: return null
        val payload = json.decodeFromString<OsrmResponse>(body)
        val route = payload.routes?.firstOrNull() ?: return null
        val coords = route.geometry?.coordinates ?: return null
        val points = coords.mapNotNull { pair ->
            if (pair.size >= 2) pair[1] to pair[0] else null
        }
        if (points.isEmpty()) return null

        val steps = route.legs
            ?.flatMap { it.steps ?: emptyList() }
            ?.map { step ->
                val loc = step.maneuver?.location
                DrivingRouteStep(
                    streetName = step.name,
                    distanceMeters = step.distance,
                    durationSeconds = step.duration,
                    maneuverType = step.maneuver?.type,
                    maneuverModifier = step.maneuver?.modifier,
                    maneuverLatitude = loc?.getOrNull(1),
                    maneuverLongitude = loc?.getOrNull(0),
                )
            } ?: emptyList()

        return OsrmRouteResult(points, route.distance, route.duration, steps)
    }

    fun formatDistance(meters: Double): String =
        if (meters >= 1000) "${"%.1f".format(meters / 1000)} km" else "${meters.toInt()} m"

    fun formatDuration(seconds: Double): String {
        val minutes = (seconds / 60).toInt().coerceAtLeast(1)
        return if (minutes < 60) "$minutes min" else "${minutes / 60}h ${minutes % 60}m"
    }

    fun formatDurationMinutes(seconds: Double): String {
        val minutes = (seconds / 60).toInt().coerceAtLeast(1)
        return "$minutes min"
    }

    fun formatArrivalTime(secondsFromNow: Double): String {
        val arrival = System.currentTimeMillis() + (secondsFromNow * 1000).toLong()
        val cal = java.util.Calendar.getInstance().apply { timeInMillis = arrival }
        val hour = cal.get(java.util.Calendar.HOUR)
        val minute = cal.get(java.util.Calendar.MINUTE)
        val amPm = if (cal.get(java.util.Calendar.AM_PM) == java.util.Calendar.AM) "AM" else "PM"
        val h = if (hour == 0) 12 else hour
        return String.format("%d:%02d %s", h, minute, amPm)
    }
}
