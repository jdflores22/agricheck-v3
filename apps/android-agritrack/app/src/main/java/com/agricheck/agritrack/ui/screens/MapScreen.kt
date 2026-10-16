package com.agricheck.agritrack.ui.screens

import android.Manifest
import android.annotation.SuppressLint
import android.content.pm.PackageManager
import android.location.Location
import android.os.Looper
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Flag
import androidx.compose.material.icons.filled.SubdirectoryArrowLeft
import androidx.compose.material.icons.filled.SubdirectoryArrowRight
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Navigation
import androidx.compose.material.icons.filled.NearMe
import androidx.compose.material.icons.automirrored.filled.VolumeOff
import androidx.compose.material.icons.automirrored.filled.VolumeUp
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import com.agricheck.agritrack.ui.camera.GeotagCaptureScreen
import java.time.Instant
import com.agricheck.agritrack.data.api.OsrmClient
import com.agricheck.agritrack.data.api.OsrmRouteResult
import com.agricheck.agritrack.data.api.RoutingClient
import com.agricheck.agritrack.data.model.ContainerTrackDto
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.data.local.PersistedNavigationSession
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.navigation.NavigationDriverOverlay
import com.agricheck.agritrack.navigation.NavigationManeuverNotificationIcons
import com.agricheck.agritrack.navigation.NavigationNotificationState
import com.agricheck.agritrack.navigation.NavigationPromptTracker
import com.agricheck.agritrack.navigation.NavigationVoiceAnnouncer
import com.agricheck.agritrack.data.api.TrafficSegmentDetail
import com.agricheck.agritrack.data.local.MapDisplaySettings
import com.agricheck.agritrack.ui.components.AgriScreenTopBar
import com.agricheck.agritrack.ui.components.LoadingState
import com.agricheck.agritrack.location.AppLocationManager
import com.agricheck.agritrack.location.DriverLocationSync
import com.agricheck.agritrack.location.DriverTrackingPolicy
import com.agricheck.agritrack.location.LocationSubscription
import com.agricheck.agritrack.location.LocationTrackingService
import com.agricheck.agritrack.location.meetsNavigationAccuracy
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.ui.theme.MapRouteColors
import com.agricheck.agritrack.util.*
import com.google.android.gms.maps.model.LatLng
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume
import org.osmdroid.tileprovider.tilesource.TileSourceFactory
import org.osmdroid.util.BoundingBox
import org.osmdroid.util.GeoPoint
import org.osmdroid.events.MapListener
import org.osmdroid.events.ScrollEvent
import org.osmdroid.events.ZoomEvent
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker
import org.osmdroid.views.overlay.Polyline

private val RouteGreen = Color(0xFF15803D)

