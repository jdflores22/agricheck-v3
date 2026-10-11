package com.agricheck.agritrack.map

import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Matrix
import android.graphics.Paint
import android.graphics.Path
import android.graphics.RectF
import com.google.android.gms.maps.model.BitmapDescriptor
import com.google.android.gms.maps.model.BitmapDescriptorFactory

/** Top-down truck marker for navigation (OSM overlay + Google Maps). */
object TruckMapIcons {
    private const val SIZE = 96

    fun createBitmapDescriptor(bearingDegrees: Float = 0f): BitmapDescriptor {
        val bitmap = Bitmap.createBitmap(SIZE, SIZE, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        drawTruck(canvas, SIZE / 2f, SIZE / 2f, bearingDegrees, mapOrientation = 0f)
        return BitmapDescriptorFactory.fromBitmap(bitmap)
    }

    fun drawTruckOnCanvas(
        canvas: Canvas,
        centerX: Float,
        centerY: Float,
        bearingDegrees: Float,
        mapOrientation: Float,
        showGlow: Boolean = true,
    ) {
        if (showGlow) {
            drawLocationGlow(canvas, centerX, centerY)
        }
        drawTruck(canvas, centerX, centerY, bearingDegrees, mapOrientation)
    }

    fun drawDriverLocation(
        canvas: Canvas,
        centerX: Float,
        centerY: Float,
        bearingDegrees: Float,
        mapOrientation: Float,
        driving: Boolean,
        animationTimeMs: Long = System.currentTimeMillis(),
    ) {
        DriverRadarPulse.drawOnCanvas(canvas, centerX, centerY, animationTimeMs)
        if (driving) {
            drawTruck(canvas, centerX, centerY, bearingDegrees, mapOrientation)
        } else {
            val dot = Paint(Paint.ANTI_ALIAS_FLAG).apply {
                color = Color.parseColor("#15803D")
                style = Paint.Style.FILL
            }
            val ring = Paint(Paint.ANTI_ALIAS_FLAG).apply {
                color = Color.WHITE
                style = Paint.Style.STROKE
                strokeWidth = 4f
            }
            canvas.drawCircle(centerX, centerY, 11f, dot)
            canvas.drawCircle(centerX, centerY, 11f, ring)
        }
    }

    private fun drawLocationGlow(canvas: Canvas, cx: Float, cy: Float) {
        val outer = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.parseColor("#4022C55E")
            style = Paint.Style.FILL
        }
        val mid = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.parseColor("#6622C55E")
            style = Paint.Style.FILL
        }
        val inner = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.parseColor("#9922C55E")
            style = Paint.Style.FILL
        }
        canvas.drawCircle(cx, cy, 42f, outer)
        canvas.drawCircle(cx, cy, 28f, mid)
        canvas.drawCircle(cx, cy, 16f, inner)
    }

    private fun drawTruck(canvas: Canvas, cx: Float, cy: Float, bearing: Float, mapOrientation: Float) {
        val body = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.parseColor("#15803D")
            style = Paint.Style.FILL
        }
        val cab = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.parseColor("#14532D")
            style = Paint.Style.FILL
        }
        val stroke = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.WHITE
            style = Paint.Style.STROKE
            strokeWidth = 4f
        }

        val trailer = Path().apply {
            addRoundRect(RectF(-14f, -8f, 14f, 28f), 4f, 4f, Path.Direction.CW)
        }
        val cabPath = Path().apply {
            addRoundRect(RectF(-12f, -26f, 12f, -6f), 3f, 3f, Path.Direction.CW)
        }
        val matrix = Matrix().apply {
            postRotate(bearing - mapOrientation)
            postTranslate(cx, cy)
        }
        trailer.transform(matrix)
        cabPath.transform(Matrix().apply { set(matrix) })

        canvas.drawPath(trailer, body)
        canvas.drawPath(cabPath, cab)
        canvas.drawPath(trailer, stroke)
        canvas.drawPath(cabPath, stroke)
    }
}
