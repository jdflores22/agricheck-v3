package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.foundation.Image
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.data.model.DriverDeliveryHandoffQrDto
import com.agricheck.agritrack.data.api.apiErrorMessage
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.ui.components.AgriScreenScaffold
import com.agricheck.agritrack.ui.components.ErrorState
import com.agricheck.agritrack.ui.components.LoadingState
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.util.rememberHandoffQrImageBitmap
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

private fun Throwable.isHandoffNotReady(): Boolean {
    val m = message.orEmpty()
    return m.contains("Confirm arrival", ignoreCase = true) ||
        m.contains("HANDOFF_NOT_READY", ignoreCase = true)
}

private fun Throwable.isWarehouseGeofenceRetry(): Boolean =
    message.orEmpty().contains("within the assigned warehouse", ignoreCase = true)

@Composable
fun DeliveryQrScreen(
    containerUuid: String,
    containerNumber: String,
    repository: DriverRepository,
    onBack: () -> Unit,
) {
    var data by remember { mutableStateOf<DriverDeliveryHandoffQrDto?>(null) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var waitingMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    suspend fun fetchOnce(): Long {
        return runCatching { repository.deliveryHandoffQr(containerUuid) }
            .fold(
                onSuccess = { dto ->
                    data = dto
                    error = null
                    waitingMessage = null
                    (dto.refreshAfterSeconds.coerceIn(30, 120) * 1000L)
                },
                onFailure = { e ->
                    when {
                        e.isHandoffNotReady() -> {
                            waitingMessage = UiStrings.DeliveryQr.waitingForArrival
                            error = null
                        }
                        e.isWarehouseGeofenceRetry() -> {
                            waitingMessage = UiStrings.DeliveryQr.waitingForGps
                            error = e.message
                        }
                        else -> {
                            waitingMessage = null
                            if (data == null) {
                                error = e.apiErrorMessage("Could not load delivery QR.")
                            }
                        }
                    }
                    when {
                        e.isHandoffNotReady() -> 4_000L
                        e.isWarehouseGeofenceRetry() -> 5_000L
                        data != null -> 30_000L
                        else -> 12_000L
                    }
                },
            )
    }

    fun retryNow() {
        scope.launch {
            loading = data == null
            fetchOnce()
            loading = false
        }
    }

    LaunchedEffect(containerUuid) {
        loading = data == null
        while (isActive) {
            val delayMs = fetchOnce()
            loading = false
            delay(delayMs)
        }
    }

    val qrBitmap = rememberHandoffQrImageBitmap(data?.qrPayload, data?.qrCodeData)

    AgriScreenScaffold(title = UiStrings.DeliveryQr.title, onBack = onBack) { padding ->
        when {
            loading && data == null && waitingMessage == null -> LoadingState()
            error != null && data == null && waitingMessage == null -> ErrorState(error ?: "Failed") { retryNow() }
            else -> {
                Column(
                    Modifier
                        .padding(padding)
                        .padding(horizontal = 20.dp, vertical = 12.dp)
                        .fillMaxSize(),
                    horizontalAlignment = Alignment.CenterHorizontally,
                ) {
                    Surface(
                        shape = RoundedCornerShape(20.dp),
                        color = Color.White,
                        shadowElevation = 1.dp,
                        modifier = Modifier.fillMaxWidth(),
                    ) {
                        Column(
                            Modifier.padding(20.dp),
                            horizontalAlignment = Alignment.CenterHorizontally,
                        ) {
                            Text(
                                containerNumber,
                                fontWeight = FontWeight.Bold,
                                style = MaterialTheme.typography.titleLarge,
                                color = AgriColors.OnSurface,
                            )
                            data?.entryReference?.let {
                                Text(
                                    "${UiStrings.DeliveryQr.entryPrefix} $it",
                                    color = AgriColors.TextSecondary,
                                )
                            }
                        }
                    }
                    Spacer(Modifier.height(20.dp))
                    if (qrBitmap != null) {
                        Surface(
                            shape = RoundedCornerShape(24.dp),
                            color = Color.White,
                            shadowElevation = 4.dp,
                        ) {
                            Image(
                                bitmap = qrBitmap,
                                contentDescription = "Delivery QR code",
                                modifier = Modifier
                                    .size(280.dp)
                                    .padding(20.dp),
                                contentScale = ContentScale.Fit,
                            )
                        }
                    } else if (waitingMessage != null) {
                        CircularProgressIndicator(color = AgriColors.PrimaryDark)
                        Spacer(Modifier.height(16.dp))
                        Text(
                            waitingMessage!!,
                            textAlign = TextAlign.Center,
                            style = MaterialTheme.typography.bodyMedium,
                            color = AgriColors.OnSurface,
                        )
                    }
                    if (data != null) {
                        Spacer(Modifier.height(20.dp))
                        Text(
                            UiStrings.DeliveryQr.rotatingHint,
                            textAlign = TextAlign.Center,
                            style = MaterialTheme.typography.labelMedium,
                            color = AgriColors.PrimaryDark,
                        )
                        Spacer(Modifier.height(8.dp))
                        Text(
                            data?.handoffInstruction ?: UiStrings.DeliveryQr.handoffDefault,
                            textAlign = TextAlign.Center,
                            lineHeight = MaterialTheme.typography.bodyMedium.lineHeight,
                            color = AgriColors.OnSurface,
                        )
                    }
                    if (error != null && data != null) {
                        Spacer(Modifier.height(12.dp))
                        Text(
                            error!!,
                            textAlign = TextAlign.Center,
                            style = MaterialTheme.typography.bodySmall,
                            color = AgriColors.TextSecondary,
                        )
                    }
                }
            }
        }
    }
}
