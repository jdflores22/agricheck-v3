package com.agricheck.agritrack.ui.camera

import android.graphics.Bitmap
import android.graphics.Matrix
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageCapture
import androidx.camera.core.ImageCaptureException
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.agricheck.agritrack.ui.theme.AgriColors
import com.google.mlkit.vision.common.InputImage
import com.google.mlkit.vision.face.FaceDetection
import com.google.mlkit.vision.face.FaceDetectorOptions
import java.io.ByteArrayOutputStream
import java.util.concurrent.Executors

@Composable
fun CameraCaptureScreen(
    title: String,
    requireFace: Boolean,
    useFrontCamera: Boolean,
    onCaptured: (ByteArray, Float?) -> Unit,
    onCancel: () -> Unit,
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    var status by remember { mutableStateOf<String?>(null) }
    var capturing by remember { mutableStateOf(false) }
    val imageCapture = remember { ImageCapture.Builder().build() }
    val executor = remember { Executors.newSingleThreadExecutor() }

    Column(Modifier.fillMaxSize()) {
        Text(
            title,
            style = MaterialTheme.typography.titleMedium,
            modifier = Modifier.padding(16.dp),
            color = AgriColors.Primary,
        )
        status?.let {
            Text(it, modifier = Modifier.padding(horizontal = 16.dp), color = AgriColors.TextSecondary)
        }
        Box(Modifier.weight(1f).fillMaxWidth()) {
            AndroidView(
                modifier = Modifier.fillMaxSize(),
                factory = { ctx ->
                    PreviewView(ctx).also { previewView ->
                        val cameraProviderFuture = ProcessCameraProvider.getInstance(ctx)
                        cameraProviderFuture.addListener({
                            val cameraProvider = cameraProviderFuture.get()
                            val preview = Preview.Builder().build().also {
                                it.surfaceProvider = previewView.surfaceProvider
                            }
                            val selector = if (useFrontCamera) {
                                CameraSelector.DEFAULT_FRONT_CAMERA
                            } else {
                                CameraSelector.DEFAULT_BACK_CAMERA
                            }
                            cameraProvider.unbindAll()
                            cameraProvider.bindToLifecycle(
                                lifecycleOwner,
                                selector,
                                preview,
                                imageCapture,
                            )
                        }, ContextCompat.getMainExecutor(ctx))
                    }
                },
            )
        }
        Row(
            Modifier.fillMaxWidth().padding(16.dp),
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            OutlinedButton(onClick = onCancel, modifier = Modifier.weight(1f)) { Text("Cancel") }
            Button(
                onClick = {
                    if (capturing) return@Button
                    capturing = true
                    status = if (requireFace) "Detecting face…" else "Capturing…"
                    imageCapture.takePicture(
                        executor,
                        object : ImageCapture.OnImageCapturedCallback() {
                            override fun onCaptureSuccess(image: ImageProxy) {
                                val bytes = imageProxyToJpeg(image)
                                image.close()
                                if (!requireFace) {
                                    capturing = false
                                    onCaptured(bytes, null)
                                    return
                                }
                                detectFace(bytes) { confidence ->
                                    capturing = false
                                    if (confidence != null) {
                                        onCaptured(bytes, confidence)
                                    } else {
                                        status = "No face detected. Center your face and try again."
                                    }
                                }
                            }

                            override fun onError(exception: ImageCaptureException) {
                                capturing = false
                                status = exception.message ?: "Capture failed"
                            }
                        },
                    )
                },
                enabled = !capturing,
                modifier = Modifier.weight(1f),
                colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
            ) {
                if (capturing) {
                    CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp, color = MaterialTheme.colorScheme.onPrimary)
                } else {
                    Text("Capture")
                }
            }
        }
    }
}

private fun imageProxyToJpeg(image: ImageProxy): ByteArray {
    val bitmap = image.toBitmap()
    val stream = ByteArrayOutputStream()
    bitmap.compress(Bitmap.CompressFormat.JPEG, 90, stream)
    return stream.toByteArray()
}

private fun detectFace(bytes: ByteArray, onResult: (Float?) -> Unit) {
    val options = FaceDetectorOptions.Builder()
        .setPerformanceMode(FaceDetectorOptions.PERFORMANCE_MODE_ACCURATE)
        .setLandmarkMode(FaceDetectorOptions.LANDMARK_MODE_NONE)
        .setClassificationMode(FaceDetectorOptions.CLASSIFICATION_MODE_ALL)
        .build()
    val detector = FaceDetection.getClient(options)
    val bitmap = android.graphics.BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
    val image = InputImage.fromBitmap(bitmap, 0)
    detector.process(image)
        .addOnSuccessListener { faces ->
            val face = faces.firstOrNull()
            val confidence = face?.let {
                val left = it.leftEyeOpenProbability ?: 0.5f
                val right = it.rightEyeOpenProbability ?: 0.5f
                ((left + right) / 2f) * 100f
            }
            onResult(confidence)
        }
        .addOnFailureListener { onResult(null) }
}
