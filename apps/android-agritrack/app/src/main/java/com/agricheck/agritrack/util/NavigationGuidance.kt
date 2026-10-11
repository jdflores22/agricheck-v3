package com.agricheck.agritrack.util

import com.agricheck.agritrack.data.api.DrivingRouteStep
import com.agricheck.agritrack.navigation.NavigationPromptBand
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.sin

object NavigationGuidance {
    fun bearingDegrees(fromLat: Double, fromLng: Double, toLat: Double, toLng: Double): Float {
        val lat1 = Math.toRadians(fromLat)
        val lat2 = Math.toRadians(toLat)
        val dLon = Math.toRadians(toLng - fromLng)
        val y = sin(dLon) * cos(lat2)
        val x = cos(lat1) * sin(lat2) - sin(lat1) * cos(lat2) * cos(dLon)
        return ((Math.toDegrees(atan2(y, x)) + 360) % 360).toFloat()
    }

    fun findActiveStep(
        steps: List<DrivingRouteStep>,
        lat: Double,
        lng: Double,
    ): DrivingRouteStep? {
        if (steps.isEmpty()) return null
        var best: DrivingRouteStep? = null
        var bestDist = Double.MAX_VALUE
        for (step in steps) {
            val mLat = step.maneuverLatitude
            val mLng = step.maneuverLongitude
            if (mLat == null || mLng == null) continue
            val d = GeoUtils.distanceMeters(lat, lng, mLat, mLng)
            if (d < bestDist) {
                bestDist = d
                best = step
            }
        }
        if (best == null) return steps.first()
        val index = steps.indexOf(best)
        return if (bestDist < 45.0 && index + 1 < steps.size) steps[index + 1] else best
    }

    fun formatManeuverPrimary(step: DrivingRouteStep): String {
        val name = step.streetName?.trim()
        if (!name.isNullOrEmpty() && name != "-") return name
        return when (step.maneuverType) {
            "arrive" -> "Arrive at destination"
            "depart" -> "Start driving"
            "roundabout" -> "Take the roundabout"
            "rotary" -> "Enter rotary"
            "merge" -> "Merge"
            "fork" -> "Keep on route"
            "end of road" -> "Turn at end of road"
            else -> "Continue straight"
        }
    }

    fun formatManeuverSecondary(step: DrivingRouteStep, towardName: String): String {
        val dist = if (step.distanceMeters >= 1000) {
            "${"%.1f".format(step.distanceMeters / 1000)} km"
        } else {
            "${step.distanceMeters.toInt()} m"
        }
        val modifier = step.maneuverModifier?.replace('_', ' ')
        return when {
            step.maneuverType == "arrive" -> towardName
            !modifier.isNullOrBlank() -> "$dist · $modifier"
            else -> "$dist · toward $towardName"
        }
    }

    fun distanceToManeuver(step: DrivingRouteStep, lat: Double, lng: Double): Double? {
        val mLat = step.maneuverLatitude ?: return null
        val mLng = step.maneuverLongitude ?: return null
        return GeoUtils.distanceMeters(lat, lng, mLat, mLng)
    }

    fun offsetAlongBearing(lat: Double, lng: Double, bearingDeg: Float, distanceMeters: Double): Pair<Double, Double> {
        val radius = 6371000.0
        val bearing = Math.toRadians(bearingDeg.toDouble())
        val lat1 = Math.toRadians(lat)
        val lng1 = Math.toRadians(lng)
        val lat2 = Math.asin(
            sin(lat1) * cos(distanceMeters / radius) +
                cos(lat1) * sin(distanceMeters / radius) * cos(bearing),
        )
        val lng2 = lng1 + atan2(
            sin(bearing) * sin(distanceMeters / radius) * cos(lat1),
            cos(distanceMeters / radius) - sin(lat1) * sin(lat2),
        )
        return Math.toDegrees(lat2) to Math.toDegrees(lng2)
    }

    private fun maneuverVerb(step: DrivingRouteStep): String {
        val mod = step.maneuverModifier?.lowercase()?.replace('_', ' ') ?: ""
        return when {
            step.maneuverType == "arrive" -> "Arrive at"
            step.maneuverType == "depart" -> "Head"
            mod.contains("uturn") && mod.contains("left") -> "Make a U-turn to the left onto"
            mod.contains("uturn") && mod.contains("right") -> "Make a U-turn to the right onto"
            mod.contains("sharp left") -> "Turn sharp left onto"
            mod.contains("sharp right") -> "Turn sharp right onto"
            mod.contains("slight left") -> "Keep left onto"
            mod.contains("slight right") -> "Keep right onto"
            mod.contains("left") -> "Turn left onto"
            mod.contains("right") -> "Turn right onto"
            step.maneuverType == "roundabout" || step.maneuverType == "rotary" -> "Enter the roundabout toward"
            step.maneuverType == "merge" -> "Merge onto"
            else -> "Continue on"
        }
    }

    private fun streetClause(step: DrivingRouteStep): String {
        val name = step.streetName?.trim()
        return if (!name.isNullOrEmpty() && name != "-") name else "the route"
    }

    fun buildVoicePrompt(
        step: DrivingRouteStep,
        band: NavigationPromptBand,
        towardName: String,
    ): String {
        val street = streetClause(step)
        val verb = maneuverVerb(step)
        return when (band) {
            NavigationPromptBand.Meters400 -> "In 400 meters, $verb $street"
            NavigationPromptBand.Meters200 -> "In 200 meters, $verb $street"
            NavigationPromptBand.Meters80 -> "In 80 meters, $verb $street"
            NavigationPromptBand.Now -> when (step.maneuverType) {
                "arrive" -> "You have arrived at $towardName"
                else -> "$verb $street now"
            }
        }
    }
}
