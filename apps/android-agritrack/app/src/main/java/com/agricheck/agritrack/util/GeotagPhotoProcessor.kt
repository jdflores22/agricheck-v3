package com.agricheck.agritrack.util

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RectF
import androidx.exifinterface.media.ExifInterface
import com.agricheck.agritrack.ui.strings.UiStrings
import java.io.ByteArrayOutputStream
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import kotlin.math.max

object GeotagPhotoProcessor {
    private val philippinesZone = ZoneId.of("Asia/Manila")
    private val displayFormatter =
        DateTimeFormatter.ofPattern("dd MMM yyyy HH:mm:ss").withZone(philippinesZone)

    fun stampJpeg(
        jpegBytes: ByteArray,
        latitude: Double,
        longitude: Double,
        capturedAt: Instant,
        brandLogo: Bitmap? = null,
    ): ByteArray {
        val source = BitmapFactory.decodeByteArray(jpegBytes, 0, jpegBytes.size)
            ?: return jpegBytes
        val bitmap = source.copy(Bitmap.Config.ARGB_8888, true)
        if (bitmap != source) source.recycle()

        val line1 = "Lat ${"%.6f".format(latitude)}  Lng ${"%.6f".format(longitude)}"
        val line2 = "${displayFormatter.format(capturedAt)} (PH time)"
        val line3 = UiStrings.Geotag.stampLabel

        val barHeight = max(bitmap.height * 0.12f, 96f)
        val logoColumnWidth = barHeight * 1.15f
        val textLeft = logoColumnWidth + 14f
        val barTop = bitmap.height - barHeight

        val canvas = Canvas(bitmap)
        val barPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = 0xC8000000.toInt() }
        canvas.drawRect(RectF(0f, barTop, bitmap.width.toFloat(), bitmap.height.toFloat()), barPaint)

        drawBrandLogoInBar(canvas, brandLogo, barTop, barHeight, logoColumnWidth)

        val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = 0xFFFFFFFF.toInt()
            textSize = max(bitmap.width * 0.026f, 20f)
        }
        val lineGap = textPaint.textSize + 5f
        val textBlockHeight = lineGap * 2 + textPaint.textSize * 0.85f
        var y = barTop + (barHeight - textBlockHeight) / 2f + textPaint.textSize
        canvas.drawText(line1, textLeft, y, textPaint)
        y += lineGap
        canvas.drawText(line2, textLeft, y, textPaint)
        y += lineGap - 2f
        textPaint.textSize *= 0.85f
        canvas.drawText(line3, textLeft, y, textPaint)

        val stream = ByteArrayOutputStream()
        bitmap.compress(Bitmap.CompressFormat.JPEG, 90, stream)
        bitmap.recycle()
        val stamped = stream.toByteArray()
        return writeExifGps(stamped, latitude, longitude)
    }

    private fun drawBrandLogoInBar(
        canvas: Canvas,
        logo: Bitmap?,
        barTop: Float,
        barHeight: Float,
        logoColumnWidth: Float,
    ) {
        if (logo == null || logo.width <= 0 || logo.height <= 0) return
        val maxLogoHeight = barHeight * 0.78f
        val scale = maxLogoHeight / logo.height
        val drawWidth = logo.width * scale
        val drawHeight = logo.height * scale
        val left = (logoColumnWidth - drawWidth) / 2f
        val top = barTop + (barHeight - drawHeight) / 2f
        canvas.drawBitmap(logo, null, RectF(left, top, left + drawWidth, top + drawHeight), null)
    }

    private fun writeExifGps(jpeg: ByteArray, latitude: Double, longitude: Double): ByteArray {
        return runCatching {
            val scratch = java.io.File.createTempFile("geotag", ".jpg")
            scratch.writeBytes(jpeg)
            val fileExif = ExifInterface(scratch.absolutePath)
            fileExif.setLatLong(latitude, longitude)
            fileExif.saveAttributes()
            val result = scratch.readBytes()
            scratch.delete()
            result
        }.getOrElse { jpeg }
    }
}
