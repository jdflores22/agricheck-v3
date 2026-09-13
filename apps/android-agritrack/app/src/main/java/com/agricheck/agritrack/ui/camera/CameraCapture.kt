package com.agricheck.agritrack.ui.camera

import android.graphics.Bitmap
import android.graphics.Matrix
import android.view.Surface
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageCapture
import androidx.camera.core.ImageCaptureException
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.agricheck.agritrack.ui.theme.AgriColors
import java.io.ByteArrayOutputStream
import java.util.concurrent.Executors

@Composable
fun CameraCaptureScreen(
    title: String,
    useFrontCamera: Boolean,
    hint: String? = null,
    errorMessage: String? = null,
    onCaptured: (ByteArray) -> Unit,
    onCancel: () -> Unit,
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val mainExecutor = remember(context) { ContextCompat.getMainExecutor(context) }
    var status by remember { mutableStateOf<String?>(null) }
    var capturing by remember { mutableStateOf(false) }
    val imageCapture = remember(useFrontCamera) {
        ImageCapture.Builder()
            .setCaptureMode(ImageCapture.CAPTURE_MODE_MINIMIZE_LATENCY)
            .build()
    }
    val captureExecutor = remember { Executors.newSingleThreadExecutor() }

    Box(
        Modifier
            .fillMaxSize()
            .background(Color.Black),
    ) {
        key(useFrontCamera) {
            AndroidView(
                modifier = Modifier.fillMaxSize(),
                factory = { ctx ->
                    PreviewView(ctx).also { previewView ->
                        previewView.scaleType = PreviewView.ScaleType.FILL_CENTER
                        val cameraProviderFuture = ProcessCameraProvider.getInstance(ctx)
                        cameraProviderFuture.addListener({
                            val cameraProvider = cameraProviderFuture.get()
                            val rotation = previewView.display?.rotation ?: Surface.ROTATION_0
                            val preview = Preview.Builder()
                                .setTargetRotation(rotation)
                                .build()
                                .also { it.surfaceProvider = previewView.surfaceProvider }
                            val selector = if (useFrontCamera) {
                                CameraSelector.DEFAULT_FRONT_CAMERA
                            } else {
                                CameraSelector.DEFAULT_BACK_CAMERA
                            }
                            imageCapture.targetRotation = rotation
                            cameraProvider.unbindAll()
                            cameraProvider.bindToLifecycle(lifecycleOwner, selector, preview, imageCapture)
                        }, ContextCompat.getMainExecutor(ctx))
                    }
                },
            )
        }

        Column(
            Modifier
                .align(Alignment.TopCenter)
                .fillMaxWidth()
                .statusBarsPadding()
                .background(Color.Black.copy(alpha = 0.65f))
                .padding(horizontal = 16.dp, vertical = 12.dp),
        ) {
            Text(
                title,
                style = MaterialTheme.typography.titleMedium,
                color = Color.White,
            )
            hint?.let {
                Text(
                    it,
                    style = MaterialTheme.typography.bodySmall,
                    color = Color.White.copy(alpha = 0.85f),
                    modifier = Modifier.padding(top = 4.dp),
                )
            }
            errorMessage?.let {
                Text(
                    it,
                    style = MaterialTheme.typography.bodySmall,
                    color = AgriColors.Error,
                    modifier = Modifier.padding(top = 6.dp),
                )
            }
            status?.let {
                Text(
                    it,
                    style = MaterialTheme.typography.bodySmall,
                    color = AgriColors.Warning,
                    modifier = Modifier.padding(top = 6.dp),
                )
            }
        }

        Row(
            Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .navigationBarsPadding()
                .background(Color.Black.copy(alpha = 0.65f))
                .padding(16.dp),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            OutlinedButton(
                onClick = onCancel,
                modifier = Modifier.weight(1f),
                colors = ButtonDefaults.outlinedButtonColors(contentColor = Color.White),
            ) {
                Text("Cancel")
            }
            Button(
                onClick = {
                    if (capturing) return@Button
                    capturing = true
                    status = "Capturing…"
                    imageCapture.takePicture(
                        captureExecutor,
                        object : ImageCapture.OnImageCapturedCallback() {
                            override fun onCaptureSuccess(image: ImageProxy) {
                                val bytes = runCatching {
                                    imageProxyToJpeg(image, mirrorForFrontCamera = useFrontCamera)
                                }.getOrNull()
                                image.close()
                                mainExecutor.execute {
                                    capturing = false
                                    if (bytes != null) {
                                        onCaptured(bytes)
                                    } else {
                                        status = "Capture failed. Try again."
                                    }
                                }
                            }

                            override fun onError(exception: ImageCaptureException) {
                                mainExecutor.execute {
                                    capturing = false
                                    status = exception.message ?: "Capture failed"
                                }
                            }
                        },
                    )
                },
                enabled = !capturing,
                modifier = Modifier.weight(1f),
                colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
            ) {
                if (capturing) {
                    CircularProgressIndicator(
                        Modifier.size(18.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text("Capture")
                }
            }
        }
    }
}

private fun imageProxyToJpeg(image: ImageProxy, mirrorForFrontCamera: Boolean): ByteArray {
    val rotation = image.imageInfo.rotationDegrees
    val source = image.toBitmap()
    val rotated = if (rotation != 0) {
        val rotateMatrix = Matrix().apply { postRotate(rotation.toFloat()) }
        Bitmap.createBitmap(source, 0, 0, source.width, source.height, rotateMatrix, true).also {
            if (it != source) source.recycle()
        }
    } else {
        source
    }
    val bitmap = if (mirrorForFrontCamera) {
        val mirrorMatrix = Matrix().apply { postScale(-1f, 1f, rotated.width / 2f, rotated.height / 2f) }
        Bitmap.createBitmap(rotated, 0, 0, rotated.width, rotated.height, mirrorMatrix, true).also {
            if (it != rotated) rotated.recycle()
        }
    } else {
        rotated
    }
    val stream = ByteArrayOutputStream()
    bitmap.compress(Bitmap.CompressFormat.JPEG, 90, stream)
    if (!bitmap.isRecycled) bitmap.recycle()
    return stream.toByteArray()
}
