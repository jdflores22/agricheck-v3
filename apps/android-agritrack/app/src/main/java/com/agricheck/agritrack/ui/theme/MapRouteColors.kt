package com.agricheck.agritrack.ui.theme

import androidx.compose.ui.graphics.Color

object MapRouteColors {
    /** Strong: driver → pickup (una), o driver → warehouse pag nakuha na ang container */
    val primary = Color(0xFF15803D)
    val primaryCasing = Color(0xFF14532D)
    const val primaryHex = "#15803D"
    const val primaryCasingHex = "#14532D"

    /** Light: pickup → warehouse hanggang hindi pa naka-pickup */
    val upcoming = Color(0xFF86EFAC)
    val upcomingCasing = Color(0xFFBBF7D0)
    const val upcomingHex = "#86EFAC"
    const val upcomingCasingHex = "#BBF7D0"

    val primaryStrokeOsm = 28f
    val primaryCasingOsm = 40f
    val upcomingStrokeOsm = 12f
    val upcomingCasingOsm = 18f
}