private sealed interface PendingGeotagCapture {
    data class Pickup(val latitude: Double, val longitude: Double, val label: String?) : PendingGeotagCapture
    data class Warehouse(val latitude: Double, val longitude: Double) : PendingGeotagCapture
}
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MapScreen(
    containerUuid: String,
    containerNumber: String,
    repository: DriverRepository,
    onBack: () -> Unit,
    onOpenQr: () -> Unit,
) {
    val context = LocalContext.current
    val appContainer = (context.applicationContext as AgriTrackApp).container
    val navSessionStore = appContainer.mapNavigationSessionStore
    val restoredSession = remember(containerUuid) { navSessionStore.get(containerUuid) }
    val scope = rememberCoroutineScope()
    var drivingMode by remember(containerUuid) {
        mutableStateOf(restoredSession?.drivingMode == true)
    }
    val useGoogleMaps = MapsConfig.useGoogleMapTilesInApp(context) ||
        MapsConfig.useGoogleMapDuringNavigation(context)
    var recenterToken by remember { mutableIntStateOf(0) }
    var track by remember { mutableStateOf<ContainerTrackDto?>(null) }
    var route by remember { mutableStateOf<OsrmRouteResult?>(null) }
    /** Preview: pickup → warehouse (light green), only before container is picked up */
    var previewWarehouseRoute by remember { mutableStateOf<OsrmRouteResult?>(null) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var followCamera by remember(containerUuid) {
        mutableStateOf(restoredSession?.drivingMode == true)
    }
    var currentLat by remember { mutableStateOf<Double?>(null) }
    var currentLng by remember { mutableStateOf<Double?>(null) }
    var bearing by remember { mutableStateOf(0f) }
    var voiceEnabled by remember(containerUuid) {
        mutableStateOf(restoredSession?.voiceEnabled ?: true)
    }
    var pickupBusy by remember { mutableStateOf(false) }
    var arriveBusy by remember { mutableStateOf(false) }
    var pendingGeotag by remember { mutableStateOf<PendingGeotagCapture?>(null) }
    var geotagAfterCamera by remember { mutableStateOf<PendingGeotagCapture?>(null) }
    var actionMessage by remember { mutableStateOf<String?>(null) }
    var routeLoading by remember { mutableStateOf(false) }
    var navigationBootstrapping by remember { mutableStateOf(false) }
    var mapDisplaySettings by remember { mutableStateOf(MapDisplaySettings()) }
    var showMapSettings by remember { mutableStateOf(false) }
    var selectedTrafficSegment by remember { mutableStateOf<TrafficSegmentDetail?>(null) }
    var lastRouteRerouteAtMs by remember { mutableLongStateOf(0L) }
    var lastComplianceReportAtMs by remember { mutableLongStateOf(0L) }
    var routeComplianceBlocked by remember { mutableStateOf(false) }
    var northUp by remember(containerUuid) {
        mutableStateOf(restoredSession?.northUp ?: false)
    }
    val voiceAnnouncer = remember { NavigationVoiceAnnouncer(context) }

    LaunchedEffect(Unit) {
        mapDisplaySettings = appContainer.settingsStore.getMapDisplaySettings()
        if (!mapDisplaySettings.showDirections) {
            voiceEnabled = false
        }
        voiceAnnouncer.muted = !mapDisplaySettings.showDirections || !voiceEnabled
    }

    LaunchedEffect(mapDisplaySettings.showDirections, voiceEnabled) {
        voiceAnnouncer.muted = !mapDisplaySettings.showDirections || !voiceEnabled
    }
    val promptTracker = remember { NavigationPromptTracker() }
    val mapRef = remember { mutableStateOf<MapView?>(null) }
    val onUserPannedMap = remember { mutableStateOf<() -> Unit>({}) }
    onUserPannedMap.value = { followCamera = false }
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { }
    val cameraPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { granted ->
        if (granted) {
            pendingGeotag = geotagAfterCamera
        } else {
            actionMessage = UiStrings.Permissions.cameraRequired
        }
        geotagAfterCamera = null
    }

    fun openGeotagCapture(pending: PendingGeotagCapture) {
        when (ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA)) {
            PackageManager.PERMISSION_GRANTED -> pendingGeotag = pending
            else -> {
                geotagAfterCamera = pending
                cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
            }
        }
    }

    DisposableEffect(Unit) {
        onDispose { voiceAnnouncer.shutdown() }
    }

    LaunchedEffect(drivingMode, northUp, voiceEnabled, containerUuid) {
        navSessionStore.update(containerUuid, drivingMode, northUp, voiceEnabled)
    }

    LaunchedEffect(containerUuid) {
        DriverLocationSync(context).flushPendingUploads()
    }

    LaunchedEffect(containerUuid) {
        if (drivingMode) return@LaunchedEffect
        val persisted = appContainer.trackingStore.getNavigationSession()
        if (persisted?.containerUuid == containerUuid) {
            drivingMode = true
            followCamera = true
            northUp = false
        }
    }

    val towardName = remember(track) {
        track?.primaryRouteTarget()?.name ?: ""
    }

    LaunchedEffect(drivingMode, towardName, containerUuid, containerNumber) {
        if (!drivingMode) return@LaunchedEffect
        appContainer.trackingStore.setNavigationSession(
            PersistedNavigationSession(
                containerUuid = containerUuid,
                containerNumber = containerNumber,
                destinationLabel = towardName,
            ),
        )
        LocationTrackingService.startNavigation(
            context,
            containerUuid,
            containerNumber,
            towardName,
        )
    }

    LaunchedEffect(towardName, drivingMode) {
        if (!drivingMode || towardName.isBlank()) return@LaunchedEffect
        LocationTrackingService.updateNavigation(
            context,
            containerUuid,
            containerNumber,
            towardName,
        )
    }

    LaunchedEffect(containerUuid) {
        if (drivingMode) return@LaunchedEffect
        val activeUuid = appContainer.trackingStore.getActiveContainerUuid()
        if (activeUuid == containerUuid) {
            drivingMode = true
            followCamera = true
            northUp = false
        }
    }

    val phase = track?.status?.let { deliveryPhase(it) }

    val navLeg = phase?.let { tripNavLeg(it) }

    val startButtonSub = when (navLeg) {
        TripNavLeg.ToPickup -> UiStrings.Map.startToPickupSub
        TripNavLeg.ToDestination -> UiStrings.Map.startToWarehouseSub
        null -> UiStrings.Map.startToPickupSub
    }

    val previewLegLabel = when (navLeg) {
        TripNavLeg.ToPickup -> UiStrings.Map.legToPickup
        TripNavLeg.ToDestination -> UiStrings.Map.legToDestination
        null -> UiStrings.Map.routePreview
    }

    val activeStep = remember(route, currentLat, currentLng, drivingMode) {
        val progress = routeProgressForDriver(drivingMode, route, currentLat, currentLng)
        val lat = progress?.snappedLatitude ?: currentLat
        val lng = progress?.snappedLongitude ?: currentLng
        if (route == null || lat == null || lng == null) null
        else NavigationGuidance.findActiveStep(route!!.steps, lat, lng)
    }

    LaunchedEffect(route?.points) {
        promptTracker.resetAll()
    }

    LaunchedEffect(drivingMode, route, activeStep, currentLat, currentLng, towardName) {
        if (!drivingMode) {
            NavigationNotificationState.clear()
            return@LaunchedEffect
        }
        val r = route ?: return@LaunchedEffect
        if (r.points.size < 2) return@LaunchedEffect
        val progress = routeProgressForDriver(true, r, currentLat, currentLng)
        val anchor = progress?.anchorPointIndex ?: 0
        val step = activeStep
        val lat = progress?.snappedLatitude ?: currentLat
        val lng = progress?.snappedLongitude ?: currentLng
        val primary = when {
            step != null && lat != null && lng != null -> {
                val distM = NavigationGuidance.distanceToManeuver(step, lat, lng)
                val distTxt = when {
                    distM == null -> ""
                    distM >= 1000 -> "${"%.1f".format(distM / 1000)} km"
                    else -> "${distM.toInt()} m"
                }
                val street = NavigationGuidance.formatManeuverPrimary(step)
                if (distTxt.isNotBlank()) "$distTxt · $street" else street
            }
            towardName.isNotBlank() -> towardName
            else -> UiStrings.Home.navigationActive
        }
        val displayLine = primary.ifBlank {
            when {
                progress != null ->
                    "${OsrmClient.formatDistance(progress.remainingDistanceMeters)} · " +
                        OsrmClient.formatDurationMinutes(progress.remainingDurationSeconds)
                towardName.isNotBlank() -> "${UiStrings.Map.continueRoute} · $towardName"
                else -> UiStrings.Home.navigationActive
            }
        }
        NavigationNotificationState.update(
            primary = displayLine,
            secondary = "",
            pointCount = r.points.size,
            anchorIndex = anchor,
            intervals = r.trafficIntervals,
            googleTraffic = r.hasTrafficColoredPolyline() || r.durationInTrafficSeconds != null,
            maneuverIcon = NavigationManeuverNotificationIcons.iconFor(step),
        )
        LocationTrackingService.refreshNavigationNotification(context)
    }

    val voiceStep by rememberUpdatedState(activeStep)
    val voiceToward by rememberUpdatedState(towardName)
    val voiceDriving by rememberUpdatedState(drivingMode)
    val voiceOn by rememberUpdatedState(voiceEnabled)
    val voiceDirectionsOn by rememberUpdatedState(mapDisplaySettings.showDirections)

    fun maybeAnnounceVoice(lat: Double, lng: Double) {
        if (!voiceDriving || !voiceOn || !voiceDirectionsOn) return
        val step = voiceStep ?: return
        val distance = NavigationGuidance.distanceToManeuver(step, lat, lng) ?: return
        val band = promptTracker.consume(step, distance) ?: return
        voiceAnnouncer.muted = false
        voiceAnnouncer.speak(NavigationGuidance.buildVoicePrompt(step, band, voiceToward))
    }

    suspend fun awaitDeviceLocation(): Location? = suspendCancellableCoroutine { cont ->
        AppLocationManager.getCurrentLocation(context) { location ->
            if (cont.isActive) cont.resume(location)
        }
    }

    suspend fun awaitFreshDeviceLocation(
        maxAgeMs: Long = 25_000L,
        timeoutMs: Long = 15_000L,
    ): Location? {
        val deadline = System.currentTimeMillis() + timeoutMs
        while (System.currentTimeMillis() < deadline) {
            val loc = awaitDeviceLocation()
            if (loc != null && System.currentTimeMillis() - loc.time <= maxAgeMs) {
                return loc
            }
            delay(450)
        }
        return awaitDeviceLocation()
    }

    fun syncDriverPositionFrom(deviceLat: Double?, deviceLng: Double?, data: ContainerTrackDto?) {
        if (deviceLat != null && deviceLng != null) {
            currentLat = deviceLat
            currentLng = deviceLng
            return
        }
        if (!drivingMode) {
            val (lat, lng) = resolveDriverLatLng(null, null, data)
            currentLat = lat
            currentLng = lng
        }
    }

    /** Fresh GPS fix + accuracy + geofence for pickup / warehouse actions. */
    suspend fun validateFreshGpsAtStop(
        stopLat: Double,
        stopLng: Double,
        stopLabel: String,
    ): Location? {
        val loc = awaitFreshDeviceLocation()
        if (loc == null) {
            actionMessage = UiStrings.Loads.locationRequired
            return null
        }
        if (!loc.meetsNavigationAccuracy()) {
            actionMessage = UiStrings.Map.locationAccuracyPoor
            return null
        }
        syncDriverPositionFrom(loc.latitude, loc.longitude, track)
        val dist = GeoUtils.distanceMeters(loc.latitude, loc.longitude, stopLat, stopLng)
        if (!isWithinDeliveryStopGeofence(dist)) {
            actionMessage = UiStrings.Map.nearStopHint.format(stopLabel, OsrmClient.formatDistance(dist))
            return null
        }
        return loc
    }

    /** @param forceDeviceGps When true (start navigation / resume), use fresh phone GPS only. */
    suspend fun fetchNavigationRoute(data: ContainerTrackDto, forceDeviceGps: Boolean = false): Boolean {
        val origin: Pair<Double, Double>? = try {
            if (forceDeviceGps) {
                val loc = awaitFreshDeviceLocation()
                if (loc != null) {
                    syncDriverPositionFrom(loc.latitude, loc.longitude, data)
                    if (loc.hasBearing()) bearing = loc.bearing
                    loc.latitude to loc.longitude
                } else {
                    null
                }
            } else {
                val lat = currentLat
                val lng = currentLng
                when {
                    lat != null && lng != null -> lat to lng
                    else -> {
                        val loc = awaitDeviceLocation()
                        if (loc != null) {
                            syncDriverPositionFrom(loc.latitude, loc.longitude, data)
                            if (loc.hasBearing()) bearing = loc.bearing
                            loc.latitude to loc.longitude
                        } else {
                            syncDriverPositionFrom(null, null, data)
                            val sLat = currentLat
                            val sLng = currentLng
                            if (sLat != null && sLng != null) sLat to sLng else null
                        }
                    }
                }
            }
        } catch (e: Exception) {
            if (e is CancellationException) throw e
            null
        }
        val (fromLat, fromLng) = origin ?: return false
        val target = data.primaryRouteTarget()
        routeLoading = true
        try {
            val result = RoutingClient.fetchDrivingRoute(
                fromLat,
                fromLng,
                target.latitude,
                target.longitude,
                onQuickRoute = null,
            )
            route = result
            val points = result?.points ?: route?.points
            return points != null && points.size >= 2
        } catch (e: Exception) {
            if (e is CancellationException) throw e
            return false
        } finally {
            routeLoading = false
        }
    }

    suspend fun fetchRouteSegment(
        fromLat: Double,
        fromLng: Double,
        toLat: Double,
        toLng: Double,
    ): OsrmRouteResult? = try {
        RoutingClient.fetchDrivingRoute(fromLat, fromLng, toLat, toLng, onQuickRoute = null)
    } catch (e: Exception) {
        if (e is CancellationException) throw e
        null
    }

    suspend fun loadPickupToWarehousePreview(data: ContainerTrackDto) {
        val phase = deliveryPhase(data.status)
        if (!shouldShowPickupToWarehousePreview(phase, drivingMode)) {
            previewWarehouseRoute = null
            return
        }
        try {
            previewWarehouseRoute = fetchRouteSegment(
                data.pickup.latitude,
                data.pickup.longitude,
                data.destination.latitude,
                data.destination.longitude,
            )
        } catch (e: Exception) {
            if (e is CancellationException) throw e
            previewWarehouseRoute = null
        }
    }

    suspend fun refreshPreviewRoutes(data: ContainerTrackDto) {
        try {
            fetchNavigationRoute(data, forceDeviceGps = false)
            if (drivingMode) {
                previewWarehouseRoute = null
            } else {
                loadPickupToWarehousePreview(data)
            }
        } catch (e: Exception) {
            if (e is CancellationException) throw e
        }
    }

    suspend fun refreshRoute(data: ContainerTrackDto) {
        try {
            fetchNavigationRoute(data, forceDeviceGps = false)
            if (drivingMode) {
                previewWarehouseRoute = null
            } else {
                loadPickupToWarehousePreview(data)
            }
        } catch (e: Exception) {
            if (e is CancellationException) throw e
        }
    }

    LaunchedEffect(drivingMode, containerUuid, track?.containerUuid) {
        val data = track ?: return@LaunchedEffect
        if (!drivingMode) return@LaunchedEffect
        while (true) {
            delay(4 * 60 * 1000L)
            refreshRoute(data)
        }
    }

    fun combinedRoutePointsForOverview(): List<Pair<Double, Double>> {
        val upcoming = previewWarehouseRoute?.points ?: emptyList()
        val active = route?.points ?: emptyList()
        return upcoming + active
    }

    suspend fun loadTrack() {
        if (track == null) loading = true
        error = null
        try {
            val data = repository.track(containerUuid)
            track = data
            if (!drivingMode) {
                syncDriverPositionFrom(currentLat, currentLng, data)
            }
            refreshPreviewRoutes(data)
        } catch (e: Exception) {
            error = e.message
            route = null
            previewWarehouseRoute = null
        }
        loading = false
    }

    fun applyOverviewCamera(map: MapView, data: ContainerTrackDto, routePoints: List<Pair<Double, Double>>) {
        map.setMapCenterOffset(0, 0)
        map.mapOrientation = 0f
        var north = maxOf(data.pickup.latitude, data.destination.latitude)
        var south = minOf(data.pickup.latitude, data.destination.latitude)
        var east = maxOf(data.pickup.longitude, data.destination.longitude)
        var west = minOf(data.pickup.longitude, data.destination.longitude)
        routePoints.forEach { (lat, lng) ->
            north = maxOf(north, lat)
            south = minOf(south, lat)
            east = maxOf(east, lng)
            west = minOf(west, lng)
        }
        currentLat?.let { lat ->
            currentLng?.let { lng ->
                north = maxOf(north, lat)
                south = minOf(south, lat)
                east = maxOf(east, lng)
                west = minOf(west, lng)
            }
        }
        val pad = 0.02
        val box = BoundingBox(
            north + pad,
            east + pad,
            south - pad,
            west - pad,
        )
        map.post { map.zoomToBoundingBox(box, true, 120) }
    }

    @SuppressLint("MissingPermission")
    fun applyDrivingCamera(map: MapView, lat: Double, lng: Double, heading: Float) {
        if (map.height > 0) {
            val offsetFrac = if (northUp) 0.12 else 0.38
            map.setMapCenterOffset(0, (map.height * offsetFrac).toInt())
        }
        val lookAhead = NavigationGuidance.offsetAlongBearing(lat, lng, heading, if (northUp) 40.0 else 120.0)
        map.controller.setZoom(if (northUp) 17.5 else 19.5)
        map.mapOrientation = if (northUp) 0f else -heading
        map.controller.setCenter(GeoPoint(lookAhead.first, lookAhead.second))
    }

    fun addOsmRouteLine(map: MapView, points: List<Pair<Double, Double>>, primary: Boolean) {
        if (points.size < 2) return
        if (primary) {
            val r = route
            val full = r?.points?.map { LatLng(it.first, it.second) } ?: emptyList()
            val chunks = if (full.size >= 2 && r != null) {
                RouteTrafficStyle.buildMergedChunks(full, 0, r.trafficIntervals, r.trafficSegmentDetails)
            } else {
                emptyList()
            }
            if (chunks.isNotEmpty()) {
                chunks.forEach { chunk ->
                    val color = RouteTrafficStyle.speedColor(chunk.speed).toArgb()
                    map.overlays.add(
                        Polyline(map).apply {
                            outlinePaint.color = color
                            outlinePaint.strokeWidth = 18f
                            setPoints(chunk.points.map { GeoPoint(it.latitude, it.longitude) })
                        },
                    )
                }
                return
            }
            val lineColor = if (r?.isOsrmFallback == true) "#F9AB00" else "#4285F4"
            val casingColor = "#1A237E"
            map.overlays.add(
                Polyline(map).apply {
                    outlinePaint.color = android.graphics.Color.parseColor(casingColor)
                    outlinePaint.strokeWidth = 22f
                    setPoints(points.map { (lat, lng) -> GeoPoint(lat, lng) })
                },
            )
            map.overlays.add(
                Polyline(map).apply {
                    outlinePaint.color = android.graphics.Color.parseColor(lineColor)
                    outlinePaint.strokeWidth = 14f
                    setPoints(points.map { (lat, lng) -> GeoPoint(lat, lng) })
                },
            )
            return
        }
        val casingColor = "#86EFAC"
        val lineColor = "#4ADE80"
        map.overlays.add(
            Polyline(map).apply {
                outlinePaint.color = android.graphics.Color.parseColor(casingColor)
                outlinePaint.strokeWidth = MapRouteColors.upcomingCasingOsm
                setPoints(points.map { (lat, lng) -> GeoPoint(lat, lng) })
            },
        )
        map.overlays.add(
            Polyline(map).apply {
                outlinePaint.color = android.graphics.Color.parseColor(lineColor)
                outlinePaint.strokeWidth = MapRouteColors.upcomingStrokeOsm
                setPoints(points.map { (lat, lng) -> GeoPoint(lat, lng) })
            },
        )
    }

    @SuppressLint("MissingPermission")
    fun updateMapOverlays(map: MapView, data: ContainerTrackDto) {
        map.overlays.clear()
        val phase = deliveryPhase(data.status)
        if (shouldShowPickupToWarehousePreview(phase, drivingMode)) {
            previewWarehouseRoute?.points?.let { addOsmRouteLine(map, it, primary = false) }
        }
        val routePointsForDisplay = routePointsForDriver(drivingMode, route, currentLat, currentLng)
        routePointsForDisplay?.let { addOsmRouteLine(map, it, primary = true) }
        val points = routePointsForDisplay ?: emptyList()

        if (!drivingMode) {
            if (data.trail.isNotEmpty()) {
                map.overlays.add(
                    Polyline(map).apply {
                        outlinePaint.color = android.graphics.Color.parseColor("#94A3B8")
                        outlinePaint.strokeWidth = 6f
                        setPoints(data.trail.map { GeoPoint(it.latitude, it.longitude) })
                    },
                )
            }
            map.overlays.add(
                Marker(map).apply {
                    position = GeoPoint(data.pickup.latitude, data.pickup.longitude)
                    title = "${UiStrings.Map.startPoint}: ${data.pickup.name}"
                },
            )
            val leg = tripNavLeg(deliveryPhase(data.status))
            map.overlays.add(
                Marker(map).apply {
                    position = GeoPoint(data.destination.latitude, data.destination.longitude)
                    title = "${UiStrings.Map.endPoint}: ${data.destination.name}"
                    alpha = if (leg == TripNavLeg.ToPickup) 0.65f else 1f
                },
            )
        } else {
            val leg = tripNavLeg(phase)
            map.overlays.add(
                Marker(map).apply {
                    position = GeoPoint(data.pickup.latitude, data.pickup.longitude)
                    title = "${UiStrings.Map.pickup}: ${data.pickup.name}"
                    alpha = if (leg == TripNavLeg.ToPickup) 1f else 0.7f
                },
            )
            map.overlays.add(
                Marker(map).apply {
                    position = GeoPoint(data.destination.latitude, data.destination.longitude)
                    title = "${UiStrings.Map.destination}: ${data.destination.name}"
                    alpha = if (leg == TripNavLeg.ToPickup) 0.55f else 1f
                },
            )
        }

        val rawDriverLat = if (drivingMode) currentLat else currentLat ?: data.currentLatitude
        val rawDriverLng = if (drivingMode) currentLng else currentLng ?: data.currentLongitude
        val progress = routeProgressForDriver(drivingMode, route, rawDriverLat, rawDriverLng)
        val driverLat = if (drivingMode && progress != null) progress.snappedLatitude else rawDriverLat
        val driverLng = if (drivingMode && progress != null) progress.snappedLongitude else rawDriverLng
        if (driverLat != null && driverLng != null) {
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
            map.overlays.add(
                NavigationDriverOverlay().apply {
                    latitude = driverLat
                    longitude = driverLng
                    bearingDegrees = heading
                    showTruckIcon = drivingMode
                },
            )
            if (drivingMode && followCamera) {
                applyDrivingCamera(map, driverLat, driverLng, heading)
            }
        }
        map.invalidate()
    }

    DisposableEffect(containerUuid, drivingMode) {
        voiceAnnouncer.muted = !voiceOn
        permissionLauncher.launch(
            arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION),
        )
        val subscription = AppLocationManager.requestLocationUpdates(
            context = context,
            intervalMs = if (drivingMode) {
                DriverTrackingPolicy.MAP_INTERVAL_NAV_MS
            } else {
                8_000L
            },
            minDistanceMeters = if (drivingMode) {
                DriverTrackingPolicy.MAP_MIN_DISTANCE_NAV_METERS
            } else {
                12f
            },
            highAccuracy = true,
        ) { location ->
            syncDriverPositionFrom(location.latitude, location.longitude, track)
            if (location.hasBearing() && location.bearing > 0f) {
                bearing = location.bearing
            }
            val announceLat = currentLat
            val announceLng = currentLng
            if (drivingMode && announceLat != null && announceLng != null) {
                maybeAnnounceVoice(announceLat, announceLng)
            }
            track?.let { data ->
                mapRef.value?.let { updateMapOverlays(it, data) }
                if (drivingMode) {
                    val r = route
                    if (r != null && r.points.size >= 2) {
                        val offRoute = routeProgressForDriver(
                            true,
                            r,
                            location.latitude,
                            location.longitude,
                        )?.offRouteMeters ?: 0.0
                        routeComplianceBlocked = offRoute >= RouteCompliancePolicy.BLOCK_OFF_ROUTE_METERS
                        if (offRoute >= RouteCompliancePolicy.WARN_OFF_ROUTE_METERS &&
                            actionMessage != UiStrings.Map.rerouting
                        ) {
                            actionMessage = UiStrings.Map.offRouteWarning
                        }
                        if (offRoute >= RouteCompliancePolicy.BLOCK_OFF_ROUTE_METERS) {
                            val now = System.currentTimeMillis()
                            if (now - lastComplianceReportAtMs > RouteCompliancePolicy.COMPLIANCE_REPORT_COOLDOWN_MS) {
                                lastComplianceReportAtMs = now
                                scope.launch {
                                    runCatching {
                                        repository.reportCompliance(
                                            containerUuid,
                                            "off_route_blocked",
                                            location.latitude,
                                            location.longitude,
                                            details = "misroute_blocked",
                                            offRouteMeters = offRoute,
                                        )
                                        loadTrack()
                                    }
                                }
                            }
                        } else if (offRoute > RouteCompliancePolicy.REROUTE_OFF_ROUTE_METERS) {
                            val now = System.currentTimeMillis()
                            if (now - lastRouteRerouteAtMs > 20_000L) {
                                lastRouteRerouteAtMs = now
                                actionMessage = UiStrings.Map.rerouting
                                scope.launch {
                                    refreshRoute(data)
                                    if (actionMessage == UiStrings.Map.rerouting) {
                                        actionMessage = null
                                    }
                                }
                            }
                        } else {
                            routeComplianceBlocked = false
                        }
                    }
                }
            }
            // GPS trail upload runs in LocationTrackingService while driving (avoid duplicate queue entries).
        }
        onDispose { subscription.cancel() }
    }

    LaunchedEffect(containerUuid) {
        permissionLauncher.launch(
            arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION),
        )
        AppLocationManager.getCurrentLocation(context) { loc ->
            if (loc != null) {
                syncDriverPositionFrom(loc.latitude, loc.longitude, track)
                if (loc.hasBearing()) bearing = loc.bearing
            }
        }
        loadTrack()
        while (true) {
            delay(if (drivingMode) 120_000 else 45_000)
            loadTrack()
        }
    }

    LaunchedEffect(track?.containerUuid, track?.status, drivingMode) {
        val data = track ?: return@LaunchedEffect
        if (drivingMode) return@LaunchedEffect
        refreshRoute(data)
    }

    LaunchedEffect(currentLat, currentLng, drivingMode, track?.containerUuid) {
        if (drivingMode) return@LaunchedEffect
        val data = track ?: return@LaunchedEffect
        if (currentLat == null || currentLng == null) return@LaunchedEffect
        delay(400)
        refreshPreviewRoutes(data)
    }

    LaunchedEffect(drivingMode, track?.containerUuid, track?.status) {
        if (drivingMode) previewWarehouseRoute = null
        track?.let { refreshPreviewRoutes(it) }
    }

    LaunchedEffect(drivingMode) {
        if (!drivingMode) return@LaunchedEffect
        val loc = awaitFreshDeviceLocation()
        if (loc != null) {
            syncDriverPositionFrom(loc.latitude, loc.longitude, track)
            if (loc.hasBearing()) bearing = loc.bearing
            track?.let { data -> mapRef.value?.let { updateMapOverlays(it, data) } }
        }
    }

    LaunchedEffect(drivingMode) {
        if (!drivingMode) return@LaunchedEffect
        val trafficInterval = if (MapsConfig.isGoogleMapsConfigured()) 120_000L else 180_000L
        while (true) {
            delay(trafficInterval)
            track?.let { refreshRoute(it) }
        }
    }

    LaunchedEffect(drivingMode, track?.containerUuid) {
        if (!drivingMode) {
            navigationBootstrapping = false
            return@LaunchedEffect
        }
        val data = track ?: return@LaunchedEffect
        val existing = route
        if (existing != null && existing.points.size >= 2) {
            navigationBootstrapping = false
            return@LaunchedEffect
        }
        navigationBootstrapping = true
        try {
            val ok = fetchNavigationRoute(data, forceDeviceGps = true)
            if (!ok) actionMessage = UiStrings.Map.routeLoadFailed
        } finally {
            navigationBootstrapping = false
        }
    }

    val complianceHoldActive = track?.deliveryComplianceHoldActive == true
    val handoffQrAvailable = track?.handoffQrAvailable == true && !complianceHoldActive
    val showQr = phase != null && canShowDeliveryQr(phase, handoffQrAvailable) && !complianceHoldActive
    val waitingAtWarehouse =
        handoffQrAvailable ||
            phase == DeliveryPhase.AtWarehouse ||
            phase == DeliveryPhase.UnderInspection
    val showPickupButton = drivingMode && navLeg == TripNavLeg.ToPickup && canConfirmPickup(phase)
    val distanceToPickupM = remember(currentLat, currentLng, track?.pickup) {
        val lat = currentLat
        val lng = currentLng
        val stop = track?.pickup
        if (lat == null || lng == null || stop == null) null
        else GeoUtils.distanceMeters(lat, lng, stop.latitude, stop.longitude)
    }
    val distanceToDestinationM = remember(currentLat, currentLng, track?.destination) {
        val lat = currentLat
        val lng = currentLng
        val stop = track?.destination
        if (lat == null || lng == null || stop == null) null
        else GeoUtils.distanceMeters(lat, lng, stop.latitude, stop.longitude)
    }
    val nearPickup = isWithinDeliveryStopGeofence(distanceToPickupM)
    val nearDestination = isWithinDeliveryStopGeofence(distanceToDestinationM)
    val showArrivedButton =
        drivingMode &&
            navLeg == TripNavLeg.ToDestination &&
            canConfirmWarehouseArrival(phase) &&
            isWithinArriveButtonApproach(distanceToDestinationM)
    fun stopDistanceHint(label: String, meters: Double?): String? {
        if (meters == null) return UiStrings.Loads.locationRequired
        if (isWithinDeliveryStopGeofence(meters)) return null
        return UiStrings.Map.nearStopHint.format(label, OsrmClient.formatDistance(meters))
    }
    val warehouseNavNeedsPickup =
        !drivingMode && phase != null && requiresPickupGeofenceToStartWarehouseNavigation(phase)
    val startNavigationEnabled =
        !waitingAtWarehouse && (!warehouseNavNeedsPickup || nearPickup)
    val startNavigationHint = when {
        warehouseNavNeedsPickup && !nearPickup ->
            stopDistanceHint(UiStrings.Map.pickup, distanceToPickupM) ?: UiStrings.Map.mustBeAtPickupForWarehouse
        else -> null
    }
    val pickupDistanceHint = if (showPickupButton) stopDistanceHint(UiStrings.Map.pickup, distanceToPickupM) else null
    val arrivedDistanceHint = if (showArrivedButton) stopDistanceHint(UiStrings.Map.destination, distanceToDestinationM) else null
    val maneuverIcon = remember(activeStep) { maneuverIconFor(activeStep) }

    val activeRouteProgress = remember(route, currentLat, currentLng, drivingMode) {
        routeProgressForDriver(drivingMode, route, currentLat, currentLng)
    }

    LaunchedEffect(track, route, previewWarehouseRoute, drivingMode, followCamera, currentLat, currentLng, bearing, northUp) {
        val data = track ?: return@LaunchedEffect
        val map = mapRef.value ?: return@LaunchedEffect
        updateMapOverlays(map, data)
    }

    LaunchedEffect(track?.containerUuid, route?.points, previewWarehouseRoute?.points, drivingMode) {
        if (drivingMode) return@LaunchedEffect
        val data = track ?: return@LaunchedEffect
        val map = mapRef.value ?: return@LaunchedEffect
        applyOverviewCamera(map, data, combinedRoutePointsForOverview())
    }

    val trafficJamMinutes = remember(route) {
        route?.trafficSegmentDetails
            ?.filter { it.speed.equals("TRAFFIC_JAM", ignoreCase = true) }
            ?.sumOf { it.durationSeconds }
            ?.let { (it / 60.0).toInt().coerceAtLeast(0) } ?: 0
    }
    val trafficSlowMinutes = remember(route) {
        route?.trafficSegmentDetails
            ?.filter { it.speed.equals("SLOW", ignoreCase = true) }
            ?.sumOf { it.durationSeconds }
            ?.let { (it / 60.0).toInt().coerceAtLeast(0) } ?: 0
    }

    Box(Modifier.fillMaxSize()) {
    Column(Modifier.fillMaxSize()) {
        if (drivingMode && mapDisplaySettings.showDirections) {
            Row(
                Modifier
                    .fillMaxWidth()
                    .statusBarsPadding()
                    .background(AgriColors.PrimaryDark)
                    .padding(horizontal = 16.dp, vertical = 14.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Icon(maneuverIcon, contentDescription = null, tint = Color.White, modifier = Modifier.size(40.dp))
                Spacer(Modifier.width(12.dp))
                Column(Modifier.weight(1f)) {
                    Text(
                        activeStep?.let { NavigationGuidance.formatManeuverPrimary(it) } ?: UiStrings.Map.continueRoute,
                        color = Color.White,
                        fontWeight = FontWeight.Bold,
                        fontSize = 22.sp,
                        maxLines = 1,
                    )
                    Text(
                        activeStep?.let { NavigationGuidance.formatManeuverSecondary(it, towardName) }
                            ?: "${UiStrings.Map.toward} $towardName",
                        color = Color.White.copy(alpha = 0.9f),
                        fontSize = 14.sp,
                        maxLines = 1,
                    )
                }
                IconButton(
                    onClick = {
                        voiceEnabled = !voiceEnabled
                        voiceAnnouncer.muted = !voiceEnabled
                    },
                ) {
                    Icon(
                        imageVector = if (voiceEnabled) Icons.AutoMirrored.Filled.VolumeUp else Icons.AutoMirrored.Filled.VolumeOff,
                        contentDescription = if (voiceEnabled) UiStrings.Map.muteVoice else UiStrings.Map.unmuteVoice,
                        tint = Color.White,
                    )
                }
            }
        } else {
            AgriScreenTopBar(
                title = containerNumber,
                subtitle = previewLegLabel,
                onBack = onBack,
            )
        }

        Box(
            Modifier
                .weight(1f)
                .fillMaxWidth()
                .background(Color(0xFFE8EEF2)),
        ) {
            when {
                loading && track == null -> LoadingState()
                error != null && track == null -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(error ?: "Failed", color = AgriColors.Error, modifier = Modifier.padding(16.dp))
                }
                else -> {
                    val data = track
                    if (useGoogleMaps && data != null) {
                        DeliveryGoogleMap(
                            modifier = Modifier.fillMaxSize(),
                            track = data,
                            route = route,
                            previewWarehouseRoute = previewWarehouseRoute,
                            drivingMode = drivingMode,
                            followCamera = followCamera,
                            currentLat = currentLat,
                            currentLng = currentLng,
                            bearing = bearing,
                            recenterToken = recenterToken,
                            northUp = northUp,
                            mapSettings = mapDisplaySettings,
                            onUserInteracted = {
                                followCamera = false
                                selectedTrafficSegment = null
                            },
                            onTrafficSegmentClick = { selectedTrafficSegment = it },
                        )
                    } else {
                        AndroidView(
                            modifier = Modifier.fillMaxSize(),
                            factory = {
                                MapView(context).apply {
                                    setTileSource(TileSourceFactory.MAPNIK)
                                    setMultiTouchControls(true)
                                    controller.setZoom(17.0)
                                    addMapListener(
                                        object : MapListener {
                                            override fun onScroll(event: ScrollEvent?): Boolean {
                                                onUserPannedMap.value.invoke()
                                                return true
                                            }

                                            override fun onZoom(event: ZoomEvent?): Boolean {
                                                onUserPannedMap.value.invoke()
                                                return true
                                            }
                                        },
                                    )
                                    mapRef.value = this
                                }
                            },
                            update = { map ->
                                mapRef.value = map
                                val t = track ?: return@AndroidView
                                updateMapOverlays(map, t)
                            },
                        )
                    }
                }
            }
            if (routeLoading && !navigationBootstrapping) {
                LinearProgressIndicator(
                    modifier = Modifier
                        .fillMaxWidth()
                        .align(Alignment.TopCenter),
                    color = AgriColors.Primary,
                )
            }
            if (navigationBootstrapping) {
                NavigationBootstrapOverlay(
                    towardName = towardName,
                    modifier = Modifier.matchParentSize(),
                )
            }
            selectedTrafficSegment?.let { segment ->
                MapTrafficSegmentTooltip(
                    segment = segment,
                    modifier = Modifier
                        .align(Alignment.TopCenter)
                        .padding(horizontal = 12.dp)
                        .padding(top = 12.dp),
                )
            }
            if (!navigationBootstrapping) {
                MapFloatingControls(
                drivingMode = drivingMode,
                northUp = northUp,
                onOpenMapSettings = { showMapSettings = true },
                onRecenter = {
                    followCamera = true
                    recenterToken++
                    val data = track
                    val map = mapRef.value
                    if (map == null) return@MapFloatingControls
                    if (drivingMode) {
                        val lat = currentLat
                        val lng = currentLng
                        if (lat != null && lng != null) {
                            applyDrivingCamera(map, lat, lng, bearing)
                        }
                    } else if (data != null) {
                        applyOverviewCamera(map, data, combinedRoutePointsForOverview())
                    }
                },
                onToggleNorthUp = {
                    northUp = !northUp
                    followCamera = true
                    recenterToken++
                    val lat = currentLat
                    val lng = currentLng
                    mapRef.value?.let { map ->
                        if (drivingMode && lat != null && lng != null) {
                            applyDrivingCamera(map, lat, lng, bearing)
                        }
                    }
                },
                modifier = Modifier.align(Alignment.BottomEnd),
            )
            }
        }

        MapTripPanel(
            drivingMode = drivingMode,
            route = route,
            remainingDurationSeconds = activeRouteProgress?.remainingDurationSeconds,
            remainingDistanceMeters = activeRouteProgress?.remainingDistanceMeters,
            routeLoading = routeLoading,
            track = track,
            navLeg = navLeg,
            towardName = towardName,
            startButtonSub = startButtonSub,
            showQr = showQr,
            showPickupButton = showPickupButton,
            showArrivedButton = showArrivedButton,
            pickupEnabled = nearPickup,
            arrivedEnabled = nearDestination,
            pickupDistanceHint = pickupDistanceHint,
            arrivedDistanceHint = arrivedDistanceHint,
            waitingAtWarehouse = waitingAtWarehouse,
            pickupBusy = pickupBusy,
            arriveBusy = arriveBusy,
            startNavigationBusy = navigationBootstrapping,
            startNavigationEnabled = startNavigationEnabled,
            startNavigationHint = startNavigationHint,
            actionMessage = actionMessage,
            onBack = onBack,
            onStartNavigation = {
                scope.launch {
                    val data = track
                    if (data == null) return@launch
                    navigationBootstrapping = true
                    actionMessage = null
                    try {
                        val phaseBefore = deliveryPhase(data.status)
                        if (requiresPickupGeofenceToStartWarehouseNavigation(phaseBefore)) {
                            val pickup = data.pickup
                            val loc = validateFreshGpsAtStop(
                                pickup.latitude,
                                pickup.longitude,
                                UiStrings.Map.pickup,
                            ) ?: return@launch
                            val result = runCatching {
                                repository.updateStatus(
                                    containerUuid,
                                    "InTransit",
                                    loc.latitude,
                                    loc.longitude,
                                )
                                loadTrack()
                            }
                            if (result.isFailure) {
                                actionMessage = result.exceptionOrNull()?.message
                                return@launch
                            }
                        }
                        previewWarehouseRoute = null
                        val routeReady = fetchNavigationRoute(track ?: data, forceDeviceGps = true)
                        if (!routeReady) {
                            actionMessage = UiStrings.Loads.locationRequired
                            return@launch
                        }
                        northUp = false
                        drivingMode = true
                        followCamera = true
                        recenterToken++
                        promptTracker.resetAll()
                    } finally {
                        navigationBootstrapping = false
                    }
                }
            },
            trafficJamMinutes = trafficJamMinutes,
            trafficSlowMinutes = trafficSlowMinutes,
            onConfirmPickup = {
                val pickup = track?.pickup
                if (pickup == null) {
                    actionMessage = UiStrings.Loads.locationRequired
                    return@MapTripPanel
                }
                scope.launch {
                    actionMessage = null
                    val loc = validateFreshGpsAtStop(
                        pickup.latitude,
                        pickup.longitude,
                        UiStrings.Map.pickup,
                    ) ?: return@launch
                    openGeotagCapture(
                        PendingGeotagCapture.Pickup(
                            loc.latitude,
                            loc.longitude,
                            track?.pickup?.name,
                        ),
                    )
                }
            },
            onArrivedAtWarehouse = {
                val destination = track?.destination
                if (destination == null) {
                    actionMessage = UiStrings.Loads.locationRequired
                    return@MapTripPanel
                }
                scope.launch {
                    actionMessage = null
                    val loc = validateFreshGpsAtStop(
                        destination.latitude,
                        destination.longitude,
                        UiStrings.Map.destination,
                    ) ?: return@launch
                    openGeotagCapture(
                        PendingGeotagCapture.Warehouse(loc.latitude, loc.longitude),
                    )
                }
            },
            onOpenQr = onOpenQr,
        )
    }

    pendingGeotag?.let { pending ->
            val title = when (pending) {
                is PendingGeotagCapture.Pickup -> UiStrings.Geotag.pickupTitle
                is PendingGeotagCapture.Warehouse -> UiStrings.Geotag.warehouseTitle
            }
            val hint = when (pending) {
                is PendingGeotagCapture.Pickup -> UiStrings.Geotag.pickupHint
                is PendingGeotagCapture.Warehouse -> UiStrings.Geotag.warehouseHint
            }
            val lat = when (pending) {
                is PendingGeotagCapture.Pickup -> pending.latitude
                is PendingGeotagCapture.Warehouse -> pending.latitude
            }
            val lng = when (pending) {
                is PendingGeotagCapture.Pickup -> pending.longitude
                is PendingGeotagCapture.Warehouse -> pending.longitude
            }
            GeotagCaptureScreen(
                latitude = lat,
                longitude = lng,
                title = title,
                hint = hint,
                onCancel = { pendingGeotag = null },
                onCaptured = { jpeg ->
                    val capturedAt = Instant.now()
                    pendingGeotag = null
                    scope.launch {
                        when (pending) {
                            is PendingGeotagCapture.Pickup -> {
                                pickupBusy = true
                                actionMessage = null
                                runCatching {
                                    repository.confirmPickupWithGeotag(
                                        containerUuid,
                                        pending.latitude,
                                        pending.longitude,
                                        pending.label,
                                        jpeg,
                                        capturedAt,
                                    )
                                    navSessionStore.clear(containerUuid)
                                    drivingMode = false
                                    followCamera = false
                                    voiceAnnouncer.stop()
                                    promptTracker.resetAll()
                                    appContainer.trackingStore.clearNavigationSession(containerUuid)
                                    LocationTrackingService.stop(context)
                                    loadTrack()
                                    actionMessage = UiStrings.Loads.pickupConfirmed
                                }.onFailure { actionMessage = it.message }
                                pickupBusy = false
                            }
                            is PendingGeotagCapture.Warehouse -> {
                                arriveBusy = true
                                actionMessage = null
                                runCatching {
                                    val result = repository.checkInAtWarehouseWithGeotag(
                                        containerUuid,
                                        pending.latitude,
                                        pending.longitude,
                                        jpeg,
                                        capturedAt,
                                    )
                                    navSessionStore.clear(containerUuid)
                                    drivingMode = false
                                    followCamera = false
                                    voiceAnnouncer.stop()
                                    promptTracker.resetAll()
                                    appContainer.trackingStore.clearNavigationSession(containerUuid)
                                    LocationTrackingService.stop(context)
                                    loadTrack()
                                    actionMessage = if (result.withinGeofence) {
                                        UiStrings.Loads.arrivalConfirmed
                                    } else {
                                        UiStrings.Loads.arrivalOutsideGeofence.format(
                                            OsrmClient.formatDistance(result.distanceMeters),
                                        )
                                    }
                                }.onFailure { e ->
                                    val msg = e.message ?: ""
                                    actionMessage = if (
                                        msg.contains("WRONG_WAREHOUSE", ignoreCase = true) ||
                                        msg.contains("Wrong warehouse", ignoreCase = true)
                                    ) {
                                        UiStrings.Loads.wrongWarehouseAlert
                                    } else {
                                        msg
                                    }
                                    loadTrack()
                                }
                                arriveBusy = false
                            }
                        }
                    }
                },
            )
        }

    if (complianceHoldActive) {
        AlertDialog(
            onDismissRequest = { },
            title = { Text(UiStrings.Loads.complianceHoldTitle) },
            text = { Text(UiStrings.Loads.complianceHoldBody) },
            confirmButton = {
                TextButton(onClick = { scope.launch { loadTrack() } }) {
                    Text("OK")
                }
            },
        )
    }

    if (routeComplianceBlocked && drivingMode) {
        AlertDialog(
            onDismissRequest = { },
            title = { Text(UiStrings.Map.offRouteBlockedTitle) },
            text = { Text(UiStrings.Map.offRouteBlockedBody) },
            confirmButton = {
                TextButton(onClick = { routeComplianceBlocked = false }) {
                    Text("OK")
                }
            },
        )
    }

    if (showMapSettings) {
        MapSettingsSheet(
            settings = mapDisplaySettings,
            onChange = { updated ->
                mapDisplaySettings = updated
                scope.launch {
                    appContainer.settingsStore.saveMapDisplaySettings(updated)
                }
            },
            onDismiss = { showMapSettings = false },
        )
    }
    }
}

