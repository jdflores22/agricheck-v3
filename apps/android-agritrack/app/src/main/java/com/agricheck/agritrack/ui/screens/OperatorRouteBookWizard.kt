package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import com.agricheck.agritrack.data.api.OsrmClient
import com.agricheck.agritrack.data.api.RoutingClient
import com.agricheck.agritrack.data.model.BookOperatorDeliveryStopsRequest
import com.agricheck.agritrack.data.model.ContainerListItemDto
import com.agricheck.agritrack.data.repository.OperatorRepository
import com.agricheck.agritrack.ui.components.AgriOutlinedField
import com.agricheck.agritrack.ui.components.AgriPrimaryButton
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.util.MapsConfig
import com.agricheck.agritrack.util.RouteTrafficStyle
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import com.google.android.gms.maps.model.CameraPosition
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.LatLngBounds
import com.google.maps.android.compose.*
import kotlinx.coroutines.launch
import org.osmdroid.events.MapEventsReceiver
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.MapEventsOverlay
import org.osmdroid.views.overlay.Marker as OsmMarker

private enum class RouteBookWizardStep { Pickup, Warehouse, Overview }

private val PickupGreen = Color(0xFF15803D)
private val WarehouseOrange = Color(0xFFF97316)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OperatorRouteBookWizard(
    repository: OperatorRepository,
    onBack: () -> Unit,
    fixedContainerUuid: String? = null,
    fixedContainerNumber: String? = null,
    saveContainerChoices: List<ContainerListItemDto> = emptyList(),
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val useGoogle = MapsConfig.useGoogleMapTilesInApp(context) ||
        MapsConfig.useGoogleMapDuringNavigation(context)

    var step by remember { mutableStateOf(RouteBookWizardStep.Pickup) }
    var loading by remember { mutableStateOf(fixedContainerUuid != null) }
    var saving by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    var message by remember { mutableStateOf<String?>(null) }

    var pickupLabel by remember { mutableStateOf("Pickup point") }
    var warehouseLabel by remember { mutableStateOf("Warehouse") }
    var pickupLat by remember { mutableStateOf(14.5862) }
    var pickupLng by remember { mutableStateOf(120.9681) }
    var warehouseLat by remember { mutableStateOf(14.5547) }
    var warehouseLng by remember { mutableStateOf(120.9872) }
    var pickupPinned by remember { mutableStateOf(false) }
    var warehousePinned by remember { mutableStateOf(false) }

    var saveTarget by remember { mutableStateOf<ContainerListItemDto?>(null) }
    var expandedContainerMenu by remember { mutableStateOf(false) }

    var route by remember { mutableStateOf<com.agricheck.agritrack.data.api.OsrmRouteResult?>(null) }
    var routeLoading by remember { mutableStateOf(false) }

    LaunchedEffect(fixedContainerUuid) {
        val uuid = fixedContainerUuid ?: return@LaunchedEffect
        loading = true
        runCatching { repository.getDeliveryStops(uuid) }
            .onSuccess { stops ->
                pickupLabel = stops.pickup.name
                pickupLat = stops.pickup.latitude
                pickupLng = stops.pickup.longitude
                pickupPinned = true
                warehouseLabel = stops.destination.name
                warehouseLat = stops.destination.latitude
                warehouseLng = stops.destination.longitude
                warehousePinned = true
            }
            .onFailure { error = it.message }
        loading = false
    }

    LaunchedEffect(step) {
        if (step != RouteBookWizardStep.Overview) return@LaunchedEffect
        routeLoading = true
        runCatching {
            RoutingClient.fetchDrivingRoute(
                pickupLat,
                pickupLng,
                warehouseLat,
                warehouseLng,
                onQuickRoute = null,
            )
        }.onSuccess { route = it }
            .onFailure { route = null }
        routeLoading = false
    }

    val cameraPositionState = rememberCameraPositionState {
        position = CameraPosition.fromLatLngZoom(LatLng(pickupLat, pickupLng), 11f)
    }

    LaunchedEffect(step, pickupLat, pickupLng, warehouseLat, warehouseLng, useGoogle) {
        if (!useGoogle) return@LaunchedEffect
        when (step) {
            RouteBookWizardStep.Pickup -> {
                cameraPositionState.move(CameraUpdateFactory.newLatLngZoom(LatLng(pickupLat, pickupLng), 14f))
            }
            RouteBookWizardStep.Warehouse, RouteBookWizardStep.Overview -> {
                val bounds = LatLngBounds.builder()
                    .include(LatLng(pickupLat, pickupLng))
                    .include(LatLng(warehouseLat, warehouseLng))
                    .build()
                cameraPositionState.move(CameraUpdateFactory.newLatLngBounds(bounds, 100))
            }
        }
    }

    val stepIndex = when (step) {
        RouteBookWizardStep.Pickup -> 1
        RouteBookWizardStep.Warehouse -> 2
        RouteBookWizardStep.Overview -> 3
    }

    val subtitle = fixedContainerNumber ?: saveTarget?.containerNumber

    Column(Modifier.fillMaxSize()) {
        Row(
            Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            TextButton(onClick = {
                when (step) {
                    RouteBookWizardStep.Pickup -> onBack()
                    RouteBookWizardStep.Warehouse -> step = RouteBookWizardStep.Pickup
                    RouteBookWizardStep.Overview -> step = RouteBookWizardStep.Warehouse
                }
            }) {
                Text(UiStrings.Map.back)
            }
            Column(Modifier.weight(1f), horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    when (step) {
                        RouteBookWizardStep.Pickup -> UiStrings.OperatorRouteBooking.wizardPickupTitle
                        RouteBookWizardStep.Warehouse -> UiStrings.OperatorRouteBooking.wizardWarehouseTitle
                        RouteBookWizardStep.Overview -> UiStrings.OperatorRouteBooking.wizardOverviewTitle
                    },
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp,
                )
                subtitle?.let {
                    Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                }
            }
            Text("$stepIndex/3", fontWeight = FontWeight.Bold, color = AgriColors.Primary)
        }

        LinearProgressIndicator(
            progress = { stepIndex / 3f },
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp),
            color = AgriColors.Primary,
        )

        if (loading) {
            LinearProgressIndicator(Modifier.fillMaxWidth(), color = AgriColors.Primary)
        }

        Text(
            when (step) {
                RouteBookWizardStep.Pickup -> UiStrings.OperatorRouteBooking.wizardPickupHint
                RouteBookWizardStep.Warehouse -> UiStrings.OperatorRouteBooking.wizardWarehouseHint
                RouteBookWizardStep.Overview -> UiStrings.OperatorRouteBooking.wizardOverviewHint
            },
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
            style = MaterialTheme.typography.bodySmall,
            color = AgriColors.TextSecondary,
        )

        Box(
            Modifier
                .weight(1f)
                .fillMaxWidth()
                .padding(horizontal = 16.dp),
        ) {
            if (useGoogle) {
                val routeChunks = remember(route, step) {
                    if (step != RouteBookWizardStep.Overview) return@remember emptyList()
                    val r = route
                    if (r == null || r.points.size < 2) emptyList()
                    else {
                        val full = r.points.map { LatLng(it.first, it.second) }
                        RouteTrafficStyle.buildMergedChunks(full, 0, r.trafficIntervals, r.trafficSegmentDetails)
                    }
                }
                GoogleMap(
                    modifier = Modifier.fillMaxSize(),
                    cameraPositionState = cameraPositionState,
                    properties = MapProperties(mapType = MapType.NORMAL),
                    uiSettings = MapUiSettings(zoomControlsEnabled = true),
                    onMapClick = { latLng ->
                        when (step) {
                            RouteBookWizardStep.Pickup -> {
                                pickupLat = latLng.latitude
                                pickupLng = latLng.longitude
                                pickupPinned = true
                            }
                            RouteBookWizardStep.Warehouse -> {
                                warehouseLat = latLng.latitude
                                warehouseLng = latLng.longitude
                                warehousePinned = true
                            }
                            RouteBookWizardStep.Overview -> Unit
                        }
                    },
                ) {
                    if (step != RouteBookWizardStep.Pickup || pickupPinned) {
                        Marker(
                            state = MarkerState(LatLng(pickupLat, pickupLng)),
                            title = pickupLabel,
                            icon = BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_GREEN),
                        )
                    }
                    if (step != RouteBookWizardStep.Pickup && (warehousePinned || step == RouteBookWizardStep.Overview)) {
                        Marker(
                            state = MarkerState(LatLng(warehouseLat, warehouseLng)),
                            title = warehouseLabel,
                            icon = BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_ORANGE),
                        )
                    }
                    if (step == RouteBookWizardStep.Overview) {
                        if (routeChunks.isNotEmpty()) {
                            routeChunks.forEach { chunk ->
                                Polyline(
                                    points = chunk.points,
                                    color = RouteTrafficStyle.speedColor(chunk.speed),
                                    width = 14f,
                                    zIndex = 3f,
                                )
                            }
                        } else {
                            route?.points?.takeIf { it.size >= 2 }?.let { pts ->
                                Polyline(
                                    points = pts.map { LatLng(it.first, it.second) },
                                    color = Color(0xFF4285F4),
                                    width = 12f,
                                )
                            }
                        }
                    }
                }
            } else {
                OsmWizardMap(
                    step = step,
                    pickupLat = pickupLat,
                    pickupLng = pickupLng,
                    warehouseLat = warehouseLat,
                    warehouseLng = warehouseLng,
                    pickupLabel = pickupLabel,
                    warehouseLabel = warehouseLabel,
                    pickupPinned = pickupPinned,
                    warehousePinned = warehousePinned,
                    onPin = { lat, lng ->
                        when (step) {
                            RouteBookWizardStep.Pickup -> {
                                pickupLat = lat
                                pickupLng = lng
                                pickupPinned = true
                            }
                            RouteBookWizardStep.Warehouse -> {
                                warehouseLat = lat
                                warehouseLng = lng
                                warehousePinned = true
                            }
                            else -> Unit
                        }
                    },
                )
            }
        }

        Surface(shadowElevation = 8.dp, color = Color.White) {
            Column(
                Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
                message?.let { Text(it, color = AgriColors.Success, style = MaterialTheme.typography.bodySmall) }

                when (step) {
                    RouteBookWizardStep.Pickup -> {
                        AgriOutlinedField(pickupLabel, { pickupLabel = it }, UiStrings.OperatorRouteBooking.pickupLabel)
                        AgriPrimaryButton(
                            text = UiStrings.OperatorRouteBooking.wizardNext,
                            onClick = {
                                if (!pickupPinned) {
                                    error = UiStrings.OperatorRouteBooking.wizardPinPickupFirst
                                    return@AgriPrimaryButton
                                }
                                error = null
                                step = RouteBookWizardStep.Warehouse
                            },
                        )
                    }
                    RouteBookWizardStep.Warehouse -> {
                        AgriOutlinedField(warehouseLabel, { warehouseLabel = it }, UiStrings.OperatorRouteBooking.warehouseLabel)
                        AgriPrimaryButton(
                            text = UiStrings.OperatorRouteBooking.wizardNext,
                            onClick = {
                                if (!warehousePinned) {
                                    error = UiStrings.OperatorRouteBooking.wizardPinWarehouseFirst
                                    return@AgriPrimaryButton
                                }
                                error = null
                                step = RouteBookWizardStep.Overview
                            },
                        )
                    }
                    RouteBookWizardStep.Overview -> {
                        if (routeLoading) {
                            LinearProgressIndicator(Modifier.fillMaxWidth(), color = AgriColors.Primary)
                        }
                        route?.let { r ->
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Text(
                                    OsrmClient.formatDurationMinutes(r.etaDurationSeconds()),
                                    fontWeight = FontWeight.Bold,
                                    fontSize = 22.sp,
                                )
                                Spacer(Modifier.width(8.dp))
                                Text(OsrmClient.formatDistance(r.distanceMeters), color = AgriColors.TextSecondary)
                            }
                        }
                        Text(
                            "${UiStrings.OperatorRouteBooking.pickupLabel}: $pickupLabel\n${"%.5f".format(pickupLat)}, ${"%.5f".format(pickupLng)}",
                            style = MaterialTheme.typography.bodySmall,
                        )
                        Text(
                            "${UiStrings.OperatorRouteBooking.warehouseLabel}: $warehouseLabel\n${"%.5f".format(warehouseLat)}, ${"%.5f".format(warehouseLng)}",
                            style = MaterialTheme.typography.bodySmall,
                        )

                        val saveUuid = fixedContainerUuid ?: saveTarget?.uuid
                        if (fixedContainerUuid == null && saveContainerChoices.isNotEmpty()) {
                            ExposedDropdownMenuBox(
                                expanded = expandedContainerMenu,
                                onExpandedChange = { expandedContainerMenu = it },
                            ) {
                                OutlinedTextField(
                                    value = saveTarget?.containerNumber ?: UiStrings.Operator.manualTestPickContainer,
                                    onValueChange = {},
                                    readOnly = true,
                                    modifier = Modifier.fillMaxWidth().menuAnchor(),
                                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expandedContainerMenu) },
                                )
                                ExposedDropdownMenu(
                                    expanded = expandedContainerMenu,
                                    onDismissRequest = { expandedContainerMenu = false },
                                ) {
                                    saveContainerChoices.forEach { c ->
                                        DropdownMenuItem(
                                            text = { Text("${c.containerNumber} · ${c.status}") },
                                            onClick = {
                                                saveTarget = c
                                                expandedContainerMenu = false
                                            },
                                        )
                                    }
                                }
                            }
                        }

                        AgriPrimaryButton(
                            text = UiStrings.OperatorRouteBooking.save,
                            onClick = {
                                val uuid = saveUuid
                                if (uuid == null) {
                                    error = UiStrings.OperatorRouteBooking.wizardPickContainerFirst
                                    return@AgriPrimaryButton
                                }
                                scope.launch {
                                    saving = true
                                    error = null
                                    message = null
                                    runCatching {
                                        repository.bookDeliveryStops(
                                            uuid,
                                            BookOperatorDeliveryStopsRequest(
                                                pickupLabel = pickupLabel.trim(),
                                                pickupLatitude = pickupLat,
                                                pickupLongitude = pickupLng,
                                                destinationLabel = warehouseLabel.trim(),
                                                destinationLatitude = warehouseLat,
                                                destinationLongitude = warehouseLng,
                                            ),
                                        )
                                    }.onSuccess {
                                        message = UiStrings.OperatorRouteBooking.saved
                                    }.onFailure {
                                        error = it.message
                                    }
                                    saving = false
                                }
                            },
                            enabled = !saving && saveUuid != null,
                            loading = saving,
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun OsmWizardMap(
    step: RouteBookWizardStep,
    pickupLat: Double,
    pickupLng: Double,
    warehouseLat: Double,
    warehouseLng: Double,
    pickupLabel: String,
    warehouseLabel: String,
    pickupPinned: Boolean,
    warehousePinned: Boolean,
    onPin: (Double, Double) -> Unit,
) {
    AndroidView(
        factory = { ctx ->
            MapView(ctx).also { map ->
                map.setMultiTouchControls(true)
                map.controller.setZoom(11.0)
                map.controller.setCenter(GeoPoint(pickupLat, pickupLng))
                if (step != RouteBookWizardStep.Overview) {
                    val receiver = object : MapEventsReceiver {
                        override fun singleTapConfirmedHelper(p: GeoPoint?): Boolean {
                            p ?: return false
                            onPin(p.latitude, p.longitude)
                            return true
                        }

                        override fun longPressHelper(p: GeoPoint?): Boolean = false
                    }
                    map.overlays.add(0, MapEventsOverlay(receiver))
                }
            }
        },
        modifier = Modifier.fillMaxSize(),
        update = { map ->
            map.overlays.removeAll { it is OsmMarker || it is MapEventsOverlay }
            if (step != RouteBookWizardStep.Overview) {
                val receiver = object : MapEventsReceiver {
                    override fun singleTapConfirmedHelper(p: GeoPoint?): Boolean {
                        p ?: return false
                        onPin(p.latitude, p.longitude)
                        return true
                    }

                    override fun longPressHelper(p: GeoPoint?): Boolean = false
                }
                map.overlays.add(0, MapEventsOverlay(receiver))
            }
            if (pickupPinned || step != RouteBookWizardStep.Pickup) {
                map.overlays.add(
                    OsmMarker(map).apply {
                        position = GeoPoint(pickupLat, pickupLng)
                        title = pickupLabel
                        setAnchor(OsmMarker.ANCHOR_CENTER, OsmMarker.ANCHOR_BOTTOM)
                    },
                )
            }
            if (step != RouteBookWizardStep.Pickup && (warehousePinned || step == RouteBookWizardStep.Overview)) {
                map.overlays.add(
                    OsmMarker(map).apply {
                        position = GeoPoint(warehouseLat, warehouseLng)
                        title = warehouseLabel
                        setAnchor(OsmMarker.ANCHOR_CENTER, OsmMarker.ANCHOR_BOTTOM)
                    },
                )
            }
            map.invalidate()
        },
    )
}
