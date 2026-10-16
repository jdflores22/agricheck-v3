package com.agricheck.agritrack.push

import android.content.Context
import android.graphics.BitmapFactory
import androidx.core.app.NotificationCompat
import android.app.PendingIntent
import android.content.Intent
import com.agricheck.agritrack.MainActivity
import com.agricheck.agritrack.R

object AgriTrackNotificationStyle {
    fun applyBranding(context: Context, builder: NotificationCompat.Builder): NotificationCompat.Builder {
        builder.setSmallIcon(R.drawable.ic_stat_agritrack)
        val bitmap = BitmapFactory.decodeResource(context.resources, R.drawable.agritrack_logo)
        if (bitmap != null) {
            builder.setLargeIcon(bitmap)
        }
        return builder
    }
}

object OfflineRecordingNotifier {
    private const val OFFLINE_ALERT_ID = 4102

    fun showOfflineAlert(context: Context) {
        PushNotificationManager.ensureChannels(context)
        if (!PushNotificationManager.hasPermission(context)) return

        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP
            putExtra(PushNotificationManager.EXTRA_OPEN_MAP, true)
        }
        val pending = PendingIntent.getActivity(
            context,
            OFFLINE_ALERT_ID,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )

        val notification = AgriTrackNotificationStyle.applyBranding(
            context,
            NotificationCompat.Builder(context, PushNotificationManager.CHANNEL_GPS_ALERTS)
                .setContentTitle(context.getString(R.string.offline_recording_alert_title))
                .setContentText(context.getString(R.string.offline_recording_alert_body))
                .setStyle(
                    NotificationCompat.BigTextStyle()
                        .bigText(context.getString(R.string.offline_recording_alert_body)),
                )
                .setPriority(NotificationCompat.PRIORITY_DEFAULT)
                .setAutoCancel(true)
                .setContentIntent(pending),
        ).build()

        androidx.core.app.NotificationManagerCompat.from(context).notify(OFFLINE_ALERT_ID, notification)
    }

    fun dismissOfflineAlert(context: Context) {
        androidx.core.app.NotificationManagerCompat.from(context).cancel(OFFLINE_ALERT_ID)
    }
}
