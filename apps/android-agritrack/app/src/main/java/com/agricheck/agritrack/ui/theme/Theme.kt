package com.agricheck.agritrack.ui.theme

import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

object AgriColors {
    val Primary = Color(0xFF22C55E)
    val PrimaryDark = Color(0xFF16A34A)
    val Background = Color(0xFFF4FBF6)
    val Surface = Color(0xFFFFFFFF)
    val SurfaceMuted = Color(0xFFEEF8F1)
    val OnSurface = Color(0xFF111827)
    val TextSecondary = Color(0xFF6B7280)
    val Divider = Color(0xFFE8EEEA)
    val Success = Color(0xFF16A34A)
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
