package com.agricheck.agritrack.ui.screens

import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.agricheck.agritrack.data.model.DriverTransportQrPreviewDto
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.ui.components.AgriPrimaryButton
import com.agricheck.agritrack.ui.components.AgriScreenScaffold
import com.agricheck.agritrack.ui.components.LoadingState
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.common.InputImage
import kotlinx.coroutines.launch
import java.util.concurrent.atomic.AtomicBoolean

@Composable
fun QrScanScreen(
    repository: DriverRepository,
    onAccepted: () -> Unit,
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val scope = rememberCoroutineScope()
    var preview by remember { mutableStateOf<DriverTransportQrPreviewDto?>(null) }
    var scannedQr by remember { mutableStateOf<String?>(null) }
    var loading by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    var message by remember { mutableStateOf<String?>(null) }
    val scannedOnce = remember { AtomicBoolean(false) }

    fun loadPreview(qr: String) {
        scope.launch {
            loading = true
            error = null
            runCatching { repository.previewTransportQr(qr) }
                .onSuccess { preview = it; scannedQr = qr }
                .onFailure { error = it.message }
            loading = false
        }
    }

    AgriScreenScaffold(title = UiStrings.ScanQr.title, nestedInMainShell = true) { padding ->
        Column(Modifier.padding(padding).fillMaxSize()) {
            if (preview == null) {
                Box(
                    Modifier
                        .weight(1f)
                        .fillMaxWidth()
                        .padding(16.dp),
                ) {
                    Surface(
                        shape = RoundedCornerShape(20.dp),
                        color = Color.Black,
                        modifier = Modifier.fillMaxSize(),
                    ) {
                        AndroidView(
                            modifier = Modifier.fillMaxSize(),
                            factory = { ctx ->
                                PreviewView(ctx).also { previewView ->
                                    val cameraProviderFuture = ProcessCameraProvider.getInstance(ctx)
                                    cameraProviderFuture.addListener({
                                        val cameraProvider = cameraProviderFuture.get()
                                        val previewUseCase = Preview.Builder().build().also {
                                            it.surfaceProvider = previewView.surfaceProvider
                                        }
                                        val scanner = BarcodeScanning.getClient()
                                        val analysis = ImageAnalysis.Builder()
                                            .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                                            .build()
                                        analysis.setAnalyzer(ContextCompat.getMainExecutor(ctx)) { imageProxy ->
                                            if (scannedOnce.get()) {
                                                imageProxy.close()
                                                return@setAnalyzer
                                            }
                                            val mediaImage = imageProxy.image
                                            if (mediaImage != null) {
                                                val image = InputImage.fromMediaImage(
                                                    mediaImage,
                                                    imageProxy.imageInfo.rotationDegrees,
                                                )
                                                scanner.process(image)
                                                    .addOnSuccessListener { barcodes ->
                                                        val raw = barcodes.firstOrNull { it.rawValue != null }?.rawValue
                                                        if (raw != null && scannedOnce.compareAndSet(false, true)) {
                                                            loadPreview(raw)
                                                        }
                                                    }
                                                    .addOnCompleteListener { imageProxy.close() }
                                            } else {
                                                imageProxy.close()
                                            }
                                        }
                                        cameraProvider.unbindAll()
                                        cameraProvider.bindToLifecycle(
                                            lifecycleOwner,
                                            CameraSelector.DEFAULT_BACK_CAMERA,
                                            previewUseCase,
                                            analysis,
                                        )
                                    }, ContextCompat.getMainExecutor(ctx))
                                }
                            },
                        )
                    }
                }
            } else {
                Column(
                    Modifier.weight(1f).padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    Surface(
                        shape = RoundedCornerShape(20.dp),
                        color = Color.White,
                        shadowElevation = 2.dp,
                        modifier = Modifier.fillMaxWidth(),
                    ) {
                        Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text(
                                preview!!.containerNumber,
                                fontWeight = FontWeight.Bold,
                                style = MaterialTheme.typography.titleLarge,
                                color = AgriColors.OnSurface,
                            )
                            Text(
                                "${UiStrings.DeliveryQr.entryPrefix} ${preview!!.entryReference}",
                                color = AgriColors.TextSecondary,
                            )
                            Text(
                                "${UiStrings.ScanQr.deliverTo}: ${preview!!.warehouseName}",
                                fontWeight = FontWeight.SemiBold,
                                color = AgriColors.PrimaryDark,
                            )
                            preview!!.warehouseAddress?.let { Text(it, style = MaterialTheme.typography.bodySmall) }
                            preview!!.scheduledWarehouseDate?.let {
                                Text("${UiStrings.ScanQr.scheduled}: $it", style = MaterialTheme.typography.bodySmall)
                            }
                            Text("${UiStrings.ScanQr.status}: ${preview!!.containerStatus}")
                            if (!preview!!.canAccept) {
                                Text(
                                    preview!!.blockReason ?: UiStrings.ScanQr.cannotAccept,
                                    color = AgriColors.Error,
                                )
                            }
                        }
                    }
                    if (loading) LoadingState()
                    error?.let { Text(it, color = AgriColors.Error) }
                    message?.let { Text(it, color = AgriColors.Success, fontWeight = FontWeight.SemiBold) }
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        OutlinedButton(
                            onClick = {
                                preview = null
                                scannedQr = null
                                scannedOnce.set(false)
                                error = null
                            },
                            shape = RoundedCornerShape(12.dp),
                        ) { Text(UiStrings.ScanQr.scanAgain) }
                        AgriPrimaryButton(
                            text = UiStrings.ScanQr.accept,
                            enabled = preview?.canAccept == true && !loading,
                            loading = loading,
                            onClick = {
                                val qr = scannedQr ?: return@AgriPrimaryButton
                                scope.launch {
                                    loading = true
                                    runCatching { repository.acceptTransportQr(qr) }
                                        .onSuccess {
                                            message = UiStrings.ScanQr.acceptSuccess
                                            onAccepted()
                                        }
                                        .onFailure { error = it.message }
                                    loading = false
                                }
                            },
                        )
                    }
                }
            }
        }
    }
}
