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
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material.icons.automirrored.filled.VolumeOff
import androidx.compose.material.icons.automirrored.filled.VolumeUp
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import com.agricheck.agritrack.ui.camera.GeotagCaptureScreen
import com.agricheck.agritrack.ui.incident.DriverIncidentReportSheet
import java.time.Instant
import com.agricheck.agritrack.data.api.OsrmClient
import com.agricheck.agritrack.data.api.apiErrorCode
import com.agricheck.agritrack.data.api.apiErrorMessage
import com.agricheck.agritrack.data.api.userMessage
import com.agricheck.agritrack.data.api.OsrmRouteResult
import com.agricheck.agritrack.data.api.RoutingClient
import com.agricheck.agritrack.data.model.ContainerTrackDto
import com.agricheck.agritrack.data.model.DriverDeliveryHandoffQrDto
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.data.local.PersistedNavigationSession
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.map.DriverMapLifecycle
import com.agricheck.agritrack.navigation.NavigationMotionSmoother
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
import com.agricheck.agritrack.util.*
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume

private val RouteGreen = Color(0xFF15803D)

private sealed interface PendingGeotagCapture {
    data class Pickup(val latitude: Double, val longitude: Double, val label: String?) : PendingGeotagCapture
    data class Warehouse(val latitude: Double, val longitude: Double) : PendingGeotagCapture
    data class EmptyContainer(val latitude: Double, val longitude: Double) : PendingGeotagCapture
}

