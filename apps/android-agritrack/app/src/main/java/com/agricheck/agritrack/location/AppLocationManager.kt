package com.agricheck.agritrack.location

import android.annotation.SuppressLint
import android.content.Context
import android.location.Location
import android.location.LocationListener
import android.location.LocationManager
import android.os.Bundle
import android.os.Looper
import com.google.android.gms.common.ConnectionResult
import com.google.android.gms.common.GoogleApiAvailability
import com.google.android.gms.location.LocationCallback
import com.google.android.gms.location.LocationRequest
import com.google.android.gms.location.LocationResult
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import com.google.android.gms.tasks.CancellationTokenSource

fun interface LocationSubscription {
    fun cancel()
}

object AppLocationManager {

    fun isGooglePlayServicesAvailable(context: Context): Boolean {
        return try {
            val availability = GoogleApiAvailability.getInstance()
            val result = availability.isGooglePlayServicesAvailable(context)
            result == ConnectionResult.SUCCESS
        } catch (_: Throwable) {
            false
        }
    }

    @SuppressLint("MissingPermission")
    fun getCurrentLocation(
        context: Context,
        onResult: (Location?) -> Unit,
    ) {
        if (isGooglePlayServicesAvailable(context)) {
            try {
                val fusedClient = LocationServices.getFusedLocationProviderClient(context)
                val token = CancellationTokenSource().token
                fusedClient.getCurrentLocation(Priority.PRIORITY_HIGH_ACCURACY, token)
                    .addOnSuccessListener { loc ->
                        if (loc != null) {
                            onResult(loc)
                        } else {
                            fallbackGetLastLocation(context, onResult)
                        }
                    }
                    .addOnFailureListener {
                        fallbackGetLastLocation(context, onResult)
                    }
                return
            } catch (_: Throwable) {
                // SecurityException / Unknown calling package name or other GMS broker error
            }
        }
        fallbackGetLastLocation(context, onResult)
    }

    @SuppressLint("MissingPermission")
    private fun fallbackGetLastLocation(
        context: Context,
        onResult: (Location?) -> Unit,
    ) {
        runCatching {
            val locationManager = context.getSystemService(Context.LOCATION_SERVICE) as? LocationManager
            if (locationManager == null) {
                onResult(null)
                return
            }
            val providers = listOf(
                LocationManager.GPS_PROVIDER,
                LocationManager.NETWORK_PROVIDER,
                LocationManager.PASSIVE_PROVIDER,
            )
            val locations = providers.mapNotNull { provider ->
                runCatching { locationManager.getLastKnownLocation(provider) }.getOrNull()
            }
            val bestLocation = locations.maxByOrNull { it.time }
            onResult(bestLocation)
        }.onFailure {
            onResult(null)
        }
    }

    @SuppressLint("MissingPermission")
    fun requestLocationUpdates(
        context: Context,
        intervalMs: Long = 60_000L,
        minDistanceMeters: Float = 10f,
        highAccuracy: Boolean = false,
        onLocation: (Location) -> Unit,
    ): LocationSubscription {
        if (isGooglePlayServicesAvailable(context)) {
            try {
                val fusedClient = LocationServices.getFusedLocationProviderClient(context)
                val priority = if (highAccuracy) {
                    Priority.PRIORITY_HIGH_ACCURACY
                } else {
                    Priority.PRIORITY_BALANCED_POWER_ACCURACY
                }
                val minInterval = (intervalMs * 0.75).toLong().coerceAtLeast(1_000L)
                val request = LocationRequest.Builder(priority, intervalMs)
                    .setMinUpdateIntervalMillis(minInterval)
                    .setMinUpdateDistanceMeters(minDistanceMeters)
                    .build()

                val callback = object : LocationCallback() {
                    override fun onLocationResult(result: LocationResult) {
                        result.lastLocation?.let { onLocation(it) }
                    }
                }

                var fallbackSubscription: LocationSubscription? = null
                val task = fusedClient.requestLocationUpdates(request, callback, Looper.getMainLooper())
                task.addOnFailureListener {
                    // Fall back to LocationManager if GMS broker fails or throws SecurityException
                    if (fallbackSubscription == null) {
                        fallbackSubscription = startLocationManagerUpdates(context, intervalMs, minDistanceMeters, onLocation)
                    }
                }

                return LocationSubscription {
                    runCatching { fusedClient.removeLocationUpdates(callback) }
                    fallbackSubscription?.cancel()
                }
            } catch (_: Throwable) {
                // GMS SecurityException: Unknown calling package name 'com.google.android.gms'
                // Fall back to LocationManager below
            }
        }

        return startLocationManagerUpdates(context, intervalMs, minDistanceMeters, onLocation)
    }

    @SuppressLint("MissingPermission")
    private fun startLocationManagerUpdates(
        context: Context,
        intervalMs: Long,
        minDistanceMeters: Float,
        onLocation: (Location) -> Unit,
    ): LocationSubscription {
        val locationManager = context.getSystemService(Context.LOCATION_SERVICE) as? LocationManager
            ?: return LocationSubscription {}

        val listener = object : LocationListener {
            override fun onLocationChanged(location: Location) {
                onLocation(location)
            }
            @Deprecated("Deprecated in Java")
            override fun onStatusChanged(provider: String?, status: Int, extras: Bundle?) {}
            override fun onProviderEnabled(provider: String) {}
            override fun onProviderDisabled(provider: String) {}
        }

        val providers = listOf(LocationManager.GPS_PROVIDER, LocationManager.NETWORK_PROVIDER)
        for (provider in providers) {
            runCatching {
                if (locationManager.isProviderEnabled(provider)) {
                    locationManager.requestLocationUpdates(
                        provider,
                        intervalMs,
                        minDistanceMeters,
                        listener,
                        Looper.getMainLooper(),
                    )
                }
            }
        }

        // Deliver best known location immediately
        fallbackGetLastLocation(context) { lastLoc ->
            lastLoc?.let { onLocation(it) }
        }

        return LocationSubscription {
            runCatching { locationManager.removeUpdates(listener) }
        }
    }
}
