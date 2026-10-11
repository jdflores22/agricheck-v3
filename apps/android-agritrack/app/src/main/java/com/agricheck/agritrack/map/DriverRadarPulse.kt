package com.agricheck.agritrack.map

import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import com.agricheck.agritrack.ui.theme.AgriColors
import com.google.android.gms.maps.model.LatLng
import com.google.maps.android.compose.Circle
import kotlin.math.cos
import kotlin.math.sin

object DriverRadarPulse {
    private const val RING_COUNT = 3
    private const val PERIOD_MS = 2_800L
    private const val MAX_RADIUS_PX = 58f
    private const val MIN_RADIUS_PX = 14f

    fun drawOnCanvas(canvas: Canvas, centerX: Float, centerY: Float, nowMs: Long) {
        for (i in 0 until RING_COUNT) {
            val offset = (PERIOD_MS / RING_COUNT) * i
            val phase = ((nowMs + offset) % PERIOD_MS) / PERIOD_MS.toFloat()
            val radius = MIN_RADIUS_PX + (MAX_RADIUS_PX - MIN_RADIUS_PX) * phase
            val alpha = (1f - phase).coerceIn(0f, 1f)

            val fill = Paint(Paint.ANTI_ALIAS_FLAG).apply {
                color = Color.argb((alpha * 55).toInt(), 34, 197, 94)
                style = Paint.Style.FILL
            }
            canvas.drawCircle(centerX, centerY, radius, fill)

            val ring = Paint(Paint.ANTI_ALIAS_FLAG).apply {
                color = Color.argb((alpha * 200).toInt(), 21, 128, 61)
                style = Paint.Style.STROKE
                strokeWidth = 2.5f
            }
            canvas.drawCircle(centerX, centerY, radius, ring)
        }

        val sweepPhase = (nowMs % PERIOD_MS) / PERIOD_MS.toFloat()
        val sweepAngle = sweepPhase * 360f
        val sweep = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.argb(70, 34, 197, 94)
            style = Paint.Style.FILL
        }
        val bounds = RectF(
            centerX - MAX_RADIUS_PX,
            centerY - MAX_RADIUS_PX,
            centerX + MAX_RADIUS_PX,
            centerY + MAX_RADIUS_PX,
        )
        canvas.drawArc(bounds, sweepAngle - 90f, 42f, true, sweep)

        val beamEndX = centerX + cos(Math.toRadians(sweepAngle.toDouble() - 90.0)).toFloat() * MAX_RADIUS_PX * 0.92f
        val beamEndY = centerY + sin(Math.toRadians(sweepAngle.toDouble() - 90.0)).toFloat() * MAX_RADIUS_PX * 0.92f
        val beam = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.argb(160, 21, 128, 61)
            style = Paint.Style.STROKE
            strokeWidth = 3f
        }
        canvas.drawLine(centerX, centerY, beamEndX, beamEndY, beam)

        val core = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.parseColor("#15803D")
            style = Paint.Style.FILL
        }
        val coreRing = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.WHITE
            style = Paint.Style.STROKE
            strokeWidth = 3f
        }
        canvas.drawCircle(centerX, centerY, 10f, core)
        canvas.drawCircle(centerX, centerY, 10f, coreRing)
    }
}

@Composable
fun DriverLocationRadarOnMap(
    center: LatLng,
    zIndexBase: Float = 6f,
) {
    val transition = rememberInfiniteTransition(label = "driverRadar")
    val rings = List(3) { index ->
        val phase by transition.animateFloat(
            initialValue = 0f,
            targetValue = 1f,
            animationSpec = infiniteRepeatable(
                animation = tween(2_800, easing = LinearEasing, delayMillis = index * 900),
                repeatMode = RepeatMode.Restart,
            ),
            label = "ring$index",
        )
        phase
    }
    val sweep by transition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(2_800, easing = LinearEasing),
            repeatMode = RepeatMode.Restart,
        ),
        label = "sweep",
    )

    val strokeGreen = AgriColors.PrimaryDark
    val fillGreen = AgriColors.Primary

    rings.forEachIndexed { index, phase ->
        val radius = 12.0 + phase * 48.0
        val alpha = (1f - phase).coerceIn(0.05f, 1f)
        Circle(
            center = center,
            radius = radius,
            fillColor = fillGreen.copy(alpha = 0.08f * alpha),
            strokeColor = strokeGreen.copy(alpha = 0.35f * alpha),
            strokeWidth = 2f,
            zIndex = zIndexBase + index * 0.1f,
        )
    }

    Circle(
        center = center,
        radius = 8.0 + sweep * 6.0,
        fillColor = fillGreen.copy(alpha = 0.35f),
        strokeColor = strokeGreen,
        strokeWidth = 3f,
        zIndex = zIndexBase + 3f,
    )
}
