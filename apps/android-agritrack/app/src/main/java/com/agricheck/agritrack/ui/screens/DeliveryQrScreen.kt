package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import coil.compose.AsyncImage
import com.agricheck.agritrack.data.model.DriverDeliveryHandoffQrDto
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.ui.components.AgriScreenScaffold
import com.agricheck.agritrack.ui.components.ErrorState
import com.agricheck.agritrack.ui.components.LoadingState
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import kotlinx.coroutines.launch

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
    val scope = rememberCoroutineScope()

    fun load() {
        scope.launch {
            loading = true
            error = null
            runCatching { repository.deliveryHandoffQr(containerUuid) }
                .onSuccess { data = it }
                .onFailure { error = it.message }
            loading = false
        }
    }

    LaunchedEffect(containerUuid) { load() }

    val imageModel = data?.qrCodeData?.let { raw ->
        if (raw.startsWith("data:")) raw else "data:image/png;base64,$raw"
    }

    AgriScreenScaffold(title = UiStrings.DeliveryQr.title, onBack = onBack) { padding ->
        when {
            loading && data == null -> LoadingState()
            error != null && data == null -> ErrorState(error ?: "Failed") { load() }
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
                    if (imageModel != null) {
                        Surface(
                            shape = RoundedCornerShape(24.dp),
                            color = Color.White,
                            shadowElevation = 4.dp,
                        ) {
                            AsyncImage(
                                model = imageModel,
                                contentDescription = "Delivery QR code",
                                modifier = Modifier
                                    .size(280.dp)
                                    .padding(20.dp),
                                contentScale = ContentScale.Fit,
                            )
                        }
                    }
                    Spacer(Modifier.height(20.dp))
                    Text(
                        data?.handoffInstruction ?: UiStrings.DeliveryQr.handoffDefault,
                        textAlign = TextAlign.Center,
                        lineHeight = MaterialTheme.typography.bodyMedium.lineHeight,
                        color = AgriColors.OnSurface,
                    )
                }
            }
        }
    }
}
