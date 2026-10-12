package com.agricheck.agritrack.ui.screens

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import com.agricheck.agritrack.data.api.OsrmRouteResult
import com.agricheck.agritrack.data.model.ContainerTrackDto
import com.agricheck.agritrack.util.MapsConfig
import androidx.compose.ui.platform.LocalContext
import com.agricheck.agritrack.map.DriverLocationRadarOnMap
import com.agricheck.agritrack.map.TruckMapIcons
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.MapRouteColors
import com.agricheck.agritrack.util.GeoUtils
import com.agricheck.agritrack.util.resolveDriverLatLng
import com.agricheck.agritrack.util.NavigationGuidance
import com.agricheck.agritrack.util.deliveryPhase
import com.agricheck.agritrack.util.shouldShowPickupToWarehousePreview
import com.agricheck.agritrack.util.TripNavLeg
import com.agricheck.agritrack.util.tripNavLeg
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.LatLngBounds
import com.google.maps.android.compose.GoogleMap
import com.google.maps.android.compose.MapProperties
import com.google.maps.android.compose.MapUiSettings
import com.google.maps.android.compose.Marker
import com.google.maps.android.compose.MarkerState
import com.google.maps.android.compose.Polyline
import com.google.maps.android.compose.rememberCameraPositionState

private val TrailGreen = Color(0xFF4ADE80)