private data class PendingIncidentGeotag(
    val step: Int,
    val categoryCode: String,
    val notes: String?,
    val latitude: Double,
    val longitude: Double,
    val photo1: ByteArray? = null,
)
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
    val useGoogleNavSdk = MapsConfig.useGoogleNavigationSdk()
    val useGoogleMaps = MapsConfig.useInAppGoogleMap(context, drivingMode)
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
    var emptyContainerBusy by remember { mutableStateOf(false) }
    var pendingGeotag by remember { mutableStateOf<PendingGeotagCapture?>(null) }
    var geotagAfterCamera by remember { mutableStateOf<PendingGeotagCapture?>(null) }
    var pendingIncidentGeotag by remember { mutableStateOf<PendingIncidentGeotag?>(null) }
    var geotagAfterIncident by remember { mutableStateOf<PendingIncidentGeotag?>(null) }
    var showIncidentSheet by remember { mutableStateOf(false) }
    var incidentSubmitting by remember { mutableStateOf(false) }
    val suspendMapForCapture =
        pendingGeotag != null || pendingIncidentGeotag != null || showIncidentSheet
    var actionMessage by remember { mutableStateOf<String?>(null) }
    var routeLoading by remember { mutableStateOf(false) }
    var navigationBootstrapping by remember { mutableStateOf(false) }
    var mapDisplaySettings by remember { mutableStateOf(MapDisplaySettings()) }
    var showMapSettings by remember { mutableStateOf(false) }
    var selectedTrafficSegment by remember { mutableStateOf<TrafficSegmentDetail?>(null) }
    var lastRouteRerouteAtMs by remember { mutableLongStateOf(0L) }
    var lastComplianceReportAtMs by remember { mutableLongStateOf(0L) }
    var routeComplianceBlocked by remember { mutableStateOf(false) }
    var mapDriverAlert by remember { mutableStateOf<MapDriverTimedAlert?>(null) }
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

    LaunchedEffect(useGoogleNavSdk, drivingMode, mapDisplaySettings.showDirections, voiceEnabled) {
        if (!useGoogleNavSdk || !drivingMode) return@LaunchedEffect
        DriverMapLifecycle.applyNavigationAudio(mapDisplaySettings.showDirections && voiceEnabled)
    }
    val promptTracker = remember { NavigationPromptTracker() }
    val navMotionSmoother = remember { NavigationMotionSmoother() }
    var navDisplayLat by remember { mutableStateOf<Double?>(null) }
    var navDisplayLng by remember { mutableStateOf<Double?>(null) }
    var navDisplayBearing by remember { mutableStateOf(0f) }
    var destinationGpsSyncPrompted by remember { mutableStateOf(false) }
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { }
    val cameraPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { granted ->
        if (granted) {
            pendingGeotag = geotagAfterCamera
            pendingIncidentGeotag = geotagAfterIncident
        } else {
            actionMessage = UiStrings.Permissions.cameraRequired
        }
        geotagAfterCamera = null
        geotagAfterIncident = null
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

    fun openIncidentGeotagCapture(pending: PendingIncidentGeotag) {
        when (ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA)) {
            PackageManager.PERMISSION_GRANTED -> pendingIncidentGeotag = pending
            else -> {
                geotagAfterIncident = pending
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

    LaunchedEffect(drivingMode) {
        if (!drivingMode) DriverMapLifecycle.onExitDrivingMode()
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
            googleTurnByTurnActive = useGoogleNavSdk,
        )
    }

    LaunchedEffect(towardName, drivingMode) {
        if (!drivingMode || towardName.isBlank()) return@LaunchedEffect
        LocationTrackingService.updateNavigation(
            context,
            containerUuid,
            containerNumber,
            towardName,
            googleTurnByTurnActive = useGoogleNavSdk,
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
        val r = route
        if (r == null || r.points.size < 2) {
            val fallbackLine = towardName.ifBlank { UiStrings.Home.navigationActive }
            NavigationNotificationState.update(
                primary = fallbackLine,
                secondary = "",
                pointCount = 0,
                anchorIndex = 0,
                intervals = emptyList(),
                googleTraffic = false,
                maneuverIcon = NavigationManeuverNotificationIcons.iconFor(null),
            )
            LocationTrackingService.refreshNavigationNotification(context)
            return@LaunchedEffect
        }
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

    LaunchedEffect(drivingMode, currentLat, currentLng) {
        if (!drivingMode) return@LaunchedEffect
        while (isActive) {
            delay(4_000L)
            LocationTrackingService.refreshNavigationNotification(context)
        }
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
        if (pendingGeotag != null || pendingIncidentGeotag != null || showIncidentSheet) {
            return false
        }
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
        if (pendingGeotag != null || pendingIncidentGeotag != null || showIncidentSheet) return
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
        if (pendingGeotag != null || pendingIncidentGeotag != null || showIncidentSheet) return
        if (routeLoading) return
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

    val trackForLocation by rememberUpdatedState(track)
    val routeForLocation by rememberUpdatedState(route)

    DisposableEffect(containerUuid, drivingMode) {
        voiceAnnouncer.muted = !voiceOn
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
            syncDriverPositionFrom(location.latitude, location.longitude, trackForLocation)
            if (location.hasBearing() && location.bearing > 0f) {
                bearing = location.bearing
            }
            val announceLat = currentLat
            val announceLng = currentLng
            if (drivingMode && announceLat != null && announceLng != null) {
                maybeAnnounceVoice(announceLat, announceLng)
            }
            trackForLocation?.let { data ->
                if (drivingMode) {
                    val r = routeForLocation
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

    LaunchedEffect(containerUuid, drivingMode) {
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
        while (isActive) {
            delay(if (drivingMode) 120_000 else 45_000)
            loadTrack()
        }
    }

    LaunchedEffect(track?.containerUuid, track?.status, drivingMode, suspendMapForCapture) {
        val data = track ?: return@LaunchedEffect
        if (drivingMode || suspendMapForCapture) return@LaunchedEffect
        refreshRoute(data)
    }

    LaunchedEffect(currentLat, currentLng, drivingMode, track?.containerUuid, suspendMapForCapture) {
        if (drivingMode || suspendMapForCapture) return@LaunchedEffect
        val data = track ?: return@LaunchedEffect
        if (currentLat == null || currentLng == null) return@LaunchedEffect
        delay(400)
        refreshPreviewRoutes(data)
    }

    LaunchedEffect(drivingMode, track?.containerUuid, track?.status, suspendMapForCapture) {
        if (suspendMapForCapture) return@LaunchedEffect
        if (drivingMode) previewWarehouseRoute = null
        track?.let { refreshPreviewRoutes(it) }
    }

    LaunchedEffect(drivingMode) {
        if (!drivingMode) return@LaunchedEffect
        val loc = awaitFreshDeviceLocation()
        if (loc != null) {
            syncDriverPositionFrom(loc.latitude, loc.longitude, track)
            if (loc.hasBearing()) bearing = loc.bearing
        }
    }

    LaunchedEffect(drivingMode, track?.containerUuid, useGoogleNavSdk) {
        if (!drivingMode) {
            navigationBootstrapping = false
            return@LaunchedEffect
        }
        val data = track ?: return@LaunchedEffect
        if (useGoogleNavSdk) {
            navigationBootstrapping = false
            scope.launch {
                runCatching { fetchNavigationRoute(data, forceDeviceGps = true) }
            }
            return@LaunchedEffect
        }
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

    fun alertStyleForMessage(message: String): MapDriverAlertStyle = when {
        message == UiStrings.Map.offRouteWarning ||
            message == UiStrings.Map.rerouting -> MapDriverAlertStyle.Warning
        message == UiStrings.Loads.wrongWarehouseAlert ||
            message.contains("compliance", ignoreCase = true) ||
            message.contains("DA", ignoreCase = true) && message.contains("alert", ignoreCase = true) ->
            MapDriverAlertStyle.Error
        message == UiStrings.Incident.submitting -> MapDriverAlertStyle.Info
        else -> MapDriverAlertStyle.Success
    }

    LaunchedEffect(complianceHoldActive) {
        if (complianceHoldActive) {
            mapDriverAlert = MapDriverTimedAlert(
                title = UiStrings.Loads.complianceHoldTitle,
                message = UiStrings.Loads.complianceHoldBody,
                style = MapDriverAlertStyle.Error,
            )
        } else if (mapDriverAlert?.title == UiStrings.Loads.complianceHoldTitle) {
            mapDriverAlert = null
        }
    }

    LaunchedEffect(routeComplianceBlocked, drivingMode) {
        if (routeComplianceBlocked && drivingMode) {
            mapDriverAlert = MapDriverTimedAlert(
                title = UiStrings.Map.offRouteBlockedTitle,
                message = UiStrings.Map.offRouteBlockedBody,
                style = MapDriverAlertStyle.Warning,
            )
        } else if (mapDriverAlert?.title == UiStrings.Map.offRouteBlockedTitle) {
            mapDriverAlert = null
        }
    }

    LaunchedEffect(actionMessage, complianceHoldActive) {
        val msg = actionMessage
        if (msg.isNullOrBlank() || complianceHoldActive) return@LaunchedEffect
        mapDriverAlert = MapDriverTimedAlert(
            title = null,
            message = msg,
            style = alertStyleForMessage(msg),
        )
    }
    val serverHandoffReady = track?.handoffQrAvailable == true && !complianceHoldActive
    val atWarehousePhase = phase == DeliveryPhase.AtWarehouse
    val handoffQrAvailable = serverHandoffReady
    val showQr = phase != null && canShowDeliveryQr(phase, serverHandoffReady) && !complianceHoldActive
    val waitingAtWarehouse = (serverHandoffReady || atWarehousePhase) && !isDriverTransitEnded(phase)
    val shouldPollHandoffQr = waitingAtWarehouse && !isDriverTransitEnded(phase)
    var handoffQr by remember { mutableStateOf<DriverDeliveryHandoffQrDto?>(null) }
    var handoffQrWaiting by remember { mutableStateOf<String?>(null) }
    val storedApiBaseUrl by appContainer.settingsStore.apiBaseUrlFlow.collectAsState(initial = null)
    val apiBaseUrl = remember(storedApiBaseUrl) {
        appContainer.resolveApiBaseUrl(storedApiBaseUrl)
    }
    LaunchedEffect(containerUuid, shouldPollHandoffQr) {
        if (!shouldPollHandoffQr) {
            handoffQr = null
            handoffQrWaiting = null
            return@LaunchedEffect
        }
        var errorBackoffMs = 8_000L
        while (isActive) {
            val delayMs = runCatching { repository.deliveryHandoffQr(containerUuid) }
                .fold(
                    onSuccess = { dto ->
                        errorBackoffMs = 8_000L
                        if (dto.qrPayload.isNotBlank() || !dto.qrCodeData.isNullOrBlank()) {
                            handoffQr = dto
                            handoffQrWaiting = null
                        } else {
                            handoffQrWaiting = UiStrings.Map.summaryGeneratingQr
                        }
                        dto.refreshAfterSeconds.coerceIn(30, 120) * 1000L
                    },
                    onFailure = { e ->
                        val code = e.apiErrorCode()
                        val m = e.apiErrorMessage(UiStrings.Map.summaryGeneratingQr)
                        when {
                            code == "HANDOFF_NOT_READY" ||
                                m.contains("Confirm arrival", ignoreCase = true) -> {
                                handoffQrWaiting = UiStrings.DeliveryQr.waitingForArrival
                            }
                            code == "OUTSIDE_GEOFENCE" ||
                                m.contains("within the assigned warehouse", ignoreCase = true) -> {
                                handoffQrWaiting = UiStrings.DeliveryQr.waitingForGps
                            }
                            code == "COMPLIANCE_HOLD" ||
                                m.contains("compliance hold", ignoreCase = true) ||
                                m.contains("DA authority", ignoreCase = true) -> {
                                handoffQrWaiting = m
                            }
                            code == "QR_UNAVAILABLE" -> {
                                handoffQrWaiting = m
                            }
                            handoffQr == null -> {
                                handoffQrWaiting = m
                            }
                        }
                        errorBackoffMs = (errorBackoffMs * 2).coerceAtMost(60_000L)
                        when {
                            code == "HANDOFF_NOT_READY" -> 12_000L
                            e is retrofit2.HttpException && e.code() in 502..504 -> errorBackoffMs
                            handoffQr != null -> 30_000L
                            else -> errorBackoffMs
                        }
                    },
                )
            delay(delayMs)
        }
    }
    LaunchedEffect(containerUuid, waitingAtWarehouse) {
        if (!waitingAtWarehouse) return@LaunchedEffect
        while (isActive) {
            delay(15_000)
            loadTrack()
        }
    }
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
    val showEmptyContainerButton =
        !drivingMode &&
            phase != null &&
            canSubmitEmptyContainerProof(phase) &&
            isWithinArriveButtonApproach(distanceToDestinationM)
    fun stopDistanceHint(label: String, meters: Double?): String? {
        if (meters == null) return UiStrings.Loads.locationRequired
        if (isWithinDeliveryStopGeofence(meters)) return null
        return UiStrings.Map.nearStopHint.format(label, OsrmClient.formatDistance(meters))
    }
    val tripCompleted = isDeliveryTripComplete(phase)
    val transitEnded = isDriverTransitEnded(phase)
    LaunchedEffect(transitEnded, containerUuid) {
        if (!transitEnded) return@LaunchedEffect
        if (drivingMode) {
            drivingMode = false
            followCamera = false
            voiceAnnouncer.stop()
            promptTracker.resetAll()
            LocationTrackingService.stop(context)
            appContainer.trackingStore.clearNavigationSession(containerUuid)
        } else {
            LocationTrackingService.stop(context)
        }
    }
    val warehouseNavNeedsPickup =
        !drivingMode && phase != null && requiresPickupGeofenceToStartWarehouseNavigation(phase)
    val startNavigationEnabled =
        !transitEnded &&
            !tripCompleted &&
            !waitingAtWarehouse &&
            (!warehouseNavNeedsPickup || nearPickup)
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

    LaunchedEffect(drivingMode) {
        if (!drivingMode) {
            navMotionSmoother.reset()
            navDisplayLat = null
            navDisplayLng = null
            return@LaunchedEffect
        }
        while (isActive) {
            val frameMs = 32L
            val step = navMotionSmoother.step(frameMs)
            if (step != null) {
                navDisplayLat = step.latitude
                navDisplayLng = step.longitude
                navDisplayBearing = step.bearing
            }
            delay(frameMs)
        }
    }

    LaunchedEffect(currentLat, currentLng, bearing, route, drivingMode) {
        if (!drivingMode) return@LaunchedEffect
        val lat = currentLat ?: return@LaunchedEffect
        val lng = currentLng ?: return@LaunchedEffect
        val progress = routeProgressForDriver(true, route, lat, lng)
        val tLat = progress?.snappedLatitude ?: lat
        val tLng = progress?.snappedLongitude ?: lng
        val points = progress?.displayPoints ?: route?.points ?: emptyList()
        val lookAhead = points.dropWhile { (plat, plng) ->
            GeoUtils.distanceMeters(tLat, tLng, plat, plng) > 80
        }.drop(1).firstOrNull()
        val heading = if (bearing > 1f) {
            bearing
        } else if (lookAhead != null) {
            NavigationGuidance.bearingDegrees(tLat, tLng, lookAhead.first, lookAhead.second)
        } else {
            bearing
        }
        navMotionSmoother.setTarget(tLat, tLng, heading)
    }

    LaunchedEffect(nearDestination, drivingMode, navLeg) {
        if (!drivingMode || navLeg != TripNavLeg.ToDestination) {
            if (!nearDestination) destinationGpsSyncPrompted = false
            return@LaunchedEffect
        }
        if (!nearDestination || destinationGpsSyncPrompted) return@LaunchedEffect
        destinationGpsSyncPrompted = true
        scope.launch {
            val synced = DriverLocationSync(context).flushPendingUploads()
            actionMessage = if (synced > 0) {
                UiStrings.Map.arrivalGpsSynced.format(synced)
            } else {
                UiStrings.Map.arrivalGpsUpToDate
            }
        }
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
    val showDeliverySheet = track != null && !navigationBootstrapping
    // Peek: minimal drag handle + booking card + pinned powered-by footer (see MapDeliverySheetScaffold).
    val deliverySheetPeek = when {
        waitingAtWarehouse -> 204.dp
        drivingMode -> 124.dp
        transitEnded || tripCompleted -> 136.dp
        else -> 132.dp
    }
    Box(Modifier.fillMaxSize()) {
    Column(Modifier.fillMaxSize()) {
        if (drivingMode && mapDisplaySettings.showDirections && !useGoogleNavSdk) {
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
                IconButton(onClick = { showIncidentSheet = true }) {
                    Icon(
                        Icons.Default.Warning,
                        contentDescription = UiStrings.Incident.reportFab,
                        tint = Color(0xFFFFB454),
                    )
                }
            }
        } else {
            AgriScreenTopBar(
                title = containerNumber,
                subtitle = if (drivingMode) towardName.ifBlank { previewLegLabel } else previewLegLabel,
                onBack = onBack,
                trailingActions = if (drivingMode) {
                    {
                        IconButton(onClick = { showIncidentSheet = true }) {
                            Icon(
                                Icons.Default.Warning,
                                contentDescription = UiStrings.Incident.reportFab,
                                tint = Color(0xFFEA580C),
                            )
                        }
                    }
                } else {
                    null
                },
            )
        }

        Box(
            Modifier
                .weight(1f)
                .fillMaxWidth()
                .background(Color(0xFFE8EEF2)),
        ) {
            val mapLayer: @Composable BoxScope.() -> Unit = {
            when {
                loading && track == null -> LoadingState()
                error != null && track == null -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    Text(error ?: "Failed", color = AgriColors.Error, modifier = Modifier.padding(16.dp))
                }
                else -> {
                    val data = track
                    if (suspendMapForCapture) {
                        Box(
                            Modifier
                                .fillMaxSize()
                                .background(Color.Black),
                        )
                    } else if (!useGoogleMaps && data != null) {
                        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                            Text(
                                UiStrings.Map.googleMapsRequired,
                                color = AgriColors.Error,
                                modifier = Modifier.padding(16.dp),
                            )
                        }
                    } else if (data != null) {
                        MapScreenDriverMapLayer(
                            modifier = Modifier.fillMaxSize(),
                            containerUuid = containerUuid,
                            track = data,
                            route = route,
                            previewWarehouseRoute = previewWarehouseRoute,
                            drivingMode = drivingMode,
                            followCamera = followCamera,
                            currentLat = currentLat,
                            currentLng = currentLng,
                            bearing = bearing,
                            navDisplayLat = if (drivingMode) navDisplayLat else null,
                            navDisplayLng = if (drivingMode) navDisplayLng else null,
                            navDisplayBearing = if (drivingMode) navDisplayBearing else 0f,
                            recenterToken = recenterToken,
                            northUp = northUp,
                            mapSettings = mapDisplaySettings,
                            voiceEnabled = voiceEnabled,
                            onUserInteracted = {
                                followCamera = false
                                selectedTrafficSegment = null
                            },
                            onTrafficSegmentClick = { selectedTrafficSegment = it },
                            onRouteError = { msg -> actionMessage = msg },
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
            if (!navigationBootstrapping && track != null && !drivingMode) {
                SmallFloatingActionButton(
                    onClick = { showIncidentSheet = true },
                    modifier = Modifier
                        .align(Alignment.TopEnd)
                        .padding(top = 12.dp, end = 12.dp),
                    containerColor = Color(0xFFEA580C),
                    contentColor = Color.White,
                ) {
                    Icon(
                        Icons.Default.Warning,
                        contentDescription = UiStrings.Incident.reportFab,
                    )
                }
            }
            MapDriverTimedAlertCard(
                alert = mapDriverAlert,
                onDismiss = {
                    mapDriverAlert = null
                    actionMessage = null
                },
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .padding(horizontal = 12.dp, vertical = 10.dp),
            )
            if (!navigationBootstrapping) {
                MapFloatingControls(
                drivingMode = drivingMode,
                northUp = northUp,
                onOpenMapSettings = { showMapSettings = true },
                onRecenter = {
                    followCamera = true
                    recenterToken++
                },
                onToggleNorthUp = {
                    northUp = !northUp
                    followCamera = true
                    recenterToken++
                },
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(end = 8.dp, bottom = 8.dp),
            )
            }
            }
            if (showDeliverySheet) {
                MapDeliverySheetScaffold(
                    modifier = Modifier.fillMaxSize(),
                    sheetPeekHeight = deliverySheetPeek,
                    sheetContent = {
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
            showEmptyContainerButton = showEmptyContainerButton,
            emptyContainerEnabled = nearDestination,
            emptyContainerBusy = emptyContainerBusy,
            startNavigationBusy = navigationBootstrapping,
            tripCompleted = tripCompleted || transitEnded,
            tripEndedMessage = when {
                tripCompleted -> UiStrings.Map.tripCompleted
                transitEnded -> UiStrings.Map.tripEndedUnderInspection
                else -> null
            },
            startNavigationEnabled = startNavigationEnabled,
            startNavigationHint = startNavigationHint,
            showInAppGoogleHint = MapsConfig.canShowTrafficLayer(context),
            hideEtaWhenGoogleNavigation = useGoogleNavSdk,
            apiBaseUrl = apiBaseUrl,
            handoffQr = handoffQr,
            handoffQrWaitingMessage = handoffQrWaiting,
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
                        if (!useGoogleNavSdk) {
                            val routeReady = fetchNavigationRoute(track ?: data, forceDeviceGps = true)
                            if (!routeReady) {
                                actionMessage = UiStrings.Loads.locationRequired
                                return@launch
                            }
                        } else {
                            val loc = awaitFreshDeviceLocation()
                            if (loc == null) {
                                actionMessage = UiStrings.Loads.locationRequired
                                return@launch
                            }
                            syncDriverPositionFrom(loc.latitude, loc.longitude, track)
                            if (loc.hasBearing()) bearing = loc.bearing
                            scope.launch {
                                runCatching { fetchNavigationRoute(data, forceDeviceGps = false) }
                            }
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
            onEmptyContainerProof = {
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
                        PendingGeotagCapture.EmptyContainer(loc.latitude, loc.longitude),
                    )
                }
            },
            onOpenQr = onOpenQr,
                        )
                    },
                    mapContent = mapLayer,
                )
            } else {
                mapLayer()
            }
        }
    }

    pendingGeotag?.let { pending ->
            val title = when (pending) {
                is PendingGeotagCapture.Pickup -> UiStrings.Geotag.pickupTitle
                is PendingGeotagCapture.Warehouse -> UiStrings.Geotag.warehouseTitle
                is PendingGeotagCapture.EmptyContainer -> UiStrings.Geotag.emptyContainerTitle
            }
            val hint = when (pending) {
                is PendingGeotagCapture.Pickup -> UiStrings.Geotag.pickupHint
                is PendingGeotagCapture.Warehouse -> UiStrings.Geotag.warehouseHint
                is PendingGeotagCapture.EmptyContainer -> UiStrings.Geotag.emptyContainerHint
            }
            val lat = when (pending) {
                is PendingGeotagCapture.Pickup -> pending.latitude
                is PendingGeotagCapture.Warehouse -> pending.latitude
                is PendingGeotagCapture.EmptyContainer -> pending.latitude
            }
            val lng = when (pending) {
                is PendingGeotagCapture.Pickup -> pending.longitude
                is PendingGeotagCapture.Warehouse -> pending.longitude
                is PendingGeotagCapture.EmptyContainer -> pending.longitude
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
                                    val container = repository.confirmPickupWithGeotag(
                                        containerUuid,
                                        pending.latitude,
                                        pending.longitude,
                                        pending.label,
                                        jpeg,
                                        capturedAt,
                                    )
                                    track = track?.applyContainerListUpdate(container)
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
                                    track = track?.applyContainerListUpdate(result.container)
                                    runCatching {
                                        handoffQr = repository.deliveryHandoffQr(containerUuid)
                                        handoffQrWaiting = null
                                    }.onFailure { e ->
                                        handoffQrWaiting = e.message?.ifBlank { null }
                                            ?: UiStrings.Map.summaryGeneratingQr
                                    }
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
                            is PendingGeotagCapture.EmptyContainer -> {
                                emptyContainerBusy = true
                                actionMessage = null
                                runCatching {
                                    val container = repository.submitEmptyContainerGeotag(
                                        containerUuid,
                                        pending.latitude,
                                        pending.longitude,
                                        jpeg,
                                        capturedAt,
                                    )
                                    track = track?.applyContainerListUpdate(container)
                                    loadTrack()
                                    actionMessage = UiStrings.Loads.emptyContainerProofSaved
                                }.onFailure { actionMessage = it.message }
                                emptyContainerBusy = false
                            }
                        }
                    }
                },
            )
        }

    pendingIncidentGeotag?.let { pending ->
        key(pending.step) {
        GeotagCaptureScreen(
            latitude = pending.latitude,
            longitude = pending.longitude,
            title = UiStrings.Geotag.incidentPhotoTitle,
            hint = UiStrings.Geotag.incidentPhotoHint(pending.step),
            captureSessionKey = pending.step,
            onCancel = { pendingIncidentGeotag = null },
            onCaptured = { jpeg ->
                val capturedAt = Instant.now()
                if (pending.step == 1) {
                    pendingIncidentGeotag = pending.copy(step = 2, photo1 = jpeg)
                } else {
                    val photo1 = pending.photo1
                    if (photo1 == null) {
                        pendingIncidentGeotag = null
                        actionMessage = UiStrings.Incident.locationRequired
                        return@GeotagCaptureScreen
                    }
                    pendingIncidentGeotag = null
                    scope.launch {
                        incidentSubmitting = true
                        actionMessage = UiStrings.Incident.submitting
                        runCatching {
                            repository.submitDriverIncidentReport(
                                containerUuid,
                                pending.categoryCode,
                                pending.notes,
                                pending.latitude,
                                pending.longitude,
                                photo1,
                                jpeg,
                                capturedAt,
                            )
                            actionMessage = UiStrings.Incident.submitted
                        }.onFailure {
                            actionMessage = it.userMessage(UiStrings.Incident.submitFailed)
                        }
                        incidentSubmitting = false
                    }
                }
            },
        )
        }
    }

    if (showIncidentSheet) {
        DriverIncidentReportSheet(
            onDismiss = { showIncidentSheet = false },
            onStartPhotos = { categoryCode, notes ->
                val lat = currentLat
                val lng = currentLng
                if (lat == null || lng == null) {
                    actionMessage = UiStrings.Incident.locationRequired
                    return@DriverIncidentReportSheet
                }
                // Set pending capture before dismissing sheet so the map stays suspended
                // (no Google Maps re-init competing with CameraX on Vivo/OEM devices).
                openIncidentGeotagCapture(
                    PendingIncidentGeotag(
                        step = 1,
                        categoryCode = categoryCode,
                        notes = notes,
                        latitude = lat,
                        longitude = lng,
                    ),
                )
                showIncidentSheet = false
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
