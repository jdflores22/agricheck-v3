package com.agricheck.agritrack.location

import android.Manifest
import android.app.Notification
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.IBinder
import android.os.Looper
import androidx.core.app.ActivityCompat
import androidx.core.app.NotificationCompat
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.MainActivity
import com.agricheck.agritrack.R
import com.agricheck.agritrack.push.PushNotificationManager
import com.google.android.gms.location.*
import kotlinx.coroutines.*

class LocationTrackingService : Service() {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private lateinit var fusedClient: FusedLocationProviderClient
    private var containerUuid: String? = null

    private val callback = object : LocationCallback() {
        override fun onLocationResult(result: LocationResult) {
            val location = result.lastLocation ?: return
            val uuid = containerUuid ?: return
            scope.launch {
                runCatching {
                    val container = (application as AgriTrackApp).container
                    container.driverRepository.recordLocation(
                        uuid,
                        location.latitude,
                        location.longitude,
                    )
                }
            }
        }
    }

    override fun onCreate() {
        super.onCreate()
        fusedClient = LocationServices.getFusedLocationProviderClient(this)
        PushNotificationManager.ensureChannels(this)
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_STOP -> {
                stopTrackingInternal()
                stopSelf()
                return START_NOT_STICKY
            }
            ACTION_START -> {
                containerUuid = intent.getStringExtra(EXTRA_CONTAINER_UUID)
                if (containerUuid.isNullOrBlank()) {
                    stopSelf()
                    return START_NOT_STICKY
                }
                scope.launch {
                    (application as AgriTrackApp).container.trackingStore.setActiveContainerUuid(containerUuid)
                }
                startForeground(NOTIFICATION_ID, buildTrackingNotification())
                startLocationUpdates()
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
        val request = LocationRequest.Builder(Priority.PRIORITY_BALANCED_POWER_ACCURACY, 60_000L)
            .setMinUpdateIntervalMillis(45_000L)
            .setMinUpdateDistanceMeters(50f)
            .build()
        fusedClient.requestLocationUpdates(request, callback, Looper.getMainLooper())
    }

    private fun stopTrackingInternal() {
        fusedClient.removeLocationUpdates(callback)
        scope.launch {
            (application as AgriTrackApp).container.trackingStore.setActiveContainerUuid(null)
        }
    }

    private fun buildTrackingNotification(): Notification {
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP
        }
        val pending = PendingIntent.getActivity(
            this,
            0,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
        return NotificationCompat.Builder(this, PushNotificationManager.CHANNEL_TRACKING)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(getString(R.string.tracking_notification_title))
            .setContentText(getString(R.string.tracking_notification_body))
            .setOngoing(true)
            .setContentIntent(pending)
            .setForegroundServiceBehavior(NotificationCompat.FOREGROUND_SERVICE_IMMEDIATE)
            .build()
    }

    companion object {
        const val ACTION_START = "com.agricheck.agritrack.action.START_TRACKING"
        const val ACTION_STOP = "com.agricheck.agritrack.action.STOP_TRACKING"
        const val EXTRA_CONTAINER_UUID = "container_uuid"
        private const val NOTIFICATION_ID = 4101

        fun start(context: Context, containerUuid: String) {
            val intent = Intent(context, LocationTrackingService::class.java).apply {
                action = ACTION_START
                putExtra(EXTRA_CONTAINER_UUID, containerUuid)
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
