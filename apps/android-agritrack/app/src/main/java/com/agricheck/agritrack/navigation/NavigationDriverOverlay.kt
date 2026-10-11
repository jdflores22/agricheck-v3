package com.agricheck.agritrack.navigation

import android.graphics.Canvas
import com.agricheck.agritrack.map.TruckMapIcons
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Overlay

class NavigationDriverOverlay : Overlay() {
    var latitude: Double = 0.0
    var longitude: Double = 0.0
    var bearingDegrees: Float = 0f
    var showTruckIcon: Boolean = true

    override fun draw(canvas: Canvas, mapView: MapView, shadow: Boolean) {
        if (shadow) return
        val projection = mapView.projection
        val point = projection.toPixels(GeoPoint(latitude, longitude), null)
        TruckMapIcons.drawDriverLocation(
            canvas,
            point.x.toFloat(),
            point.y.toFloat(),
            bearingDegrees,
            mapView.mapOrientation,
            driving = showTruckIcon,
            animationTimeMs = System.currentTimeMillis(),
        )
        mapView.postInvalidateOnAnimation()
    }
}
