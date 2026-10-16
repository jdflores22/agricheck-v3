package com.agricheck.agritrack.location

import android.Manifest
import android.app.Notification
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.location.Location
import android.os.IBinder
import android.widget.RemoteViews
import androidx.core.app.ActivityCompat
import androidx.core.app.NotificationCompat
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.MainActivity
import com.agricheck.agritrack.R
import com.agricheck.agritrack.navigation.NavigationNotificationState
import com.agricheck.agritrack.navigation.NavigationTrafficStripBitmap
import com.agricheck.agritrack.push.AgriTrackNotificationStyle
import com.agricheck.agritrack.push.OfflineRecordingNotifier
import com.agricheck.agritrack.push.PushNotificationManager
import android.view.View
import kotlinx.coroutines.*

class LocationTrackingService : Service() {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private var locationSubscription: LocationSubscription? = null
    private var containerUuid: String? = null
    private var containerNumber: String? = null
    private var destinationLabel: String? = null
    private var navigationMode: Boolean = false
    private val locationSync by lazy { DriverLocationSync(this) }

    private fun handleLocationUpdate(location: Location) {
        val uuid = containerUuid ?: return
        scope.launch {
            val showOfflineAlert = locationSync.onLocationSample(uuid, location)
            if (showOfflineAlert) {
                OfflineRecordingNotifier.showOfflineAlert(this@LocationTrackingService)
            }
            refreshForegroundNotification()
        }
    }

