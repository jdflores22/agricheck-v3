package com.agricheck.agritrack.location

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import com.agricheck.agritrack.push.OfflineRecordingNotifier
import kotlinx.coroutines.launch

/** Upload queued GPS points as soon as mobile data / Wi‑Fi returns. */
object ConnectivitySyncWatcher {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    @Volatile
    private var registered = false

    fun register(context: Context) {
        if (registered) return
        registered = true
        val appContext = context.applicationContext
        val cm = appContext.getSystemService(ConnectivityManager::class.java) ?: return
        cm.registerDefaultNetworkCallback(
            object : ConnectivityManager.NetworkCallback() {
                override fun onLost(network: Network) {
                    if (DriverGpsConnectivityState.markNetworkLost()) {
                        OfflineRecordingNotifier.showOfflineAlert(appContext)
                    }
                    LocationTrackingService.requestNotificationRefresh(appContext)
                }

                override fun onAvailable(network: Network) {
                    scheduleFlush(appContext)
                }

                override fun onCapabilitiesChanged(network: Network, caps: android.net.NetworkCapabilities) {
                    if (caps.hasCapability(android.net.NetworkCapabilities.NET_CAPABILITY_VALIDATED)) {
                        DriverGpsConnectivityState.markNetworkAvailable()
                        OfflineRecordingNotifier.dismissOfflineAlert(appContext)
                        scheduleFlush(appContext)
                        LocationTrackingService.requestNotificationRefresh(appContext)
                    }
                }
            },
        )
    }

    private fun scheduleFlush(appContext: Context) {
        scope.launch {
            DriverLocationSync(appContext).flushPendingUploads()
        }
    }
}
