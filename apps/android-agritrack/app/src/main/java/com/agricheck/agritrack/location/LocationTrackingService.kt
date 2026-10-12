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
import androidx.core.app.ActivityCompat
import androidx.core.app.NotificationCompat
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.MainActivity
import com.agricheck.agritrack.R
import com.agricheck.agritrack.push.PushNotificationManager
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
            locationSync.onLocationSample(uuid, location)
        }
    }

    override fun onCreate() {
        super.onCreate()
        PushNotificationManager.ensureChannels(this)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                stopTrackingInternal()
                stopSelf()
                return START_NOT_STICKY
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
                val manager = getSystemService(NOTIFICATION_SERVICE) as android.app.NotificationManager
                manager.notify(NOTIFICATION_ID, buildTrackingNotification())
            }
        }
        return START_STICKY
    }

    override fun onDestroy() {
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
        val title = if (navigationMode) {
            getString(R.string.nav_notification_title)
        } else {
            getString(R.string.tracking_notification_title)
        }
        val body = when {
            navigationMode && dest != null ->
                getString(R.string.nav_notification_body_to, dest, number)
            navigationMode ->
                getString(R.string.nav_notification_body_active, number)
            else -> getString(R.string.tracking_notification_body)
        }
        return NotificationCompat.Builder(this, PushNotificationManager.CHANNEL_TRACKING)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(NotificationCompat.BigTextStyle().bigText(body))
            .setOngoing(true)
            .setContentIntent(pending)
            .setForegroundServiceBehavior(NotificationCompat.FOREGROUND_SERVICE_IMMEDIATE)
            .build()
    }

    companion object {
        const val ACTION_START = "com.agricheck.agritrack.action.START_TRACKING"
        const val ACTION_UPDATE = "com.agricheck.agritrack.action.UPDATE_TRACKING"
        const val ACTION_STOP = "com.agricheck.agritrack.action.STOP_TRACKING"
        const val EXTRA_CONTAINER_UUID = "container_uuid"
        const val EXTRA_CONTAINER_NUMBER = "container_number"
        const val EXTRA_DESTINATION_LABEL = "destination_label"
        const val EXTRA_NAVIGATION_MODE = "navigation_mode"
        private const val NOTIFICATION_ID = 4101

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

        fun stop(context: Context) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_STOP
            }
            context.startService(intent)
        }
    }
}