@Composable
fun DeliveryGoogleMap(
    modifier: Modifier = Modifier,
    track: ContainerTrackDto,
    route: OsrmRouteResult?,
    previewWarehouseRoute: OsrmRouteResult?,
    drivingMode: Boolean,
    followCamera: Boolean,
    currentLat: Double?,
    currentLng: Double?,
    bearing: Float,
    recenterToken: Int,
    northUp: Boolean,
    onUserInteracted: () -> Unit,
) {
    val (driverLat, driverLng) = resolveDriverLatLng(currentLat, currentLng, track, drivingMode)
    val leg = tripNavLeg(deliveryPhase(track.status))
    val showWarehousePreview = shouldShowPickupToWarehousePreview(deliveryPhase(track.status), drivingMode)

    val initialLat = driverLat ?: track.currentLatitude ?: track.pickup.latitude
    val initialLng = driverLng ?: track.currentLongitude ?: track.pickup.longitude

    val cameraPositionState = rememberCameraPositionState {
        position = CameraPosition.fromLatLngZoom(LatLng(initialLat, initialLng), 15f)
    }

    val pickupState = remember(track.pickup) {
        MarkerState(LatLng(track.pickup.latitude, track.pickup.longitude))
    }
    val destinationState = remember(track.destination) {
        MarkerState(LatLng(track.destination.latitude, track.destination.longitude))
    }
    val driverState = remember {
        MarkerState(LatLng(initialLat, initialLng))
    }

    LaunchedEffect(route?.points, previewWarehouseRoute?.points, drivingMode, recenterToken, driverLat, driverLng) {
        if (drivingMode) return@LaunchedEffect
        val builder = LatLngBounds.builder()
        builder.include(LatLng(track.pickup.latitude, track.pickup.longitude))
        builder.include(LatLng(track.destination.latitude, track.destination.longitude))
        previewWarehouseRoute?.points?.forEach { (lat, lng) -> builder.include(LatLng(lat, lng)) }
        route?.points?.forEach { (lat, lng) -> builder.include(LatLng(lat, lng)) }
        if (driverLat != null && driverLng != null) {
            builder.include(LatLng(driverLat, driverLng))
        }
        runCatching {
            cameraPositionState.animate(CameraUpdateFactory.newLatLngBounds(builder.build(), 140))
        }
    }

    LaunchedEffect(drivingMode, followCamera, recenterToken, northUp, driverLat, driverLng, bearing, route?.points) {
        if (!drivingMode || !followCamera) return@LaunchedEffect
        if (driverLat == null || driverLng == null) return@LaunchedEffect
        driverState.position = LatLng(driverLat, driverLng)
        val points = route?.points ?: emptyList()
        val lookAhead = points.dropWhile { (lat, lng) ->
            GeoUtils.distanceMeters(driverLat, driverLng, lat, lng) > 80
        }.drop(1).firstOrNull()
        val heading = if (bearing > 1f) {
            bearing
        } else if (lookAhead != null) {
            NavigationGuidance.bearingDegrees(driverLat, driverLng, lookAhead.first, lookAhead.second)
        } else {
            bearing
        }
        val offset = NavigationGuidance.offsetAlongBearing(driverLat, driverLng, heading, 110.0)
        val camBearing = if (northUp) 0f else heading
        val tilt = if (northUp) 0f else 58f
        cameraPositionState.animate(
            CameraUpdateFactory.newCameraPosition(
                CameraPosition.Builder()
                    .target(LatLng(offset.first, offset.second))
                    .zoom(18.5f)
                    .bearing(camBearing)
                    .tilt(tilt)
                    .build(),
            ),
        )
    }

    LaunchedEffect(driverLat, driverLng) {
        if (driverLat != null && driverLng != null) {
            driverState.position = LatLng(driverLat, driverLng)
        }
    }

    val pickupIcon = remember { BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_GREEN) }
    val warehouseIcon = remember { BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_ORANGE) }
    val mapContext = LocalContext.current
    val showTraffic = drivingMode && MapsConfig.useGoogleMapTilesInApp(mapContext)

    GoogleMap(
        modifier = modifier,
        cameraPositionState = cameraPositionState,
        properties = MapProperties(
            isMyLocationEnabled = false,
            isTrafficEnabled = showTraffic,
        ),
        uiSettings = MapUiSettings(
            zoomControlsEnabled = false,
            myLocationButtonEnabled = false,
            compassEnabled = false,
        ),
        onMapClick = { onUserInteracted() },
    ) {
        if (showWarehousePreview) {
            previewWarehouseRoute?.points?.takeIf { it.size >= 2 }?.let { pts ->
                val latLngs = pts.map { LatLng(it.first, it.second) }
                Polyline(points = latLngs, color = MapRouteColors.upcomingCasing, width = 16f, zIndex = 0f)
                Polyline(points = latLngs, color = MapRouteColors.upcoming, width = 10f, zIndex = 1f)
            }
        }

        route?.points?.takeIf { it.size >= 2 }?.let { pts ->
            val latLngs = pts.map { LatLng(it.first, it.second) }
            Polyline(points = latLngs, color = MapRouteColors.primaryCasing, width = 28f, zIndex = 2f)
            Polyline(points = latLngs, color = MapRouteColors.primary, width = 20f, zIndex = 3f)
        }

        if (!drivingMode && track.trail.isNotEmpty()) {
            Polyline(
                points = track.trail.map { LatLng(it.latitude, it.longitude) },
                color = TrailGreen,
                width = 6f,
            )
        }

        Marker(
            state = pickupState,
            title = track.pickup.name,
            snippet = "Pickup",
            icon = pickupIcon,
            alpha = if (leg == TripNavLeg.ToPickup) 1f else 0.7f,
            zIndex = 5f,
        )
        Marker(
            state = destinationState,
            title = track.destination.name,
            snippet = "Warehouse",
            icon = warehouseIcon,
            alpha = if (leg == TripNavLeg.ToPickup) 0.65f else 1f,
            zIndex = 4f,
        )

        if (driverLat != null && driverLng != null) {
            val driverPos = LatLng(driverLat, driverLng)
            DriverLocationRadarOnMap(center = driverPos, zIndexBase = 6f)
            if (drivingMode) {
                val heading = bearing
                Marker(
                    state = driverState,
                    title = UiStrings.Map.you,
                    rotation = heading,
                    flat = true,
                    icon = TruckMapIcons.createBitmapDescriptor(heading),
                    zIndex = 8f,
                )
            }
        }
    }
}
