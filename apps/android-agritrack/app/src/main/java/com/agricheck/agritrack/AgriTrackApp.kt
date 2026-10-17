package com.agricheck.agritrack

import android.app.Application
import android.util.Log
import com.agricheck.agritrack.data.api.NetworkModule
import com.agricheck.agritrack.data.api.NetworkServices
import com.agricheck.agritrack.data.local.DriverLocationQueueStore
import com.agricheck.agritrack.data.local.MapNavigationSessionStore
import com.agricheck.agritrack.data.local.OperatorRouteTestStore
import com.agricheck.agritrack.data.local.SettingsStore
import com.agricheck.agritrack.data.local.TokenStore
import com.agricheck.agritrack.data.local.TrackingStore
import com.agricheck.agritrack.data.repository.BrandingRepository
import com.agricheck.agritrack.data.repository.AuthRepository
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.data.repository.OperatorRepository
import com.agricheck.agritrack.data.repository.NewsRepository
import com.agricheck.agritrack.data.repository.PushRepository
import com.agricheck.agritrack.location.AppLocationManager
import com.agricheck.agritrack.location.ConnectivitySyncWatcher
import com.agricheck.agritrack.util.CoilImageLoader
class AgriTrackApp : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        runCatching { GoogleMapRuntimeInit.initialize(this) }
            .onFailure { e -> Log.e("AgriTrackApp", "Google map/nav init failed", e) }
        container = AppContainer(this)
        CoilImageLoader.install(this, container.tokenStore, BuildConfig.API_BASE_URL)
        ConnectivitySyncWatcher.register(this)
    }
}

class AppContainer(private val app: Application) {
    val tokenStore = TokenStore(app)
    val settingsStore = SettingsStore(app)
    val trackingStore = TrackingStore(app)
    val driverLocationQueueStore = DriverLocationQueueStore(app)
    val mapNavigationSessionStore = MapNavigationSessionStore()
    val operatorRouteTestStore = OperatorRouteTestStore(app)

    @Volatile
    private var cachedServices: NetworkServices? = null
    @Volatile
    private var cachedBaseUrl: String? = null

    suspend fun services(): NetworkServices {
        val base = resolveApiBaseUrl(settingsStore.getApiBaseUrl())
        val existing = cachedServices
        if (existing != null && cachedBaseUrl == base) return existing
        val created = NetworkModule.create(tokenStore, base)
        cachedServices = created
        cachedBaseUrl = base
        return created
    }

    fun resolveApiBaseUrl(stored: String?): String {
        val fallback = com.agricheck.agritrack.util.ApiUrlNormalizer.normalize(BuildConfig.API_BASE_URL)
            ?: BuildConfig.API_BASE_URL.trim().trimEnd('/')
        val value = com.agricheck.agritrack.util.ApiUrlNormalizer.normalize(stored)
        if (value.isNullOrBlank()) return fallback
        if (isLocalDevApiUrl(value) && !isLocalDevApiUrl(fallback)) return fallback
        return value
    }

    private fun isLocalDevApiUrl(url: String): Boolean {
        val normalized = url.trim().lowercase()
        return normalized.startsWith("http://")
            || normalized.contains("localhost")
            || normalized.contains("127.0.0.1")
            || normalized.contains("10.0.2.2")
    }

    suspend fun invalidateNetwork() {
        cachedServices = null
        cachedBaseUrl = null
        brandingRepository.invalidate()
        val base = resolveApiBaseUrl(settingsStore.getApiBaseUrl())
        CoilImageLoader.install(app, tokenStore, base)
    }

    val brandingRepository = BrandingRepository(this@AppContainer)
    val authRepository = AuthRepository(this, tokenStore)
    val driverRepository = DriverRepository(this)
    val operatorRepository = OperatorRepository(this)
    val pushRepository = PushRepository(this)
    val newsRepository = NewsRepository(this)
}
