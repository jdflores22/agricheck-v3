package com.agricheck.agritrack.navigation

import com.agricheck.agritrack.data.api.DrivingRouteStep

enum class NavigationPromptBand {
    Meters400,
    Meters200,
    Meters80,
    Now,
}

class NavigationPromptTracker {
    private var stepKey: String? = null
    private val spoken = mutableSetOf<NavigationPromptBand>()

    fun resetAll() {
        stepKey = null
        spoken.clear()
    }

    fun reset(step: DrivingRouteStep) {
        val key = "${step.maneuverLatitude}_${step.maneuverLongitude}_${step.maneuverType}_${step.streetName}"
        if (key != stepKey) {
            stepKey = key
            spoken.clear()
        }
    }

    fun bandForDistance(distanceMeters: Double): NavigationPromptBand? = when {
        distanceMeters <= 25 -> NavigationPromptBand.Now
        distanceMeters <= 90 -> NavigationPromptBand.Meters80
        distanceMeters <= 220 -> NavigationPromptBand.Meters200
        distanceMeters <= 450 -> NavigationPromptBand.Meters400
        else -> null
    }

    fun consume(step: DrivingRouteStep, distanceMeters: Double): NavigationPromptBand? {
        reset(step)
        val band = bandForDistance(distanceMeters) ?: return null
        if (spoken.contains(band)) return null
        spoken.add(band)
        return band
    }
}
