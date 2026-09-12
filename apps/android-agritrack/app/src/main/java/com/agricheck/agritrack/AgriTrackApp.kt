package com.agricheck.agritrack

import android.app.Application
import com.agricheck.agritrack.data.api.NetworkModule
import com.agricheck.agritrack.data.api.NetworkServices
import com.agricheck.agritrack.data.local.SettingsStore
import com.agricheck.agritrack.data.local.TokenStore
import com.agricheck.agritrack.data.local.TrackingStore
import com.agricheck.agritrack.data.repository.AuthRepository
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.data.repository.OperatorRepository
import com.agricheck.agritrack.data.repository.PushRepository
import org.osmdroid.config.Configuration

class AgriTrackApp : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        Configuration.getInstance().load(this, getSharedPreferences("osmdroid", MODE_PRIVATE))
        Configuration.getInstance().userAgentValue = packageName
        container = AppContainer(this)
    }
}

class AppContainer(private val app: Application) {
    val tokenStore = TokenStore(app)
    val settingsStore = SettingsStore(app)
    val trackingStore = TrackingStore(app)

    @Volatile
    private var cachedServices: NetworkServices? = null
    @Volatile
    private var cachedBaseUrl: String? = null

    suspend fun services(): NetworkServices {
        val base = settingsStore.getApiBaseUrl()?.trim()?.trimEnd('/')
            ?: BuildConfig.API_BASE_URL.trim().trimEnd('/')
        val existing = cachedServices
        if (existing != null && cachedBaseUrl == base) return existing
        val created = NetworkModule.create(tokenStore, base)
        cachedServices = created
        cachedBaseUrl = base
        return created
    }

    suspend fun invalidateNetwork() {
        cachedServices = null
        cachedBaseUrl = null
    }

    val authRepository = AuthRepository(this, tokenStore)
    val driverRepository = DriverRepository(this)
    val operatorRepository = OperatorRepository(this)
    val pushRepository = PushRepository(this)
}
