package com.agricheck.agritrack.location

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
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
                override fun onAvailable(network: Network) {
                    scheduleFlush(appContext)
                }

                override fun onCapabilitiesChanged(network: Network, caps: android.net.NetworkCapabilities) {
                    if (caps.hasCapability(android.net.NetworkCapabilities.NET_CAPABILITY_VALIDATED)) {
                        scheduleFlush(appContext)
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
