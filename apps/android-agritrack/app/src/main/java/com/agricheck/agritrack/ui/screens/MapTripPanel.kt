package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Photo
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.Verified
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.foundation.background
import androidx.compose.ui.draw.clip
import androidx.compose.foundation.Image
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import coil.compose.AsyncImage
import com.agricheck.agritrack.data.model.ContainerDeliveryLegSummaryDto
import com.agricheck.agritrack.data.api.OsrmClient
import com.agricheck.agritrack.data.api.OsrmRouteResult
import com.agricheck.agritrack.data.model.ContainerTrackDto
import com.agricheck.agritrack.data.model.DriverDeliveryHandoffQrDto
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.util.ApiAssetUrl
import com.agricheck.agritrack.util.rememberHandoffQrImageBitmap
import com.agricheck.agritrack.util.TripNavLeg
import com.agricheck.agritrack.util.formatApiInstantForDisplay
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

private val RouteGreen = Color(0xFF15803D)
private val MutedCoord = Color(0xFF9CA3AF)
private val CardBorder = Color(0xFFE2E8F0)
private val NavyText = Color(0xFF0F172A)
private val BlueAccent = Color(0xFF2563EB)
private val PhilippinesZone = ZoneId.of("Asia/Manila")
private val LegTimeFormat = DateTimeFormatter.ofPattern("MMM d, yyyy · h:mm a", Locale.ENGLISH)

private fun formatLegTime(iso: String?): String? =
    formatApiInstantForDisplay(iso, PhilippinesZone, LegTimeFormat)

