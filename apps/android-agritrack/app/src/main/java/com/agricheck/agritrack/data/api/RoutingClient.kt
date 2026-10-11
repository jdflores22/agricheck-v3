package com.agricheck.agritrack.data.api

import com.agricheck.agritrack.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.withContext
import kotlinx.coroutines.Dispatchers.Main

object RoutingClient {
    /**
     * Fetches OSRM first (fast polyline), then prefers Google Directions when configured.
     * [onQuickRoute] is invoked on the caller's dispatcher when OSRM returns (optional).
     */
    suspend fun fetchDrivingRoute(
        fromLat: Double,
        fromLng: Double,
        toLat: Double,
        toLng: Double,
        onQuickRoute: ((OsrmRouteResult) -> Unit)? = null,
    ): OsrmRouteResult? = withContext(Dispatchers.IO) {
        coroutineScope {
            val osrmDeferred = async { OsrmClient.fetchDrivingRoute(fromLat, fromLng, toLat, toLng) }
            val key = BuildConfig.GOOGLE_MAPS_API_KEY
            val googleDeferred = if (key.isNotBlank()) {
                async { GoogleDirectionsClient.fetchDrivingRoute(fromLat, fromLng, toLat, toLng, key) }
            } else {
                null
            }

            val osrm = osrmDeferred.await()
            if (osrm != null && onQuickRoute != null) {
                withContext(Main) { onQuickRoute(osrm) }
            }

            googleDeferred?.await() ?: osrm
        }
    }
}
