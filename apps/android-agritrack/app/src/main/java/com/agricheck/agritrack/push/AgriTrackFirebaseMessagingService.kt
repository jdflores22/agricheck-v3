package com.agricheck.agritrack.push

import com.agricheck.agritrack.AgriTrackApp
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class AgriTrackFirebaseMessagingService : FirebaseMessagingService() {
    private val scope = CoroutineScope(Dispatchers.IO)

    override fun onMessageReceived(message: RemoteMessage) {
        val type = message.data["type"]
        if (type == "agritrack_assignment") {
            val containerNumber = message.data["containerNumber"] ?: "container"
            val containerUuid = message.data["containerUuid"]
            PushNotificationManager.showAssignmentNotification(this, containerNumber, containerUuid)
        } else {
            val title = message.notification?.title ?: message.data["title"] ?: return
            val body = message.notification?.body ?: message.data["body"] ?: return
            PushNotificationManager.showAssignmentNotification(this, title, null)
        }
    }

    override fun onNewToken(token: String) {
        val app = application as? AgriTrackApp ?: return
        scope.launch {
            PushTokenRegistrar.sync(applicationContext) { pushToken ->
                app.container.pushRepository.register(pushToken, "android")
            }
        }
    }
}
