package com.agricheck.agritrack.ui.screens

import android.Manifest
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Map
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.VerifiedUser
import androidx.compose.material.icons.outlined.Info
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.agricheck.agritrack.data.model.ContainerListItemDto
import com.agricheck.agritrack.data.model.DriverDashboardDto
import com.agricheck.agritrack.data.model.DriverProfileDto
import com.agricheck.agritrack.data.model.UserSummaryDto
import com.agricheck.agritrack.data.repository.DriverRepository
import com.agricheck.agritrack.AgriTrackApp
import com.agricheck.agritrack.BuildConfig
import com.agricheck.agritrack.data.local.PersistedNavigationSession
import com.agricheck.agritrack.location.AppLocationManager
import com.agricheck.agritrack.location.LocationTrackingService
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.util.MapsConfig
import com.agricheck.agritrack.util.*
import kotlinx.coroutines.launch

@Composable
fun DashboardScreen(
    user: UserSummaryDto?,
    repository: DriverRepository,
    onOpenMap: (ContainerListItemDto) -> Unit,
    onOpenQr: (ContainerListItemDto) -> Unit,
) {
    var dashboard by remember { mutableStateOf<DriverDashboardDto?>(null) }
    var assigned by remember { mutableStateOf<List<ContainerListItemDto>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val context = LocalContext.current
    val trackingStore = (context.applicationContext as AgriTrackApp).container.trackingStore
    val navSession by trackingStore.navigationSessionFlow.collectAsState(initial = null)
    val active = remember(assigned) { pickActiveDelivery(assigned) }
    val dockDelivery = remember(assigned, navSession, active) {
        active ?: navSession?.let { ns -> assigned.find { it.uuid == ns.containerUuid } }
    }
    val showDock = dockDelivery != null || navSession != null

    fun load() {
        scope.launch {
            loading = true
            error = null
            runCatching {
                dashboard = repository.dashboard()
                assigned = repository.assignedContainers()
            }.onFailure { error = it.message }
            loading = false
        }
    }

    LaunchedEffect(Unit) { load() }

    Box(
        Modifier
            .fillMaxSize()
            .background(AgriColors.Background),
    ) {
        when {
            loading && dashboard == null -> LoadingState()
            error != null && dashboard == null -> ErrorState(error ?: "Failed") { load() }
            else -> {
                Column(
                    Modifier
                        .fillMaxSize()
                        .padding(horizontal = 20.dp, vertical = 16.dp)
                        .padding(bottom = if (showDock) 108.dp else 0.dp),
                ) {
                    Text(
                        UiStrings.Home.greeting,
                        style = MaterialTheme.typography.bodyMedium,
                        color = AgriColors.TextSecondary,
                    )
                    Text(
                        user?.firstName ?: UiStrings.appName,
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold,
                        color = AgriColors.OnSurface,
                    )
                    Spacer(Modifier.height(22.dp))
                    Row(
                        Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                    ) {
                        HomeShortcut(
                            icon = Icons.Default.Map,
                            label = UiStrings.Home.shortcutMap,
                            modifier = Modifier.weight(1f),
                            onClick = { active?.let(onOpenMap) ?: assigned.firstOrNull()?.let(onOpenMap) },
                        )
                        HomeShortcut(
                            icon = Icons.Default.QrCodeScanner,
                            label = UiStrings.Home.shortcutQr,
                            modifier = Modifier.weight(1f),
                            onClick = {
                                val target = active ?: assigned.firstOrNull()
                                if (target != null) onOpenQr(target)
                            },
                        )
                        HomeShortcut(
                            icon = Icons.Default.LocalShipping,
                            label = UiStrings.Home.shortcutLoads,
                            modifier = Modifier.weight(1f),
                            onClick = { assigned.firstOrNull()?.let(onOpenMap) },
                        )
                    }
                    Spacer(Modifier.height(22.dp))
                    Text(
                        UiStrings.Home.summary,
                        fontWeight = FontWeight.SemiBold,
                        color = AgriColors.OnSurface,
                    )
                    Spacer(Modifier.height(10.dp))
                    Row(
                        Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(10.dp),
                    ) {
                        HomeStatChip(UiStrings.Home.assigned, "${dashboard?.assignedContainers ?: 0}", Modifier.weight(1f))
                        HomeStatChip(UiStrings.Home.inTransit, "${dashboard?.inTransitContainers ?: 0}", Modifier.weight(1f))
                        HomeStatChip(UiStrings.Home.profileCompletion, "${dashboard?.profileCompletion ?: 0}%", Modifier.weight(1f))
                    }
                }
            }
        }

        if (showDock) {
            ActiveDeliveryDock(
                delivery = dockDelivery,
                navSession = navSession,
                onOpenMap = {
                    val target = dockDelivery
                    val currentSession = navSession
                    if (target != null) {
                        onOpenMap(target)
                    } else if (currentSession != null) {
                        onOpenMap(
                            ContainerListItemDto(
                                uuid = currentSession.containerUuid,
                                containerNumber = currentSession.containerNumber,
                                entryUuid = "",
                                entryReference = "",
                                status = "Assigned",
                                pickup = null,
                                destination = null,
                            ),
                        )
                    }
                },
                onOpenQr = {
                    dockDelivery?.let(onOpenQr)
                },
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .fillMaxWidth()
                    .padding(horizontal = 12.dp, vertical = 10.dp),
            )
        }
    }
}

@Composable
private fun HomeShortcut(
    icon: ImageVector,
    label: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Surface(
        modifier = modifier.clickable(onClick = onClick),
        shape = RoundedCornerShape(20.dp),
        color = Color.White,
        shadowElevation = 1.dp,
        tonalElevation = 0.dp,
    ) {
        Column(
            Modifier.padding(vertical = 18.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Box(
                Modifier
                    .size(44.dp)
                    .clip(CircleShape)
                    .background(AgriColors.Primary.copy(alpha = 0.12f)),
                contentAlignment = Alignment.Center,
            ) {
                Icon(icon, contentDescription = label, tint = AgriColors.PrimaryDark)
            }
            Spacer(Modifier.height(8.dp))
            Text(label, fontWeight = FontWeight.SemiBold, fontSize = 13.sp, color = AgriColors.OnSurface)
        }
    }
}

@Composable
private fun HomeStatChip(label: String, value: String, modifier: Modifier = Modifier) {
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(16.dp),
        color = Color.White,
    ) {
        Column(Modifier.padding(horizontal = 12.dp, vertical = 14.dp)) {
            Text(label, color = AgriColors.TextSecondary, style = MaterialTheme.typography.labelSmall)
            Spacer(Modifier.height(4.dp))
            Text(value, fontWeight = FontWeight.Bold, fontSize = 20.sp, color = AgriColors.PrimaryDark)
        }
    }
}

@Composable
private fun ActiveDeliveryDock(
    delivery: ContainerListItemDto?,
    navSession: PersistedNavigationSession?,
    onOpenMap: () -> Unit,
    onOpenQr: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val navigationActive = navSession != null
    val phase = delivery?.status?.let { deliveryPhase(it) }
    val containerNumber = delivery?.containerNumber ?: navSession?.containerNumber ?: ""
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(22.dp),
        color = Color.White,
        shadowElevation = 10.dp,
    ) {
        Row(
            Modifier.padding(horizontal = 14.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Box(
                Modifier
                    .size(40.dp)
                    .clip(CircleShape)
                    .background(AgriColors.Primary.copy(alpha = 0.14f)),
                contentAlignment = Alignment.Center,
            ) {
                Icon(Icons.Default.Place, contentDescription = null, tint = AgriColors.PrimaryDark)
            }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Text(
                    if (navigationActive) UiStrings.Home.navigationActive else UiStrings.Home.currentDelivery,
                    style = MaterialTheme.typography.labelSmall,
                    color = if (navigationActive) AgriColors.PrimaryDark else AgriColors.TextSecondary,
                )
                Text(
                    containerNumber,
                    fontWeight = FontWeight.Bold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
                val routeLine = when {
                    navSession != null && !navSession.destinationLabel.isNullOrBlank() ->
                        UiStrings.Home.navigatingTo(navSession.destinationLabel)
                    delivery != null ->
                        "${delivery.pickup?.name ?: UiStrings.Map.pickup} → ${delivery.destination?.name ?: UiStrings.Map.destination}"
                    else -> UiStrings.Map.continueRoute
                }
                Text(
                    routeLine,
                    style = MaterialTheme.typography.bodySmall,
                    color = AgriColors.TextSecondary,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
            Spacer(Modifier.width(8.dp))
            if (phase != null && canShowDeliveryQr(phase)) {
                IconButton(onClick = onOpenQr, modifier = Modifier.size(36.dp)) {
                    Icon(Icons.Default.QrCodeScanner, contentDescription = UiStrings.Home.showQr, tint = AgriColors.PrimaryDark)
                }
            }
            Button(
                onClick = onOpenMap,
                shape = RoundedCornerShape(12.dp),
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
            ) {
                Text(UiStrings.Home.go, fontWeight = FontWeight.Bold)
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun ContainersScreen(
    repository: DriverRepository,
    onOpenMap: (ContainerListItemDto) -> Unit,
    onOpenQr: (ContainerListItemDto) -> Unit,
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
        title = UiStrings.Loads.title,
        nestedInMainShell = true,
        refreshing = loading,
        onRefresh = { load() },
    ) { padding ->
        LazyColumn(
            Modifier.padding(padding).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            actionMessage?.let { item { Text(it, color = AgriColors.Success, fontWeight = FontWeight.SemiBold) } }
            if (error != null && containers.isEmpty()) item { ErrorState(error ?: "Failed") { load() } }
            if (!loading && containers.isEmpty() && error == null) item { EmptyState(UiStrings.Loads.empty) }

            items(containers, key = { it.uuid }) { container ->
                val phase = deliveryPhase(container.status)
                Surface(
                    shape = RoundedCornerShape(20.dp),
                    color = Color.White,
                    shadowElevation = 1.dp,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Column(Modifier.weight(1f)) {
                                Text(
                                    container.containerNumber,
                                    fontWeight = FontWeight.Bold,
                                    color = AgriColors.OnSurface,
                                )
                                Text(
                                    "${UiStrings.Loads.entry} ${container.entryReference}",
                                    color = AgriColors.TextSecondary,
                                    style = MaterialTheme.typography.bodySmall,
                                )
                            }
                            StatusChip(UiStrings.deliveryPhaseLabel(phase))
                        }
                        container.pickup?.let {
                            LoadRouteLine(
                                UiStrings.Loads.pickupLabel,
                                it.name,
                                AgriColors.PrimaryDark,
                            )
                        }
                        container.destination?.let {
                            LoadRouteLine(
                                UiStrings.Loads.destinationLabel,
                                it.name,
                                AgriColors.TextSecondary,
                            )
                        }
                        container.lastLatitude?.let { lat ->
                            Text(
                                "${UiStrings.Loads.lastGps}: ${"%.5f".format(lat)}, ${"%.5f".format(container.lastLongitude)}",
                                style = MaterialTheme.typography.bodySmall,
                                color = AgriColors.TextSecondary,
                            )
                        }
                        FlowRow(
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                            modifier = Modifier.fillMaxWidth(),
                        ) {
                    if (canConfirmPickup(phase)) {
                        OutlinedButton(onClick = {
                            val perms = arrayOf(
                                Manifest.permission.ACCESS_FINE_LOCATION,
                                Manifest.permission.ACCESS_COARSE_LOCATION,
                            )
                            permissionLauncher.launch(perms)
                            AppLocationManager.getCurrentLocation(context) { loc ->
                                if (loc == null) {
                                    actionMessage = UiStrings.Loads.locationRequired
                                    return@getCurrentLocation
                                }
                                scope.launch {
                                    runCatching {
                                        repository.confirmPickup(
                                            container.uuid,
                                            loc.latitude,
                                            loc.longitude,
                                            container.pickup?.name,
                                        )
                                    }
                                        .onSuccess { actionMessage = UiStrings.Loads.pickupConfirmed; load() }
                                        .onFailure { actionMessage = it.message }
                                }
                            }
                        }) { Text(UiStrings.Loads.confirmPickup) }
                    }
                    if (canStartTransit(phase)) {
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
                                        .onSuccess { actionMessage = UiStrings.Loads.inTransitMessage(container.containerNumber); load() }
                                        .onFailure { actionMessage = it.message }
                                }
                            },
                            colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
                        ) { Text(UiStrings.Loads.startDelivery) }
                    }
                    if (canShowDeliveryQr(phase)) {
                        OutlinedButton(onClick = { onOpenQr(container) }) { Text(UiStrings.Loads.deliveryQr) }
                    }
                    OutlinedButton(onClick = { onOpenMap(container) }) { Text(UiStrings.Loads.deliveryMap) }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun LoadRouteLine(label: String, name: String, labelColor: Color) {
    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.Top) {
        Text(
            "$label: ",
            style = MaterialTheme.typography.bodySmall,
            fontWeight = FontWeight.SemiBold,
            color = labelColor,
        )
        Text(
            name,
            style = MaterialTheme.typography.bodySmall,
            color = AgriColors.OnSurface,
            modifier = Modifier.weight(1f),
        )
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
    val displayName = user?.let { "${it.firstName} ${it.lastName}".trim() }
        ?: profile?.fullName
        ?: UiStrings.Profile.driver

    LaunchedEffect(Unit) {
        scope.launch {
            runCatching { repository.profile() }.onSuccess { profile = it }
            loading = false
        }
    }

    val completion = profile?.completionPercentage ?: 0

    LazyColumn(
        Modifier
            .fillMaxSize()
            .background(AgriColors.Background)
            .padding(horizontal = 20.dp, vertical = 16.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        item {
            Surface(
                shape = RoundedCornerShape(22.dp),
                color = Color.White,
                shadowElevation = 2.dp,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Column(
                    Modifier.padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                ) {
                    Box(
                        Modifier
                            .size(88.dp)
                            .clip(CircleShape)
                            .background(AgriColors.Primary.copy(alpha = 0.14f)),
                        contentAlignment = Alignment.Center,
                    ) {
                        Text(
                            displayName.take(1).uppercase(),
                            fontWeight = FontWeight.Bold,
                            fontSize = 32.sp,
                            color = AgriColors.PrimaryDark,
                        )
                    }
                    Spacer(Modifier.height(12.dp))
                    Text(displayName, fontWeight = FontWeight.Bold, fontSize = 20.sp, color = AgriColors.OnSurface)
                    Text(
                        user?.email ?: "",
                        color = AgriColors.TextSecondary,
                        style = MaterialTheme.typography.bodyMedium,
                    )
                    Spacer(Modifier.height(16.dp))
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text(
                            UiStrings.Profile.completion,
                            style = MaterialTheme.typography.labelMedium,
                            color = AgriColors.TextSecondary,
                        )
                        Text(
                            "$completion%",
                            fontWeight = FontWeight.Bold,
                            color = AgriColors.PrimaryDark,
                        )
                    }
                    Spacer(Modifier.height(8.dp))
                    LinearProgressIndicator(
                        progress = { (completion.coerceIn(0, 100) / 100f) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(8.dp)
                            .clip(RoundedCornerShape(999.dp)),
                        color = AgriColors.Primary,
                        trackColor = AgriColors.Primary.copy(alpha = 0.12f),
                    )
                }
            }
        }
        if (loading && profile == null) {
            item {
                Box(Modifier.fillMaxWidth().padding(24.dp), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator(color = AgriColors.Primary, modifier = Modifier.size(28.dp), strokeWidth = 3.dp)
                }
            }
        } else {
            item { AgriSectionTitle(UiStrings.Profile.details) }
            item {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    profile?.licenseNumber?.let {
                        ProfileInfoCard(UiStrings.Profile.license, it)
                    }
                    profile?.phoneNumber?.let {
                        ProfileInfoCard(UiStrings.Profile.phone, it)
                    }
                    profile?.operatorName?.let {
                        ProfileInfoCard(UiStrings.Profile.fleet, it)
                    }
                    Surface(
                        shape = RoundedCornerShape(16.dp),
                        color = Color.White,
                        modifier = Modifier.fillMaxWidth(),
                    ) {
                        Row(
                            Modifier.padding(horizontal = 16.dp, vertical = 14.dp),
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Icon(
                                Icons.Default.VerifiedUser,
                                contentDescription = null,
                                tint = if (profile?.faceVerified == true) AgriColors.Success else AgriColors.Warning,
                            )
                            Spacer(Modifier.width(12.dp))
                            Text(
                                if (profile?.faceVerified == true) {
                                    UiStrings.Profile.faceVerified
                                } else {
                                    UiStrings.Profile.facePending
                                },
                                fontWeight = FontWeight.SemiBold,
                                color = AgriColors.OnSurface,
                            )
                        }
                    }
                    AgriMenuRow(
                        title = UiStrings.Profile.apiSettings,
                        subtitle = UiStrings.Settings.apiSection,
                        onClick = onOpenSettings,
                    )
                }
            }
        }
        item {
            OutlinedButton(
                onClick = onLogout,
                modifier = Modifier.fillMaxWidth().height(52.dp),
                shape = RoundedCornerShape(14.dp),
                colors = ButtonDefaults.outlinedButtonColors(contentColor = AgriColors.Error),
                border = androidx.compose.foundation.BorderStroke(1.dp, AgriColors.Error.copy(alpha = 0.35f)),
            ) {
                Text(UiStrings.Profile.logout, fontWeight = FontWeight.SemiBold)
            }
        }
        item { Spacer(Modifier.height(8.dp)) }
    }
}

@Composable
private fun ProfileInfoCard(label: String, value: String) {
    Surface(shape = RoundedCornerShape(16.dp), color = Color.White, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(horizontal = 16.dp, vertical = 14.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = AgriColors.TextSecondary)
            Spacer(Modifier.height(4.dp))
            Text(value, fontWeight = FontWeight.SemiBold, color = AgriColors.OnSurface)
        }
    }
}

@Composable
fun SettingsHubScreen(
    user: UserSummaryDto?,
    showOperator: Boolean,
    onOpenProfile: () -> Unit,
    onOpenOperator: () -> Unit,
    onOpenApiSettings: () -> Unit,
) {
    val context = LocalContext.current
    val mapsStatus = when {
        MapsConfig.useGoogleMapTilesInApp(context) -> UiStrings.More.mapsTilesOn
        MapsConfig.isGoogleMapsConfigured() -> UiStrings.More.mapsKeyOnly
        else -> UiStrings.More.mapsOff
    }
    val mapsOk = MapsConfig.isGoogleMapsConfigured() || MapsConfig.useGoogleMapTilesInApp(context)
    val displayName = user?.let { "${it.firstName} ${it.lastName}".trim() } ?: UiStrings.Profile.driver

    Column(
        Modifier
            .fillMaxSize()
            .background(AgriColors.Background)
            .padding(horizontal = 20.dp, vertical = 16.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        Surface(
            shape = RoundedCornerShape(20.dp),
            color = Color.White,
            shadowElevation = 1.dp,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Row(
                Modifier.padding(16.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Box(
                    Modifier
                        .size(48.dp)
                        .clip(CircleShape)
                        .background(AgriColors.Primary.copy(alpha = 0.14f)),
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(Icons.Default.VerifiedUser, contentDescription = null, tint = AgriColors.PrimaryDark)
                }
                Spacer(Modifier.width(12.dp))
                Column(Modifier.weight(1f)) {
                    Text(displayName, fontWeight = FontWeight.Bold, color = AgriColors.OnSurface)
                    Text(UiStrings.Nav.account, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                }
                TextButton(onClick = onOpenProfile) {
                    Text(UiStrings.Profile.details, fontWeight = FontWeight.SemiBold)
                }
            }
        }
        if (showOperator) {
            AgriMenuRow(
                title = UiStrings.Operator.title,
                subtitle = UiStrings.Nav.operator,
                onClick = onOpenOperator,
            )
        }
        Surface(
            shape = RoundedCornerShape(20.dp),
            color = Color.White,
            shadowElevation = 1.dp,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(UiStrings.More.aboutTitle, fontWeight = FontWeight.Bold)
                Text(UiStrings.More.aboutBody, color = AgriColors.TextSecondary)
                Surface(
                    color = (if (mapsOk) AgriColors.Success else AgriColors.Warning).copy(alpha = 0.1f),
                    shape = RoundedCornerShape(12.dp),
                ) {
                    Text(
                        mapsStatus,
                        modifier = Modifier.padding(12.dp),
                        color = if (mapsOk) AgriColors.Success else AgriColors.Warning,
                        style = MaterialTheme.typography.bodySmall,
                    )
                }
            }
        }
        AgriMenuRow(
            title = UiStrings.More.apiSettings,
            subtitle = UiStrings.Settings.apiSection,
            onClick = onOpenApiSettings,
        )
        Text(
            UiStrings.More.version(BuildConfig.VERSION_NAME),
            color = AgriColors.TextSecondary,
            style = MaterialTheme.typography.bodySmall,
        )
    }
}

@Composable
fun MoreScreen(onOpenSettings: () -> Unit) {
    val context = LocalContext.current
    val mapsStatus = when {
        MapsConfig.useGoogleMapTilesInApp(context) -> UiStrings.More.mapsTilesOn
        MapsConfig.isGoogleMapsConfigured() -> UiStrings.More.mapsKeyOnly
        else -> UiStrings.More.mapsOff
    }
    val mapsOk = MapsConfig.isGoogleMapsConfigured() || MapsConfig.useGoogleMapTilesInApp(context)

    AgriScreenScaffold(title = UiStrings.More.title, nestedInMainShell = true) { padding ->
        Column(
            Modifier
                .padding(padding)
                .padding(horizontal = 20.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            Surface(
                shape = RoundedCornerShape(20.dp),
                color = Color.White,
                shadowElevation = 1.dp,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Outlined.Info, contentDescription = null, tint = AgriColors.PrimaryDark)
                        Spacer(Modifier.width(10.dp))
                        Text(UiStrings.More.aboutTitle, fontWeight = FontWeight.Bold, color = AgriColors.OnSurface)
                    }
                    Text(UiStrings.More.aboutBody, color = AgriColors.OnSurface, style = MaterialTheme.typography.bodyMedium)
                    Surface(
                        color = (if (mapsOk) AgriColors.Success else AgriColors.Warning).copy(alpha = 0.1f),
                        shape = RoundedCornerShape(12.dp),
                    ) {
                        Text(
                            mapsStatus,
                            modifier = Modifier.padding(12.dp),
                            color = if (mapsOk) AgriColors.Success else AgriColors.Warning,
                            style = MaterialTheme.typography.bodySmall,
                            fontWeight = FontWeight.Medium,
                        )
                    }
                }
            }
            AgriMenuRow(
                title = UiStrings.More.apiSettings,
                subtitle = UiStrings.Settings.apiSection,
                onClick = onOpenSettings,
            )
            Text(
                UiStrings.More.version(BuildConfig.VERSION_NAME),
                color = AgriColors.TextSecondary,
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.padding(start = 4.dp, top = 4.dp),
            )
        }
    }
}
