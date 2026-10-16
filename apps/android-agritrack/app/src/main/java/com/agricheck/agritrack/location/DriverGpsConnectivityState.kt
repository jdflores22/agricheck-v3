package com.agricheck.agritrack.location

/**
 * Latest GPS upload connectivity (updated on each location sample and network callbacks).
 */
object DriverGpsConnectivityState {
    @Volatile
    var isNetworkOffline: Boolean = false
        private set

    @Volatile
    private var offlineSessionActive: Boolean = false

    /** @return true when the user should see a one-shot offline recording alert. */
    fun onSample(offline: Boolean): Boolean {
        isNetworkOffline = offline
        if (offline && !offlineSessionActive) {
            offlineSessionActive = true
            return true
        }
        if (!offline) {
            offlineSessionActive = false
        }
        return false
    }

    /** Network dropped before the next GPS sample. */
    fun markNetworkLost(): Boolean {
        isNetworkOffline = true
        if (!offlineSessionActive) {
            offlineSessionActive = true
            return true
        }
        return false
    }

    fun markNetworkAvailable() {
        isNetworkOffline = false
        offlineSessionActive = false
    }
}
