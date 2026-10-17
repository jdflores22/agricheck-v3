package com.agricheck.agritrack.util

import android.graphics.Bitmap
import android.graphics.Color
import android.util.Base64
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import com.google.zxing.BarcodeFormat
import com.google.zxing.qrcode.QRCodeWriter

fun decodeHandoffQrImageBitmap(qrCodeData: String?): ImageBitmap? {
    val raw = qrCodeData?.trim()?.takeIf { it.isNotEmpty() } ?: return null
    val base64 = raw.substringAfter("base64,", raw)
    val bytes = runCatching { Base64.decode(base64, Base64.DEFAULT) }.getOrNull() ?: return null
    val bitmap = android.graphics.BitmapFactory.decodeByteArray(bytes, 0, bytes.size) ?: return null
    return bitmap.asImageBitmap()
}

fun encodeHandoffQrImageBitmap(qrPayload: String, size: Int = 512): ImageBitmap? {
    val content = qrPayload.trim()
    if (content.isEmpty()) return null
    return runCatching {
        val matrix = QRCodeWriter().encode(content, BarcodeFormat.QR_CODE, size, size)
        val pixels = IntArray(size * size)
        for (y in 0 until size) {
            for (x in 0 until size) {
                pixels[y * size + x] = if (matrix[x, y]) Color.BLACK else Color.WHITE
            }
        }
        Bitmap.createBitmap(size, size, Bitmap.Config.RGB_565).apply {
            setPixels(pixels, 0, size, 0, 0, size, size)
        }.asImageBitmap()
    }.getOrNull()
}

@Composable
fun rememberHandoffQrImageBitmap(
    qrPayload: String?,
    qrCodeData: String?,
): ImageBitmap? = remember(qrPayload, qrCodeData) {
    decodeHandoffQrImageBitmap(qrCodeData)
        ?: encodeHandoffQrImageBitmap(qrPayload.orEmpty())
}