    override fun onCreate() {
        super.onCreate()
        PushNotificationManager.ensureChannels(this)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                NavigationNotificationState.clear()
                stopTrackingInternal()
                stopSelf()
                return START_NOT_STICKY
            }
            ACTION_REFRESH_NOTIFICATION -> {
                refreshForegroundNotification()
                return START_STICKY
            }
            ACTION_START, ACTION_UPDATE -> {
                containerUuid = intent.getStringExtra(EXTRA_CONTAINER_UUID)
                if (containerUuid.isNullOrBlank()) {
                    stopSelf()
                    return START_NOT_STICKY
                }
                containerNumber = intent.getStringExtra(EXTRA_CONTAINER_NUMBER)
                destinationLabel = intent.getStringExtra(EXTRA_DESTINATION_LABEL)
                navigationMode = intent.getBooleanExtra(EXTRA_NAVIGATION_MODE, false)
                scope.launch {
                    (application as AgriTrackApp).container.trackingStore.setActiveContainerUuid(containerUuid)
                    DriverLocationSync(this@LocationTrackingService).flushPendingUploads()
                }
                startForeground(NOTIFICATION_ID, buildTrackingNotification())
                startLocationUpdates()
                refreshForegroundNotification()
            }
        }
        return START_STICKY
    }

    override fun onDestroy() {
        NavigationNotificationState.clear()
        stopTrackingInternal()
        scope.cancel()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun startLocationUpdates() {
        if (ActivityCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
            != PackageManager.PERMISSION_GRANTED
        ) {
            stopSelf()
            return
        }
        locationSubscription?.cancel()
        val nav = navigationMode
        locationSubscription = AppLocationManager.requestLocationUpdates(
            context = this,
            intervalMs = if (nav) {
                DriverTrackingPolicy.SERVICE_INTERVAL_NAV_MS
            } else {
                DriverTrackingPolicy.SERVICE_INTERVAL_IDLE_MS
            },
            minDistanceMeters = if (nav) {
                DriverTrackingPolicy.SERVICE_MIN_DISTANCE_NAV_METERS
            } else {
                DriverTrackingPolicy.SERVICE_MIN_DISTANCE_IDLE_METERS
            },
            highAccuracy = true,
            onLocation = { location -> handleLocationUpdate(location) },
        )
    }

    private fun stopTrackingInternal() {
        locationSubscription?.cancel()
        locationSubscription = null
        scope.launch {
            (application as AgriTrackApp).container.trackingStore.setActiveContainerUuid(null)
        }
    }

    private fun refreshForegroundNotification() {
        val manager = getSystemService(NOTIFICATION_SERVICE) as android.app.NotificationManager
        manager.notify(NOTIFICATION_ID, buildTrackingNotification())
    }

    private fun buildTrackingNotification(): Notification {
        val uuid = containerUuid
        val number = containerNumber ?: ""
        val dest = destinationLabel?.takeIf { it.isNotBlank() }
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            if (!uuid.isNullOrBlank()) {
                putExtra(PushNotificationManager.EXTRA_OPEN_MAP, true)
                putExtra(PushNotificationManager.EXTRA_CONTAINER_UUID, uuid)
                putExtra(PushNotificationManager.EXTRA_CONTAINER_NUMBER, number)
            }
        }
        val pending = PendingIntent.getActivity(
            this,
            0,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
        val offline = DriverGpsConnectivityState.isNetworkOffline
        val title = when {
            offline && !navigationMode -> getString(R.string.tracking_notification_offline_title)
            navigationMode -> getString(R.string.nav_notification_title)
            else -> getString(R.string.tracking_notification_title)
        }
        val body = when {
            offline && !navigationMode -> getString(R.string.tracking_notification_offline_body)
            navigationMode && dest != null ->
                getString(R.string.nav_notification_body_to, dest, number)
            navigationMode ->
                getString(R.string.nav_notification_body_active, number)
            else -> getString(R.string.tracking_notification_body)
        }
        val builder = AgriTrackNotificationStyle.applyBranding(
            this,
            NotificationCompat.Builder(this, PushNotificationManager.CHANNEL_TRACKING)
                .setOngoing(true)
                .setContentIntent(pending)
                .setForegroundServiceBehavior(NotificationCompat.FOREGROUND_SERVICE_IMMEDIATE),
        )

        if (navigationMode) {
            val state = NavigationNotificationState
            val maneuver = state.maneuverPrimary.ifBlank {
                getString(R.string.nav_notification_maneuver_fallback)
            }
            val custom = buildNavigationRemoteViews(maneuver)
            builder
                .setContentTitle(" ")
                .setContentText(" ")
                .setStyle(NotificationCompat.DecoratedCustomViewStyle())
                .setCustomContentView(custom)
                .setCustomBigContentView(custom)
        } else {
            builder
                .setContentTitle(title)
                .setContentText(body)
                .setStyle(NotificationCompat.BigTextStyle().bigText(body))
        }
        return builder.build()
    }

    private fun buildNavigationRemoteViews(maneuver: String): RemoteViews {
        val views = RemoteViews(packageName, R.layout.notification_navigation)
        val state = NavigationNotificationState
        views.setImageViewResource(R.id.nav_notification_maneuver_icon, state.maneuverIconRes)
        views.setTextViewText(R.id.nav_notification_maneuver, maneuver)
        val widthPx = (resources.displayMetrics.widthPixels * 0.88f).toInt()
        val strip = NavigationTrafficStripBitmap.create(
            widthPx = widthPx,
            heightPx = (14 * resources.displayMetrics.density).toInt().coerceAtLeast(28),
            intervals = state.trafficIntervals,
            routePointCount = state.routePointCount,
            anchorIndex = state.anchorPointIndex,
            hasTrafficData = state.hasGoogleTrafficLine,
        )
        views.setImageViewBitmap(R.id.nav_notification_traffic_strip, strip)
        if (DriverGpsConnectivityState.isNetworkOffline) {
            views.setViewVisibility(R.id.nav_notification_offline_status, View.VISIBLE)
        } else {
            views.setViewVisibility(R.id.nav_notification_offline_status, View.GONE)
        }
        return views
    }

    companion object {
        const val ACTION_START = "com.agricheck.agritrack.action.START_TRACKING"
        const val ACTION_UPDATE = "com.agricheck.agritrack.action.UPDATE_TRACKING"
        const val ACTION_REFRESH_NOTIFICATION = "com.agricheck.agritrack.action.REFRESH_NAV_NOTIFICATION"
        const val ACTION_STOP = "com.agricheck.agritrack.action.STOP_TRACKING"
        const val EXTRA_CONTAINER_UUID = "container_uuid"
        const val EXTRA_CONTAINER_NUMBER = "container_number"
        const val EXTRA_DESTINATION_LABEL = "destination_label"
        const val EXTRA_NAVIGATION_MODE = "navigation_mode"
        private const val NOTIFICATION_ID = 4101

        fun requestNotificationRefresh(context: Context) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_REFRESH_NOTIFICATION
            }
            context.startService(intent)
        }

        fun start(context: Context, containerUuid: String) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_START
                putExtra(EXTRA_CONTAINER_UUID, containerUuid)
                putExtra(EXTRA_NAVIGATION_MODE, false)
            }
            context.startForegroundService(intent)
        }

        fun startNavigation(
            context: Context,
            containerUuid: String,
            containerNumber: String,
            destinationLabel: String,
        ) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_START
                putExtra(EXTRA_CONTAINER_UUID, containerUuid)
                putExtra(EXTRA_CONTAINER_NUMBER, containerNumber)
                putExtra(EXTRA_DESTINATION_LABEL, destinationLabel)
                putExtra(EXTRA_NAVIGATION_MODE, true)
            }
            context.startForegroundService(intent)
        }

        fun updateNavigation(
            context: Context,
            containerUuid: String,
            containerNumber: String,
            destinationLabel: String,
        ) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_UPDATE
                putExtra(EXTRA_CONTAINER_UUID, containerUuid)
                putExtra(EXTRA_CONTAINER_NUMBER, containerNumber)
                putExtra(EXTRA_DESTINATION_LABEL, destinationLabel)
                putExtra(EXTRA_NAVIGATION_MODE, true)
            }
            context.startForegroundService(intent)
        }

        fun refreshNavigationNotification(context: Context) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_REFRESH_NOTIFICATION
            }
            context.startService(intent)
        }

        fun stop(context: Context) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_STOP
            }
            context.startService(intent)
        }
    }
}