private fun routeProgressForDriver(
    drivingMode: Boolean,
    route: OsrmRouteResult?,
    latitude: Double?,
    longitude: Double?,
): RouteProgress? {
    if (!drivingMode || route == null || latitude == null || longitude == null || route.points.size < 2) {
        return null
    }
    return RouteProgressHelper.compute(
        route.points,
        route.distanceMeters,
        route.durationSeconds,
        latitude,
        longitude,
        route.durationInTrafficSeconds,
    )
}

private fun routePointsForDriver(
    drivingMode: Boolean,
    route: OsrmRouteResult?,
    latitude: Double?,
    longitude: Double?,
): List<Pair<Double, Double>>? {
    if (route == null) return null
    if (!drivingMode || latitude == null || longitude == null) return route.points
    return routeProgressForDriver(drivingMode, route, latitude, longitude)?.displayPoints ?: route.points
}

private fun maneuverIconFor(step: com.agricheck.agritrack.data.api.DrivingRouteStep?): ImageVector {
    if (step == null) return Icons.Default.Navigation
    val mod = step.maneuverModifier?.lowercase() ?: ""
    return when {
        step.maneuverType == "arrive" -> Icons.Default.Flag
        mod.contains("left") -> Icons.Default.SubdirectoryArrowLeft
        mod.contains("right") -> Icons.Default.SubdirectoryArrowRight
        else -> Icons.Default.Navigation
    }
}
