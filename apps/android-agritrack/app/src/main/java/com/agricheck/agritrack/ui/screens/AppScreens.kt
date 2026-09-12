package com.agricheck.agritrack.ui.screens

import android.Manifest
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.data.model.ContainerListItemDto
import com.agricheck.agritrack.data.model.DriverDashboardDto
import com.agricheck.agritrack.data.model.DriverProfileDto
import com.agricheck.agritrack.data.model.UserSummaryDto
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.location.LocationTrackingService
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.theme.AgriColors
import kotlinx.coroutines.launch

@Composable
fun DashboardScreen(user: UserSummaryDto?, repository: DriverRepository) {
    var dashboard by remember { mutableStateOf<DriverDashboardDto?>(null) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    fun load() {
        scope.launch {
            loading = true
            error = null
            runCatching { repository.dashboard() }
                .onSuccess { dashboard = it }
                .onFailure { error = it.message }
            loading = false
        }
    }

    LaunchedEffect(Unit) { load() }

    AgriScreenScaffold(
        title = "AgriTrack",
        subtitle = user?.let { "Welcome, ${it.firstName}" },
        refreshing = loading,
        onRefresh = { load() },
    ) { padding ->
        when {
            loading && dashboard == null -> LoadingState()
            error != null && dashboard == null -> ErrorState(error ?: "Failed") { load() }
            else -> {
                Column(
                    Modifier.padding(padding).padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    ElevatedCard(shape = MaterialTheme.shapes.large, modifier = Modifier.fillMaxWidth()) {
                        Column(Modifier.padding(16.dp)) {
                            Text("Transport overview", fontWeight = FontWeight.Bold, color = AgriColors.Primary)
                            Text(
                                "Monitor assigned loads, GPS tracking, and warehouse delivery status.",
                                color = AgriColors.TextSecondary,
                                style = MaterialTheme.typography.bodySmall,
                            )
                        }
                    }
                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.fillMaxWidth()) {
                        MetricCard("Assigned", "${dashboard?.assignedContainers ?: 0}", Modifier.weight(1f))
                        MetricCard("In transit", "${dashboard?.inTransitContainers ?: 0}", Modifier.weight(1f))
                    }
                    MetricCard("Profile completion", "${dashboard?.profileCompletion ?: 0}%")
                }
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun ContainersScreen(
    repository: DriverRepository,
    onOpenMap: (ContainerListItemDto) -> Unit,
) {
    val context = LocalContext.current
    var containers by remember { mutableStateOf<List<ContainerListItemDto>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var actionMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions(),
    ) { /* handled in action */ }

    fun load() {
        scope.launch {
            loading = true
            error = null
            runCatching { repository.assignedContainers() }
                .onSuccess { containers = it }
                .onFailure { error = it.message }
            loading = false
        }
    }

    LaunchedEffect(Unit) { load() }

    AgriScreenScaffold(
        title = "My Loads",
        subtitle = "Update status, GPS, and live map",
        refreshing = loading,
        onRefresh = { load() },
    ) { padding ->
        LazyColumn(
            Modifier.padding(padding).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            item { AgriGuidanceBanner("Set In Transit to start background GPS. Tap a load for the live OSRM map.") }
            actionMessage?.let { item { Text(it, color = AgriColors.Success, fontWeight = FontWeight.SemiBold) } }
            if (error != null && containers.isEmpty()) item { ErrorState(error ?: "Failed") { load() } }
            if (!loading && containers.isEmpty() && error == null) item { EmptyState("No assigned containers.") }

            items(containers, key = { it.uuid }) { container ->
                AgriListItemCard(
                    title = container.containerNumber,
                    subtitle = "Entry ${container.entryReference}",
                    status = container.status,
                    onClick = { onOpenMap(container) },
                )
                container.lastLatitude?.let { lat ->
                    Text(
                        "Last GPS: ${"%.5f".format(lat)}, ${"%.5f".format(container.lastLongitude)}",
                        style = MaterialTheme.typography.bodySmall,
                        color = AgriColors.TextSecondary,
                    )
                }
                FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
                    Button(
                        onClick = {
                            val perms = mutableListOf(
                                Manifest.permission.ACCESS_FINE_LOCATION,
                                Manifest.permission.ACCESS_COARSE_LOCATION,
                            )
                            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                                perms.add(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
                            }
                            permissionLauncher.launch(perms.toTypedArray())
                            scope.launch {
                                runCatching {
                                    LocationTrackingService.start(context, container.uuid)
                                    repository.updateStatus(container.uuid, "InTransit")
                                }
                                    .onSuccess { actionMessage = "${container.containerNumber} is in transit"; load() }
                                    .onFailure { actionMessage = it.message }
                            }
                        },
                        colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
                    ) { Text("In Transit") }
                    OutlinedButton(onClick = {
                        scope.launch {
                            runCatching {
                                repository.updateStatus(container.uuid, "AtWarehouse")
                                LocationTrackingService.stop(context)
                            }
                                .onSuccess { actionMessage = "Arrived at warehouse"; load() }
                                .onFailure { actionMessage = it.message }
                        }
                    }) { Text("At Warehouse") }
                    OutlinedButton(onClick = { onOpenMap(container) }) { Text("Live Map") }
                }
            }
        }
    }
}

@Composable
fun ProfileScreen(
    user: UserSummaryDto?,
    repository: DriverRepository,
    onLogout: () -> Unit,
    onOpenSettings: () -> Unit,
) {
    var profile by remember { mutableStateOf<DriverProfileDto?>(null) }
    var loading by remember { mutableStateOf(true) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        scope.launch {
            runCatching { repository.profile() }.onSuccess { profile = it }
            loading = false
        }
    }

    AgriScreenScaffold(title = "Profile", onLogout = onLogout) { padding ->
        Column(Modifier.padding(padding).padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            ElevatedCard(shape = MaterialTheme.shapes.large) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text(user?.let { "${it.firstName} ${it.lastName}" } ?: "Driver", fontWeight = FontWeight.Bold, style = MaterialTheme.typography.titleLarge)
                    Text(user?.email ?: "", color = AgriColors.TextSecondary)
                    Text("Roles: ${user?.roles?.joinToString() ?: ""}", style = MaterialTheme.typography.bodySmall)
                }
            }
            if (!loading && profile != null) {
                AgriSectionTitle("Driver profile")
                MetricCard("Completion", "${profile?.completionPercentage ?: 0}%")
                profile?.phoneNumber?.let { Text("Phone: $it") }
                profile?.vehicleRegistration?.let { Text("Vehicle: $it") }
            }
            OutlinedButton(onClick = onOpenSettings, modifier = Modifier.fillMaxWidth()) {
                Text("API settings")
            }
        }
    }
}

@Composable
fun MoreScreen(onOpenSettings: () -> Unit) {
    AgriScreenScaffold(title = "More") { padding ->
        Column(Modifier.padding(padding).padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            AgriSectionTitle("AgriCheck V3")
            Text("AgriTrack is the official mobile companion for container transport operations.")
            OutlinedButton(onClick = onOpenSettings, modifier = Modifier.fillMaxWidth()) { Text("API settings") }
            Text("Version 1.1.0 · Native Android", color = AgriColors.TextSecondary, style = MaterialTheme.typography.bodySmall)
        }
    }
}
