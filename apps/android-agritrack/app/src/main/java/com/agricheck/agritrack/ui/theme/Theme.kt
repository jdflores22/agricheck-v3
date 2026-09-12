package com.agricheck.agritrack.ui.theme

import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

object AgriColors {
    val Primary = Color(0xFF166534)
    val PrimaryDark = Color(0xFF14532D)
    val Background = Color(0xFFF5F7FA)
    val Surface = Color(0xFFFFFFFF)
    val SurfaceMuted = Color(0xFFEEF2F7)
    val OnSurface = Color(0xFF0F172A)
    val TextSecondary = Color(0xFF64748B)
    val Divider = Color(0xFFE2E8F0)
    val Success = Color(0xFF2E7D32)
    val Warning = Color(0xFFED6C02)
    val Error = Color(0xFFC62828)
}

private val LightColors = lightColorScheme(
    primary = AgriColors.Primary,
    onPrimary = Color.White,
    primaryContainer = Color(0xFFDCFCE7),
    background = AgriColors.Background,
    surface = AgriColors.Surface,
    onSurface = AgriColors.OnSurface,
    error = AgriColors.Error,
)

private val AppShapes = Shapes(
    extraSmall = RoundedCornerShape(8.dp),
    small = RoundedCornerShape(10.dp),
    medium = RoundedCornerShape(12.dp),
    large = RoundedCornerShape(16.dp),
    extraLarge = RoundedCornerShape(24.dp),
)

@Composable
fun AgriTrackTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColors,
        shapes = AppShapes,
        content = content,
    )
}
