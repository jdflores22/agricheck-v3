package com.agricheck.agritrack.data.api

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.TimeUnit

@Serializable
private data class GoogleDirectionsResponse(
    val status: String,
    val routes: List<GoogleRoute>? = null,
)

@Serializable
private data class GoogleRoute(
    @SerialName("overview_polyline") val overviewPolyline: GooglePolyline? = null,
    val legs: List<GoogleLeg>? = null,
)

@Serializable
private data class GooglePolyline(val points: String? = null)

@Serializable
private data class GoogleLeg(
    val distance: GoogleValue? = null,
    val duration: GoogleValue? = null,
    val steps: List<GoogleStep>? = null,
)

@Serializable
private data class GoogleValue(val value: Double = 0.0)

@Serializable
private data class GoogleStep(
    val distance: GoogleValue? = null,
    val duration: GoogleValue? = null,
    val maneuver: String? = null,
    @SerialName("end_location") val endLocation: GoogleLatLng? = null,
)

@Serializable
private data class GoogleLatLng(val lat: Double = 0.0, val lng: Double = 0.0)

object GoogleDirectionsClient {
    private val json = Json { ignoreUnknownKeys = true }
    private val client = OkHttpClient.Builder()
        .connectTimeout(20, TimeUnit.SECONDS)
        .readTimeout(20, TimeUnit.SECONDS)
        .build()

    fun fetchDrivingRoute(
        fromLat: Double,
        fromLng: Double,
        toLat: Double,
        toLng: Double,
        apiKey: String,
    ): OsrmRouteResult? {
        if (apiKey.isBlank()) return null
        val url =
            "https://maps.googleapis.com/maps/api/directions/json" +
                "?origin=${fromLat},${fromLng}" +
                "&destination=${toLat},${toLng}" +
                "&mode=driving" +
                "&key=${apiKey}"
        val request = Request.Builder().url(url).get().build()
        val body = client.newCall(request).execute().body?.string() ?: return null
        val payload = json.decodeFromString<GoogleDirectionsResponse>(body)
        if (payload.status != "OK") return null
        val route = payload.routes?.firstOrNull() ?: return null
        val encoded = route.overviewPolyline?.points ?: return null
        val leg = route.legs?.firstOrNull() ?: return null
        val points = decodePolyline(encoded).map { it.first to it.second }
        if (points.size < 2) return null

        val steps = leg.steps?.mapNotNull { step ->
            val maneuver = step.maneuver
            val (type, modifier) = parseManeuver(maneuver)
            DrivingRouteStep(
                streetName = null,
                distanceMeters = step.distance?.value ?: 0.0,
                durationSeconds = step.duration?.value ?: 0.0,
                maneuverType = type,
                maneuverModifier = modifier,
                maneuverLatitude = step.endLocation?.lat,
                maneuverLongitude = step.endLocation?.lng,
            )
        } ?: emptyList()

        return OsrmRouteResult(
            points = points,
            distanceMeters = leg.distance?.value ?: 0.0,
            durationSeconds = leg.duration?.value ?: 0.0,
            steps = steps,
        )
    }

    private fun parseManeuver(maneuver: String?): Pair<String?, String?> {
        if (maneuver.isNullOrBlank()) return null to null
        val parts = maneuver.split("-")
        return when {
            parts.size >= 2 -> parts[0] to parts.drop(1).joinToString("-")
            else -> maneuver to null
        }
    }

    private fun decodePolyline(encoded: String): List<Pair<Double, Double>> {
        val coordinates = mutableListOf<Pair<Double, Double>>()
        var index = 0
        var lat = 0
        var lng = 0
        while (index < encoded.length) {
            var shift = 0
            var result = 0
            var b: Int
            do {
                val ch = encoded[index]
                index++
                b = ch.code - 63
                result = result or ((b and 0x1f) shl shift)
                shift += 5
            } while (b >= 0x20)
            val deltaLat = if (result and 1 != 0) (result shr 1).inv() else result shr 1
            lat += deltaLat

            shift = 0
            result = 0
            do {
                val ch = encoded[index]
                index++
                b = ch.code - 63
                result = result or ((b and 0x1f) shl shift)
                shift += 5
            } while (b >= 0x20)
            val deltaLng = if (result and 1 != 0) (result shr 1).inv() else result shr 1
            lng += deltaLng

            coordinates.add(lat / 1e5 to lng / 1e5)
        }
        return coordinates
    }
}