private fun formatCoordinates(lat: Double?, lng: Double?): String? {
    if (lat == null || lng == null) return null
    return String.format(Locale.US, "%.5f, %.5f", lat, lng)
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MapTripPanel(
    drivingMode: Boolean,
    route: OsrmRouteResult?,
    remainingDurationSeconds: Double?,
    remainingDistanceMeters: Double?,
    routeLoading: Boolean,
    track: ContainerTrackDto?,
    navLeg: TripNavLeg?,
    towardName: String,
    startButtonSub: String,
    showQr: Boolean,
    showPickupButton: Boolean,
    showArrivedButton: Boolean,
    pickupEnabled: Boolean,
    arrivedEnabled: Boolean,
    pickupDistanceHint: String?,
    arrivedDistanceHint: String?,
    waitingAtWarehouse: Boolean,
    pickupBusy: Boolean,
    arriveBusy: Boolean,
    showEmptyContainerButton: Boolean = false,
    emptyContainerEnabled: Boolean = true,
    emptyContainerBusy: Boolean = false,
    tripCompleted: Boolean = false,
    tripEndedMessage: String? = null,
    startNavigationBusy: Boolean,
    startNavigationEnabled: Boolean,
    startNavigationHint: String?,
    showInAppGoogleHint: Boolean = false,
    hideEtaWhenGoogleNavigation: Boolean = false,
    apiBaseUrl: String = "",
    handoffQr: DriverDeliveryHandoffQrDto? = null,
    handoffQrWaitingMessage: String? = null,
    onBack: () -> Unit,
    onStartNavigation: () -> Unit,
    onConfirmPickup: () -> Unit,
    onArrivedAtWarehouse: () -> Unit,
    onEmptyContainerProof: () -> Unit = {},
    onOpenQr: () -> Unit,
    trafficJamMinutes: Int = 0,
    trafficSlowMinutes: Int = 0,
    modifier: Modifier = Modifier,
) {
    val hideTripMetrics = hideEtaWhenGoogleNavigation && (drivingMode || waitingAtWarehouse)
    val summaryLegs = track?.deliverySummary.orEmpty()
    var geotagPreviewUrl by remember { mutableStateOf<String?>(null) }

    Column(
        modifier
            .fillMaxWidth()
            .navigationBarsPadding()
            .padding(horizontal = 16.dp, vertical = 4.dp),
    ) {
            track?.let { data ->
                BookingIdCard(
                    bookingId = data.entryReference,
                    driverName = data.assignedDriverName,
                    plate = data.assignedVehiclePlate,
                )
                Spacer(Modifier.height(12.dp))
                val tripLegs = summaryLegs.filter { it.leg == "pickup" || it.leg == "warehouse" }
                val daNmisLeg = summaryLegs.firstOrNull { it.leg == "da_nmis" || it.leg == "nmis" }
                TripSummaryCard(
                    pickupName = data.pickup.name,
                    pickupLat = data.pickup.latitude,
                    pickupLng = data.pickup.longitude,
                    warehouseName = data.destination.name,
                    warehouseLat = data.destination.latitude,
                    warehouseLng = data.destination.longitude,
                    summaryLegs = tripLegs,
                    apiBaseUrl = apiBaseUrl,
                    onViewGeotag = { geotagPreviewUrl = it },
                )
                daNmisLeg?.let { leg ->
                    Spacer(Modifier.height(12.dp))
                    DaNmisHandoffCard(leg = leg, containerStatus = data.status)
                }
                Spacer(Modifier.height(12.dp))
            }

            if (routeLoading && route == null && !hideTripMetrics) {
                LinearProgressIndicator(modifier = Modifier.fillMaxWidth(), color = AgriColors.Primary)
            }
            if (route != null && !hideTripMetrics) {
                val etaSeconds = when {
                    drivingMode && remainingDurationSeconds != null -> remainingDurationSeconds
                    else -> route.etaDurationSeconds()
                }
                val etaDistance = when {
                    drivingMode && remainingDistanceMeters != null -> remainingDistanceMeters
                    else -> route.distanceMeters
                }
                val trafficDelayMin = route.durationInTrafficSeconds?.let { traffic ->
                    val normal = route.durationSeconds.coerceAtLeast(1.0)
                    ((traffic - normal) / 60.0).toInt().coerceAtLeast(0)
                } ?: 0
                val etaColor = when {
                    route.isOsrmFallback -> Color(0xFFF9AB00)
                    trafficDelayMin >= 3 || (drivingMode && trafficJamMinutes > 0) -> Color(0xFFEA4335)
                    trafficDelayMin >= 1 || trafficSlowMinutes > 0 -> Color(0xFFF9AB00)
                    else -> RouteGreen
                }
                TripEtaRow(
                    durationLabel = OsrmClient.formatDurationMinutes(etaSeconds),
                    metaLabel = "${OsrmClient.formatDistance(etaDistance)} · ${OsrmClient.formatArrivalTime(etaSeconds)}",
                    accentColor = etaColor,
                )
                Spacer(Modifier.height(8.dp))
            }

            if (!drivingMode) {
                if (tripCompleted && tripEndedMessage != null) {
                    Text(
                        tripEndedMessage,
                        style = MaterialTheme.typography.bodyMedium,
                        color = RouteGreen,
                        fontWeight = FontWeight.SemiBold,
                        modifier = Modifier.padding(top = 8.dp),
                    )
                } else {
                    if (waitingAtWarehouse) {
                        Text(
                            UiStrings.Map.waitingForQrScan,
                            style = MaterialTheme.typography.bodySmall,
                            color = AgriColors.Primary,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(top = 6.dp),
                        )
                    }
                    startNavigationHint?.let {
                        Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                    }
                    if (!waitingAtWarehouse) {
                        Spacer(Modifier.height(8.dp))
                        Button(
                            onClick = onStartNavigation,
                            enabled = startNavigationEnabled && !startNavigationBusy,
                            modifier = Modifier.fillMaxWidth().height(52.dp),
                            shape = RoundedCornerShape(14.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
                        ) {
                            if (startNavigationBusy) {
                                CircularProgressIndicator(
                                    color = Color.White,
                                    modifier = Modifier.size(22.dp),
                                    strokeWidth = 2.dp,
                                )
                            } else {
                                Icon(Icons.Default.LocalShipping, contentDescription = null, tint = Color.White)
                                Spacer(Modifier.width(8.dp))
                                Text(UiStrings.Map.startButton, fontWeight = FontWeight.Bold)
                            }
                        }
                        if (showInAppGoogleHint) {
                            Text(
                                UiStrings.Map.inAppGoogleNavigation,
                                style = MaterialTheme.typography.bodySmall,
                                color = AgriColors.TextSecondary,
                                modifier = Modifier.padding(top = 6.dp),
                            )
                        }
                    }
                }
            } else {
                if (showPickupButton) {
                    pickupDistanceHint?.let {
                        Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                    }
                    Spacer(Modifier.height(6.dp))
                    Button(
                        onClick = onConfirmPickup,
                        enabled = pickupEnabled && !pickupBusy,
                        modifier = Modifier.fillMaxWidth().height(46.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = AgriColors.PrimaryDark),
                    ) {
                        if (pickupBusy) {
                            CircularProgressIndicator(color = Color.White, modifier = Modifier.size(22.dp))
                        } else {
                            Text(UiStrings.Map.pickupContainer, fontWeight = FontWeight.Bold)
                        }
                    }
                }
                if (showArrivedButton) {
                    Spacer(Modifier.height(6.dp))
                    Button(
                        onClick = onArrivedAtWarehouse,
                        enabled = arrivedEnabled && !arriveBusy,
                        modifier = Modifier.fillMaxWidth().height(46.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
                    ) {
                        if (arriveBusy) {
                            CircularProgressIndicator(color = Color.White, modifier = Modifier.size(22.dp))
                        } else {
                            Icon(Icons.Default.Place, contentDescription = null, tint = Color.White)
                            Spacer(Modifier.width(8.dp))
                            Text(UiStrings.Map.arrivedAtWarehouse, fontWeight = FontWeight.Bold)
                        }
                    }
                }
                if (showEmptyContainerButton) {
                    Spacer(Modifier.height(6.dp))
                    Button(
                        onClick = onEmptyContainerProof,
                        enabled = emptyContainerEnabled && !emptyContainerBusy,
                        modifier = Modifier.fillMaxWidth().height(46.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = AgriColors.PrimaryDark),
                    ) {
                        if (emptyContainerBusy) {
                            CircularProgressIndicator(color = Color.White, modifier = Modifier.size(22.dp))
                        } else {
                            Text(UiStrings.Map.emptyContainerProof, fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }

            if (waitingAtWarehouse && showQr) {
                Spacer(Modifier.height(10.dp))
                MapInlineHandoffQr(
                    handoffQr = handoffQr,
                    waitingMessage = handoffQrWaitingMessage,
                )
            }

            if (showQr && !waitingAtWarehouse && !tripCompleted) {
                Spacer(Modifier.height(6.dp))
                OutlinedButton(onClick = onOpenQr, modifier = Modifier.fillMaxWidth()) {
                    Text(UiStrings.Map.deliveryQr)
                }
            }
            Spacer(Modifier.height(12.dp))
    }

    geotagPreviewUrl?.let { url ->
        GeotagImageDialog(imageUrl = url, onDismiss = { geotagPreviewUrl = null })
    }
}

@Composable
private fun BookingIdCard(
    bookingId: String,
    driverName: String?,
    plate: String?,
) {
    val clipboard = LocalClipboardManager.current
    val context = LocalContext.current
    ElevatedCard(
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.elevatedCardColors(containerColor = Color.White),
        elevation = CardDefaults.elevatedCardElevation(defaultElevation = 1.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(Modifier.padding(horizontal = 14.dp, vertical = 12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    Icons.Default.Description,
                    contentDescription = null,
                    tint = AgriColors.Primary,
                    modifier = Modifier.size(22.dp),
                )
                Spacer(Modifier.width(10.dp))
                Column(Modifier.weight(1f)) {
                    Text(
                        UiStrings.Map.bookingId,
                        style = MaterialTheme.typography.labelSmall,
                        color = AgriColors.TextSecondary,
                    )
                    Text(
                        bookingId,
                        fontWeight = FontWeight.Bold,
                        color = NavyText,
                        fontSize = 15.sp,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                    )
                }
                IconButton(
                    onClick = {
                        clipboard.setText(AnnotatedString(bookingId))
                        android.widget.Toast.makeText(context, UiStrings.Map.bookingIdCopied, android.widget.Toast.LENGTH_SHORT).show()
                    },
                ) {
                    Icon(Icons.Default.ContentCopy, contentDescription = UiStrings.Map.bookingId, tint = MutedCoord)
                }
            }
            val meta = buildList {
                if (!driverName.isNullOrBlank()) add(driverName.trim())
                if (!plate.isNullOrBlank()) add("${UiStrings.Map.plateNumber}: ${plate.trim()}")
            }.joinToString("  ·  ")
            if (meta.isNotBlank()) {
                Text(
                    meta,
                    style = MaterialTheme.typography.bodySmall,
                    color = AgriColors.TextSecondary,
                    modifier = Modifier.padding(top = 6.dp),
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
            }
        }
    }
}

@Composable
private fun TripEtaRow(
    durationLabel: String,
    metaLabel: String,
    accentColor: Color,
) {
    Row(
        Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween,
    ) {
        Row(verticalAlignment = Alignment.Bottom) {
            Text(
                durationLabel,
                color = accentColor,
                fontWeight = FontWeight.Bold,
                fontSize = 26.sp,
            )
            Spacer(Modifier.width(8.dp))
            Text(metaLabel, color = MutedCoord, fontSize = 13.sp, modifier = Modifier.padding(bottom = 3.dp))
        }
    }
}

private fun abbrevStopLabel(name: String): String {
    val trimmed = name.trim()
    if (trimmed.isEmpty()) return "—"
    val parts = trimmed.split(Regex("\\s+")).filter { it.isNotBlank() }
    if (parts.size == 1) return parts[0].take(8).uppercase(Locale.ENGLISH)
    if (parts.size == 2 && parts[0].length <= 6) {
        return parts[0].uppercase(Locale.ENGLISH)
    }
    return parts.take(3).map { it.first().uppercaseChar() }.joinToString("")
}

@Composable
private fun TripSummaryCard(
    pickupName: String,
    pickupLat: Double,
    pickupLng: Double,
    warehouseName: String,
    warehouseLat: Double,
    warehouseLng: Double,
    summaryLegs: List<ContainerDeliveryLegSummaryDto>,
    apiBaseUrl: String,
    onViewGeotag: (String) -> Unit,
) {
    val pickupCoords = formatCoordinates(pickupLat, pickupLng)
    val warehouseCoords = formatCoordinates(warehouseLat, warehouseLng)
    val destinationRed = Color(0xFFEF4444)
    val startBadgeBg = Color(0xFFEFF6FF)
    val destBadgeBg = Color(0xFFFEE2E2)
    val destBadgeFg = Color(0xFFDC2626)

    ElevatedCard(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.elevatedCardColors(containerColor = Color.White),
        elevation = CardDefaults.elevatedCardElevation(defaultElevation = 1.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(Modifier.padding(horizontal = 18.dp, vertical = 16.dp)) {
            Row(Modifier.fillMaxWidth()) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier
                        .width(28.dp)
                        .padding(top = 4.dp),
                ) {
                    RouteStartMarker()
                    DashedVerticalLine(
                        modifier = Modifier
                            .width(2.dp)
                            .height(40.dp)
                            .padding(vertical = 4.dp),
                        color = Color(0xFFD1D5DB),
                    )
                    Icon(
                        Icons.Default.Place,
                        contentDescription = null,
                        tint = destinationRed,
                        modifier = Modifier.size(22.dp),
                    )
                }
                Column(
                    Modifier
                        .weight(1f)
                        .padding(start = 6.dp),
                ) {
                    TripRouteStopContent(
                        title = abbrevStopLabel(pickupName),
                        coordinates = pickupCoords,
                        badgeLabel = UiStrings.Map.routeBadgeStart,
                        badgeBg = startBadgeBg,
                        badgeFg = BlueAccent,
                    )
                    Spacer(Modifier.height(22.dp))
                    TripRouteStopContent(
                        title = abbrevStopLabel(warehouseName),
                        coordinates = warehouseCoords,
                        badgeLabel = UiStrings.Map.routeBadgeDestination,
                        badgeBg = destBadgeBg,
                        badgeFg = destBadgeFg,
                    )
                }
            }

            if (summaryLegs.isNotEmpty()) {
                HorizontalDivider(
                    modifier = Modifier.padding(top = 16.dp, bottom = 12.dp),
                    color = CardBorder,
                )
                Text(
                    UiStrings.Map.tripSummaryTitle,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = MutedCoord,
                    letterSpacing = 0.3.sp,
                )
                Spacer(Modifier.height(8.dp))
                summaryLegs.forEach { leg ->
                    TripProgressLegRow(
                        leg = leg,
                        apiBaseUrl = apiBaseUrl,
                        onViewGeotag = onViewGeotag,
                    )
                }
            }
        }
    }
}

@Composable
private fun RouteStartMarker() {
    Box(
        Modifier.size(14.dp),
        contentAlignment = Alignment.Center,
    ) {
        Box(
            Modifier
                .fillMaxSize()
                .clip(CircleShape)
                .background(BlueAccent),
        )
        Box(
            Modifier
                .size(5.dp)
                .clip(CircleShape)
                .background(Color.White),
        )
    }
}

@Composable
private fun DashedVerticalLine(modifier: Modifier, color: Color) {
    Canvas(modifier) {
        val stroke = 2.5f
        val pathEffect = PathEffect.dashPathEffect(floatArrayOf(6f, 6f), 0f)
        drawLine(
            color = color,
            start = Offset(size.width / 2f, 0f),
            end = Offset(size.width / 2f, size.height),
            strokeWidth = stroke,
            pathEffect = pathEffect,
        )
    }
}

@Composable
private fun TripRouteStopContent(
    title: String,
    coordinates: String?,
    badgeLabel: String,
    badgeBg: Color,
    badgeFg: Color,
) {
    Row(
        Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f).padding(end = 8.dp)) {
            Text(
                title,
                fontWeight = FontWeight.Bold,
                color = NavyText,
                fontSize = 15.sp,
                letterSpacing = 0.2.sp,
            )
            coordinates?.let {
                Text(
                    it,
                    fontSize = 12.sp,
                    color = MutedCoord,
                    modifier = Modifier.padding(top = 2.dp),
                )
            }
        }
        RouteBadge(badgeLabel, badgeBg, badgeFg)
    }
}

@Composable
private fun RouteBadge(text: String, bg: Color, fg: Color) {
    Surface(shape = RoundedCornerShape(10.dp), color = bg) {
        Text(
            text,
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = fg,
        )
    }
}

@Composable
private fun DaNmisHandoffCard(
    leg: ContainerDeliveryLegSummaryDto,
    containerStatus: String,
) {
    val scanTime = formatLegTime(leg.arrivedAtUtc)
    val inspectionTime = formatLegTime(leg.departedAtUtc)
    val tripComplete = containerStatus.equals("Inspected", ignoreCase = true) ||
        containerStatus.equals("Released", ignoreCase = true)
    val completeGreen = Color(0xFF15803D)

    ElevatedCard(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.elevatedCardColors(containerColor = Color.White),
        elevation = CardDefaults.elevatedCardElevation(defaultElevation = 1.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(Modifier.padding(horizontal = 18.dp, vertical = 16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    Icons.Default.Verified,
                    contentDescription = null,
                    tint = BlueAccent,
                    modifier = Modifier.size(20.dp),
                )
                Spacer(Modifier.width(8.dp))
                Text(
                    UiStrings.Map.daNmisTitle,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = BlueAccent,
                    letterSpacing = 0.4.sp,
                )
            }
            leg.statusLabel?.let { status ->
                Spacer(Modifier.height(8.dp))
                Text(
                    status,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = NavyText,
                    maxLines = 3,
                )
            }
            HorizontalDivider(
                modifier = Modifier.padding(vertical = 12.dp),
                color = CardBorder,
            )
            Text(
                scanTime?.let { "${UiStrings.Map.daNmisScanConfirmed} · $it" }
                    ?: UiStrings.Map.daNmisAwaitingScan,
                fontSize = 12.sp,
                color = MutedCoord,
                maxLines = 2,
            )
            Spacer(Modifier.height(6.dp))
            Text(
                inspectionTime?.let { "${UiStrings.Map.daNmisInspectionCompleted} · $it" }
                    ?: UiStrings.Map.daNmisInspectionPending,
                fontSize = 12.sp,
                color = MutedCoord,
                maxLines = 2,
            )
            if (tripComplete) {
                Spacer(Modifier.height(8.dp))
                Text(
                    "${UiStrings.Map.daNmisContainerConfirmed} ($containerStatus)",
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = completeGreen,
                )
            }
        }
    }
}

@Composable
private fun TripProgressLegRow(
    leg: ContainerDeliveryLegSummaryDto,
    apiBaseUrl: String,
    onViewGeotag: (String) -> Unit,
) {
    val photoUrl = ApiAssetUrl.resolve(apiBaseUrl, leg.photoRelativeUrl)
    val arrived = formatLegTime(leg.arrivedAtUtc)
    val departed = formatLegTime(leg.departedAtUtc)
    val status = leg.statusLabel

    Row(
        Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(
                leg.title,
                fontSize = 13.sp,
                fontWeight = FontWeight.Medium,
                color = NavyText,
            )
            when {
                status != null -> Text(status, fontSize = 11.sp, color = MutedCoord, maxLines = 2)
                arrived != null -> Text(arrived, fontSize = 11.sp, color = MutedCoord)
                else -> Text(UiStrings.Map.summaryNoTimestamp, fontSize = 11.sp, color = MutedCoord)
            }
            if (departed != null && leg.leg == "pickup") {
                Text(
                    "${UiStrings.Map.summaryDeparted}: $departed",
                    fontSize = 11.sp,
                    color = MutedCoord,
                )
            }
        }
        if (!photoUrl.isNullOrBlank()) {
            TextButton(
                onClick = { onViewGeotag(photoUrl) },
                contentPadding = PaddingValues(horizontal = 4.dp, vertical = 0.dp),
            ) {
                Icon(Icons.Default.Photo, contentDescription = null, modifier = Modifier.size(16.dp), tint = BlueAccent)
                Spacer(Modifier.width(4.dp))
                Text(UiStrings.Map.viewImageAction, fontSize = 11.sp, color = BlueAccent)
            }
        }
    }
}

@Composable
private fun MapInlineHandoffQr(
    handoffQr: DriverDeliveryHandoffQrDto?,
    waitingMessage: String?,
) {
    val qrBitmap: ImageBitmap? = rememberHandoffQrImageBitmap(
        handoffQr?.qrPayload,
        handoffQr?.qrCodeData,
    )
    Surface(
        shape = RoundedCornerShape(12.dp),
        color = Color(0xFFF8FAFC),
        border = androidx.compose.foundation.BorderStroke(1.dp, AgriColors.Primary.copy(alpha = 0.2f)),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(
            Modifier.padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            when {
                qrBitmap != null -> {
                    Image(
                        bitmap = qrBitmap,
                        contentDescription = "Delivery QR code",
                        modifier = Modifier.size(160.dp),
                        contentScale = ContentScale.Fit,
                    )
                    Text(
                        handoffQr?.handoffInstruction ?: UiStrings.DeliveryQr.handoffDefault,
                        style = MaterialTheme.typography.bodySmall,
                        color = AgriColors.OnSurface,
                        modifier = Modifier.padding(top = 6.dp),
                    )
                }
                waitingMessage != null -> {
                    CircularProgressIndicator(color = AgriColors.PrimaryDark, modifier = Modifier.size(32.dp))
                    Spacer(Modifier.height(8.dp))
                    Text(waitingMessage, style = MaterialTheme.typography.bodySmall, color = AgriColors.OnSurface)
                }
                else -> {
                    CircularProgressIndicator(color = AgriColors.PrimaryDark, modifier = Modifier.size(32.dp))
                    Spacer(Modifier.height(8.dp))
                    Text(UiStrings.Map.summaryGeneratingQr, color = AgriColors.TextSecondary, fontSize = 12.sp)
                }
            }
        }
    }
}

@Composable
private fun GeotagImageDialog(imageUrl: String, onDismiss: () -> Unit) {
    Dialog(onDismissRequest = onDismiss) {
        Surface(shape = RoundedCornerShape(16.dp), color = Color.White) {
            Column(Modifier.padding(12.dp)) {
                Text(
                    UiStrings.Map.geotagImageTitle,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                Spacer(Modifier.height(8.dp))
                AsyncImage(
                    model = imageUrl,
                    contentDescription = UiStrings.Map.geotagImageTitle,
                    modifier = Modifier
                        .fillMaxWidth()
                        .heightIn(max = 420.dp),
                    contentScale = ContentScale.Fit,
                )
                Spacer(Modifier.height(8.dp))
                TextButton(onClick = onDismiss, modifier = Modifier.align(Alignment.End)) {
                    Text(UiStrings.Map.back)
                }
            }
        }
    }
}
