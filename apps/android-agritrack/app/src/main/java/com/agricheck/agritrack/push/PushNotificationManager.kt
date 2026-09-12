package com.agricheck.agritrack.push

import android.Manifest
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.agricheck.agritrack.BuildConfig
import com.agricheck.agritrack.MainActivity
import com.agricheck.agritrack.R
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.tasks.await

object PushNotificationManager {
    const val CHANNEL_ASSIGNMENTS = "agritrack_assignments"
    const val CHANNEL_TRACKING = "agritrack_tracking"

    fun ensureChannels(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val manager = context.getSystemService(NotificationManager::class.java) ?: return
        manager.createNotificationChannel(
            NotificationChannel(
                CHANNEL_ASSIGNMENTS,
                context.getString(R.string.push_channel_assignments),
                NotificationManager.IMPORTANCE_HIGH,
            ),
        )
        manager.createNotificationChannel(
            NotificationChannel(
                CHANNEL_TRACKING,
                context.getString(R.string.push_channel_tracking),
                NotificationManager.IMPORTANCE_LOW,
            ),
        )
    }

    fun hasPermission(context: Context): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return true
        return ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
    }

    fun showAssignmentNotification(context: Context, containerNumber: String, containerUuid: String?) {
        ensureChannels(context)
        if (!hasPermission(context)) return

        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            putExtra(EXTRA_OPEN_CONTAINERS, true)
            containerUuid?.let { putExtra(EXTRA_CONTAINER_UUID, it) }
        }
        val pending = PendingIntent.getActivity(
            context,
            containerNumber.hashCode(),
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )

        val notification = NotificationCompat.Builder(context, CHANNEL_ASSIGNMENTS)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(context.getString(R.string.assignment_notification_title))
            .setContentText(context.getString(R.string.assignment_notification_body, containerNumber))
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setAutoCancel(true)
            .setContentIntent(pending)
            .build()

        NotificationManagerCompat.from(context).notify(containerNumber.hashCode(), notification)
    }

    const val EXTRA_OPEN_CONTAINERS = "open_containers"
    const val EXTRA_CONTAINER_UUID = "container_uuid"
}

object PushTokenRegistrar {
    private const val PREFS = "agritrack_push"
    private const val KEY_TOKEN = "push_token"

    suspend fun sync(context: Context, register: suspend (String) -> Unit) {
        if (!BuildConfig.FIREBASE_ENABLED) return
        val token = runCatching { FirebaseMessaging.getInstance().token.await() }.getOrNull()
        if (token.isNullOrBlank()) return
        runCatching {
            register(token)
            saveLocalToken(context, token)
        }
    }

    suspend fun unregister(context: Context, unregister: suspend (String) -> Unit) {
        if (!BuildConfig.FIREBASE_ENABLED) return
        val token = loadLocalToken(context) ?: return
        runCatching {
            unregister(token)
            clearLocalToken(context)
        }
    }

    private fun saveLocalToken(context: Context, token: String) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString(KEY_TOKEN, token).apply()
    }

    private fun loadLocalToken(context: Context): String? =
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getString(KEY_TOKEN, null)

    private fun clearLocalToken(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().remove(KEY_TOKEN).apply()
    }
}

object AssignmentNotifier {
    suspend fun checkForNewAssignments(
        context: Context,
        containers: List<com.agricheck.agritrack.data.model.ContainerListItemDto>,
        trackingStore: com.agricheck.agritrack.data.local.TrackingStore,
        isInitialLoad: Boolean,
    ) {
        val known = trackingStore.loadKnownAssignmentUuids()
        val current = containers.map { it.uuid }.toSet()
        if (!isInitialLoad) {
            containers.filter { !known.contains(it.uuid) }.forEach { container ->
                PushNotificationManager.showAssignmentNotification(context, container.containerNumber, container.uuid)
            }
        }
        trackingStore.saveKnownAssignmentUuids(current)
    }
}
