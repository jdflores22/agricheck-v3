package com.agricheck.agritrack.ui.screens

import android.Manifest
import android.annotation.SuppressLint
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.data.api.OsrmClient
import com.agricheck.agritrack.data.model.ContainerTrackDto
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.location.LocationTrackingService
import com.agricheck.agritrack.ui.components.LoadingState
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.util.GeoUtils
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import com.google.android.gms.tasks.CancellationTokenSource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import org.osmdroid.tileprovider.tilesource.TileSourceFactory
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker
import org.osmdroid.views.overlay.Polyline
import androidx.compose.ui.viewinterop.AndroidView
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MapScreen(
    containerUuid: String,
    containerNumber: String,
    repository: DriverRepository,
    onBack: () -> Unit,
) {
    val context = LocalContext.current
    var track by remember { mutableStateOf<ContainerTrackDto?>(null) }
    var routePoints by remember { mutableStateOf<List<Pair<Double, Double>>>(emptyList()) }
    var routeMeta by remember { mutableStateOf<String?>(null) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var checkInMessage by remember { mutableStateOf<String?>(null) }
    var checkInLoading by remember { mutableStateOf(false) }
    var currentLat by remember { mutableStateOf<Double?>(null) }
    var currentLng by remember { mutableStateOf<Double?>(null) }
    val fusedLocation = remember { LocationServices.getFusedLocationProviderClient(context) }
    val scope = rememberCoroutineScope()
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { }

    @SuppressLint("MissingPermission")
    fun refreshCurrentLocation() {
        fusedLocation.lastLocation.addOnSuccessListener { location ->
            if (location != null) {
                currentLat = location.latitude
                currentLng = location.longitude
            }
        }
        fusedLocation.getCurrentLocation(Priority.PRIORITY_HIGH_ACCURACY, CancellationTokenSource().token)
            .addOnSuccessListener { location ->
                if (location != null) {
                    currentLat = location.latitude
                    currentLng = location.longitude
                }
            }
    }

    suspend fun loadTrack() {
        loading = true
        error = null
        runCatching { repository.track(containerUuid) }
            .onSuccess { data ->
                track = data
                val fromLat = data.currentLatitude ?: data.trail.lastOrNull()?.latitude
                val fromLng = data.currentLongitude ?: data.trail.lastOrNull()?.longitude
                if (fromLat != null && fromLng != null) {
                    val route = withContext(Dispatchers.IO) {
                        OsrmClient.fetchDrivingRoute(fromLat, fromLng, data.destination.latitude, data.destination.longitude)
                    }
                    routePoints = route?.points ?: emptyList()
                    routeMeta = route?.let {
                        "Route: ${OsrmClient.formatDistance(it.distanceMeters)} · ${OsrmClient.formatDuration(it.durationSeconds)}"
                    }
                } else {
                    routePoints = emptyList()
                    routeMeta = null
                }
            }
            .onFailure {
                error = it.message
                routePoints = emptyList()
            }
        loading = false
    }

    LaunchedEffect(containerUuid) {
        permissionLauncher.launch(
            arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION),
        )
        loadTrack()
        refreshCurrentLocation()
        while (true) {
            delay(30_000)
            loadTrack()
            refreshCurrentLocation()
        }
    }

    val distanceMeters = remember(track, currentLat, currentLng) {
        val data = track
        val lat = currentLat ?: data?.currentLatitude ?: data?.trail?.lastOrNull()?.latitude
        val lng = currentLng ?: data?.currentLongitude ?: data?.trail?.lastOrNull()?.longitude
        if (lat != null && lng != null && data != null) {
            GeoUtils.distanceMeters(lat, lng, data.destination.latitude, data.destination.longitude)
        } else {
            null
        }
    }
    val withinGeofence = distanceMeters != null && distanceMeters <= GeoUtils.DEFAULT_WAREHOUSE_GEOFENCE_METERS

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(containerNumber, fontWeight = FontWeight.Bold)
                        Text(track?.destination?.name ?: "Loading…", style = MaterialTheme.typography.bodySmall)
                    }
                },
                navigationIcon = {
                    TextButton(onClick = onBack) { Text("Back", color = androidx.compose.ui.graphics.Color.White) }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = AgriColors.Primary,
                    titleContentColor = androidx.compose.ui.graphics.Color.White,
                ),
            )
        },
        containerColor = AgriColors.Background,
        floatingActionButton = {
            if (withinGeofence && track != null) {
                ExtendedFloatingActionButton(
                    onClick = {
                        val lat = currentLat ?: return@ExtendedFloatingActionButton
                        val lng = currentLng ?: return@ExtendedFloatingActionButton
                        checkInLoading = true
                        scope.launch {
                            runCatching { repository.checkInAtWarehouse(containerUuid, lat, lng) }
                                .onSuccess { result ->
                                    if (result.withinGeofence) {
                                        checkInMessage = "Checked in at ${track?.destination?.name}. Doctor notified."
                                        LocationTrackingService.stop(context)
                                        loadTrack()
                                    } else {
                                        checkInMessage = "Still ${result.distanceMeters.toInt()}m away. Move closer to warehouse."
                                    }
                                }
                                .onFailure { checkInMessage = it.message }
                            checkInLoading = false
                        }
                    },
                    containerColor = AgriColors.Primary,
                ) {
                    if (checkInLoading) {
                        CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                    } else {
                        Text("I'M HERE", fontWeight = FontWeight.Bold)
                    }
                }
            }
        },
    ) { padding ->
        when {
            loading && track == null -> LoadingState()
            error != null && track == null -> Box(Modifier.padding(padding)) {
                Text(error ?: "Failed", color = AgriColors.Error, modifier = Modifier.padding(16.dp))
            }
            else -> {
                Column(Modifier.padding(padding).fillMaxSize()) {
                    routeMeta?.let {
                        Text(it, modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp), fontWeight = FontWeight.SemiBold)
                    }
                    distanceMeters?.let {
                        Text(
                            "Distance to warehouse: ${it.toInt()} m",
                            modifier = Modifier.padding(horizontal = 16.dp),
                            color = if (withinGeofence) AgriColors.Success else AgriColors.TextSecondary,
                            fontWeight = FontWeight.SemiBold,
                        )
                    }
                    checkInMessage?.let {
                        Text(it, modifier = Modifier.padding(horizontal = 16.dp), color = AgriColors.Primary)
                    }
                    AndroidView(
                        modifier = Modifier.fillMaxSize(),
                        factory = {
                            MapView(context).apply {
                                setTileSource(TileSourceFactory.MAPNIK)
                                setMultiTouchControls(true)
                                controller.setZoom(11.0)
                            }
                        },
                        update = { map ->
                            map.overlays.clear()
                            val data = track ?: return@AndroidView

                            if (data.trail.isNotEmpty()) {
                                map.overlays.add(
                                    Polyline(map).apply {
                                        outlinePaint.color = android.graphics.Color.parseColor("#2563EB")
                                        outlinePaint.strokeWidth = 8f
                                        setPoints(data.trail.map { GeoPoint(it.latitude, it.longitude) })
                                    },
                                )
                            }
                            if (routePoints.isNotEmpty()) {
                                map.overlays.add(
                                    Polyline(map).apply {
                                        outlinePaint.color = android.graphics.Color.parseColor("#166534")
                                        outlinePaint.strokeWidth = 10f
                                        setPoints(routePoints.map { (lat, lng) -> GeoPoint(lat, lng) })
                                    },
                                )
                            }

                            val driverLat = data.currentLatitude ?: data.trail.lastOrNull()?.latitude
                            val driverLng = data.currentLongitude ?: data.trail.lastOrNull()?.longitude
                            if (driverLat != null && driverLng != null) {
                                map.overlays.add(
                                    Marker(map).apply {
                                        position = GeoPoint(driverLat, driverLng)
                                        title = "Current position"
                                    },
                                )
                            }
                            map.overlays.add(
                                Marker(map).apply {
                                    position = GeoPoint(data.destination.latitude, data.destination.longitude)
                                    title = data.destination.name
                                },
                            )

                            val focus = driverLat?.let { lat ->
                                driverLng?.let { lng -> GeoPoint(lat, lng) }
                            } ?: GeoPoint(data.destination.latitude, data.destination.longitude)
                            map.controller.animateTo(focus)
                            map.invalidate()
                        },
                    )
                }
            }
        }
    }
}
