package com.agricheck.agritrack.util

import android.content.Context
import com.agricheck.agritrack.BuildConfig
import com.agricheck.agritrack.location.AppLocationManager

object MapsConfig {
    fun googleMapsApiKey(): String = BuildConfig.GOOGLE_MAPS_API_KEY.trim()

    fun isGoogleMapsConfigured(): Boolean {
        val key = googleMapsApiKey()
        return key.isNotEmpty() &&
            !key.contains("PLACEHOLDER", ignoreCase = true) &&
            !key.startsWith("your_key", ignoreCase = true)
    }

    /** Google Map *tiles* in-app (needs Maps SDK + correct SHA-1 + working Google Play Services). Directions still use the API key when set. */
    fun useGoogleMapTilesInApp(context: Context? = null): Boolean {
        if (!BuildConfig.ENABLE_GOOGLE_MAP_TILES || !isGoogleMapsConfigured()) return false
        return context == null || AppLocationManager.isGooglePlayServicesAvailable(context)
    }
}
