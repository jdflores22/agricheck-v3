package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Photo
import androidx.compose.material.icons.filled.Place
import androidx.compose.material.icons.filled.Route
import androidx.compose.material.icons.filled.Verified
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.foundation.background
import androidx.compose.ui.draw.clip
import androidx.compose.foundation.Image
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
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
import coil.request.ImageRequest
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
private val SheetMutedBg = Color(0xFFF1F5F9)
private val CardBorder = Color(0xFFE2E8F0)
private val NavyText = Color(0xFF0F172A)
private val BlueAccent = Color(0xFF2563EB)
private val BlueChipBg = Color(0xFFEFF6FF)
private val GreenChipBg = Color(0xFFECFDF5)
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
    onOpenQr: () -> Unit,
    trafficJamMinutes: Int = 0,
    trafficSlowMinutes: Int = 0,
    modifier: Modifier = Modifier,
) {
    val hideTripMetrics = hideEtaWhenGoogleNavigation && (drivingMode || waitingAtWarehouse)
    val summaryLegs = track?.deliverySummary.orEmpty()
    val showLegDetails = summaryLegs.isNotEmpty()
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
                Spacer(Modifier.height(10.dp))
                TripRouteTimelineCard(
                    pickupName = data.pickup.name,
                    pickupLat = data.pickup.latitude,
                    pickupLng = data.pickup.longitude,
                    warehouseName = data.destination.name,
                    warehouseLat = data.destination.latitude,
                    warehouseLng = data.destination.longitude,
                )
                Spacer(Modifier.height(10.dp))
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
            }

            if (waitingAtWarehouse && showQr) {
                Spacer(Modifier.height(10.dp))
                MapInlineHandoffQr(
                    handoffQr = handoffQr,
                    waitingMessage = handoffQrWaitingMessage,
                )
            }

            if (showLegDetails && summaryLegs.isNotEmpty()) {
                Spacer(Modifier.height(14.dp))
                Column(
                    Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(16.dp))
                        .background(SheetMutedBg)
                        .padding(12.dp),
                ) {
                TripSummarySectionHeader()
                Spacer(Modifier.height(10.dp))
                val trip = track
                summaryLegs.forEach { leg ->
                    if (leg.leg == "nmis") {
                        NmisLegRow(leg)
                    } else {
                        val fallbackLat = when (leg.leg) {
                            "pickup" -> trip?.pickup?.latitude
                            "warehouse" -> trip?.destination?.latitude
                            else -> null
                        }
                        val fallbackLng = when (leg.leg) {
                            "pickup" -> trip?.pickup?.longitude
                            "warehouse" -> trip?.destination?.longitude
                            else -> null
                        }
                        DeliveryLegSummaryCard(
                            leg = leg,
                            apiBaseUrl = apiBaseUrl,
                            fallbackLatitude = fallbackLat,
                            fallbackLongitude = fallbackLng,
                            onViewImage = { url -> geotagPreviewUrl = url },
                        )
                    }
                    Spacer(Modifier.height(8.dp))
                }
                }
            }

            if (showQr && !waitingAtWarehouse) {
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
private fun TripSummarySectionHeader() {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Surface(
            shape = RoundedCornerShape(10.dp),
            color = BlueChipBg,
            modifier = Modifier.size(40.dp),
        ) {
            Icon(
                Icons.Default.Route,
                contentDescription = null,
                tint = BlueAccent,
                modifier = Modifier.padding(9.dp),
            )
        }
        Spacer(Modifier.width(10.dp))
        Column {
            Text(
                UiStrings.Map.tripSummaryTitle,
                fontWeight = FontWeight.Bold,
                color = NavyText,
                fontSize = 16.sp,
            )
            Text(
                UiStrings.Map.tripSummarySubtitle,
                style = MaterialTheme.typography.bodySmall,
                color = AgriColors.TextSecondary,
            )
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
    val parts = name.trim().split(Regex("\\s+")).filter { it.isNotBlank() }
    if (parts.isEmpty()) return "—"
    if (parts.size == 1) return parts[0].take(6).uppercase(Locale.ENGLISH)
    return parts.take(2).map { it.first().uppercaseChar() }.joinToString("")
}

@Composable
private fun TripRouteTimelineCard(
    pickupName: String,
    pickupLat: Double,
    pickupLng: Double,
    warehouseName: String,
    warehouseLat: Double,
    warehouseLng: Double,
) {
    val pickupSub = formatCoordinates(pickupLat, pickupLng)
    val warehouseSub = formatCoordinates(warehouseLat, warehouseLng)
    ElevatedCard(
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.elevatedCardColors(containerColor = Color.White),
        elevation = CardDefaults.elevatedCardElevation(defaultElevation = 1.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(Modifier.padding(14.dp)) {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.padding(top = 4.dp),
            ) {
                Box(
                    Modifier
                        .size(11.dp)
                        .clip(CircleShape)
                        .background(BlueAccent),
                )
                Box(
                    Modifier
                        .width(2.dp)
                        .height(44.dp)
                        .background(Color(0xFFCBD5E1)),
                )
                Icon(
                    Icons.Default.Place,
                    contentDescription = null,
                    tint = Color(0xFFEF4444),
                    modifier = Modifier.size(20.dp),
                )
            }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.Top) {
                    Column(Modifier.weight(1f)) {
                        Text(
                            abbrevStopLabel(pickupName),
                            fontWeight = FontWeight.Bold,
                            color = NavyText,
                            fontSize = 14.sp,
                        )
                        Text(
                            pickupName,
                            style = MaterialTheme.typography.bodySmall,
                            color = AgriColors.TextSecondary,
                            maxLines = 2,
                            overflow = TextOverflow.Ellipsis,
                        )
                        pickupSub?.let {
                            Text(it, fontSize = 11.sp, color = MutedCoord, modifier = Modifier.padding(top = 2.dp))
                        }
                    }
                    RouteBadge(UiStrings.Map.routeBadgeStart, Color(0xFFDCFCE7), RouteGreen)
                }
                Spacer(Modifier.height(14.dp))
                Row(verticalAlignment = Alignment.Top) {
                    Column(Modifier.weight(1f)) {
                        Text(
                            abbrevStopLabel(warehouseName),
                            fontWeight = FontWeight.Bold,
                            color = NavyText,
                            fontSize = 14.sp,
                        )
                        Text(
                            warehouseName,
                            style = MaterialTheme.typography.bodySmall,
                            color = AgriColors.TextSecondary,
                            maxLines = 2,
                            overflow = TextOverflow.Ellipsis,
                        )
                        warehouseSub?.let {
                            Text(it, fontSize = 11.sp, color = MutedCoord, modifier = Modifier.padding(top = 2.dp))
                        }
                    }
                    RouteBadge(UiStrings.Map.routeBadgeDestination, Color(0xFFFEE2E2), Color(0xFFDC2626))
                }
            }
        }
    }
}

@Composable
private fun RouteBadge(text: String, bg: Color, fg: Color) {
    Surface(shape = RoundedCornerShape(8.dp), color = bg) {
        Text(
            text,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
            fontSize = 11.sp,
            fontWeight = FontWeight.SemiBold,
            color = fg,
        )
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
private fun DeliveryLegSummaryCard(
    leg: ContainerDeliveryLegSummaryDto,
    apiBaseUrl: String,
    fallbackLatitude: Double? = null,
    fallbackLongitude: Double? = null,
    onViewImage: (String) -> Unit,
) {
    val photoUrl = ApiAssetUrl.resolve(apiBaseUrl, leg.photoRelativeUrl)
    val isWarehouse = leg.leg == "warehouse"
    val accent = if (isWarehouse) RouteGreen else BlueAccent
    val chipBg = if (isWarehouse) GreenChipBg else BlueChipBg
    val coords = formatCoordinates(
        leg.captureLatitude ?: fallbackLatitude,
        leg.captureLongitude ?: fallbackLongitude,
    )
    val hasPhoto = !photoUrl.isNullOrBlank()
    ElevatedCard(
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.elevatedCardColors(containerColor = Color.White),
        elevation = CardDefaults.elevatedCardElevation(defaultElevation = 2.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(Modifier.height(IntrinsicSize.Min)) {
            if (isWarehouse) {
                Box(
                    Modifier
                        .width(4.dp)
                        .fillMaxHeight()
                        .background(RouteGreen),
                )
            }
            Row(
                Modifier
                    .weight(1f)
                    .padding(12.dp),
                verticalAlignment = Alignment.Top,
            ) {
                Surface(shape = RoundedCornerShape(12.dp), color = chipBg, modifier = Modifier.size(44.dp)) {
                    Icon(
                        imageVector = if (isWarehouse) Icons.Default.LocalShipping else Icons.Default.Inventory2,
                        contentDescription = null,
                        tint = accent,
                        modifier = Modifier.padding(10.dp),
                    )
                }
                Spacer(Modifier.width(10.dp))
                Column(Modifier.weight(1f)) {
                    Text(leg.title, fontWeight = FontWeight.Bold, color = NavyText, fontSize = 15.sp)
                    leg.statusLabel?.let { status ->
                        StatusPill(
                            text = status,
                            positive = !status.contains("not", ignoreCase = true) &&
                                !status.contains("awaiting", ignoreCase = true),
                        )
                        Spacer(Modifier.height(4.dp))
                    }
                    LegTimestampBlock(leg)
                    coords?.let {
                        Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 6.dp)) {
                            Icon(Icons.Default.Place, contentDescription = null, tint = MutedCoord, modifier = Modifier.size(14.dp))
                            Spacer(Modifier.width(4.dp))
                            Text(it, fontSize = 11.sp, color = MutedCoord)
                        }
                    }
                }
                if (hasPhoto) {
                    VerticalDivider(
                        modifier = Modifier
                            .padding(horizontal = 10.dp)
                            .height(72.dp),
                        color = CardBorder,
                    )
                    Surface(
                        onClick = { onViewImage(photoUrl!!) },
                        shape = RoundedCornerShape(12.dp),
                        color = Color.White,
                        border = androidx.compose.foundation.BorderStroke(1.dp, CardBorder),
                        modifier = Modifier.width(88.dp),
                    ) {
                        Column(
                            Modifier.padding(vertical = 12.dp, horizontal = 6.dp),
                            horizontalAlignment = Alignment.CenterHorizontally,
                        ) {
                            Icon(Icons.Default.Photo, contentDescription = null, tint = accent, modifier = Modifier.size(22.dp))
                            Spacer(Modifier.height(4.dp))
                            Text(
                                UiStrings.Map.viewImageAction,
                                fontSize = 11.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = accent,
                            )
                            Icon(
                                Icons.AutoMirrored.Filled.KeyboardArrowRight,
                                contentDescription = null,
                                tint = accent,
                                modifier = Modifier.size(18.dp),
                            )
                        }
                    }
                } else if (isWarehouse) {
                    Icon(
                        Icons.Default.ChevronRight,
                        contentDescription = null,
                        tint = MutedCoord,
                        modifier = Modifier
                            .padding(start = 4.dp, top = 12.dp)
                            .size(24.dp),
                    )
                }
            }
        }
    }
}

@Composable
private fun LegTimestampBlock(leg: ContainerDeliveryLegSummaryDto) {
    val arrived = formatLegTime(leg.arrivedAtUtc)
    val departed = formatLegTime(leg.departedAtUtc)
    if (arrived != null) {
        Text(
            arrived,
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.SemiBold,
            color = NavyText,
            modifier = Modifier.padding(top = 4.dp),
        )
    }
    if (departed != null) {
        Text(
            "${UiStrings.Map.summaryDeparted}: $departed",
            style = MaterialTheme.typography.bodySmall,
            color = AgriColors.TextSecondary,
            modifier = Modifier.padding(top = 2.dp),
        )
    }
    if (arrived == null && departed == null) {
        Text(
            UiStrings.Map.summaryNoTimestamp,
            style = MaterialTheme.typography.bodySmall,
            color = AgriColors.TextSecondary,
            modifier = Modifier.padding(top = 4.dp),
        )
    }
}

@Composable
private fun StatusPill(text: String, positive: Boolean) {
    val bg = if (positive) Color(0xFFDCFCE7) else Color(0xFFF1F5F9)
    val fg = if (positive) RouteGreen else MutedCoord
    Surface(shape = RoundedCornerShape(20.dp), color = bg) {
        Row(
            Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Box(
                Modifier
                    .size(6.dp)
                    .clip(CircleShape)
                    .background(fg),
            )
            Spacer(Modifier.width(6.dp))
            Text(text, fontSize = 11.sp, fontWeight = FontWeight.Medium, color = fg)
        }
    }
}

@Composable
private fun NmisLegRow(leg: ContainerDeliveryLegSummaryDto) {
    Surface(
        shape = RoundedCornerShape(12.dp),
        color = Color(0xFFEFF6FF),
        border = androidx.compose.foundation.BorderStroke(1.dp, Color(0xFFBFDBFE)),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(Modifier.padding(10.dp), verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Verified, contentDescription = null, tint = Color(0xFF1D4ED8), modifier = Modifier.size(20.dp))
            Spacer(Modifier.width(8.dp))
            Column {
                Text(leg.title, fontWeight = FontWeight.SemiBold, fontSize = 13.sp)
                leg.statusLabel?.let {
                    Text(it, style = MaterialTheme.typography.bodySmall, color = Color(0xFF1D4ED8))
                }
                formatLegTime(leg.arrivedAtUtc)?.let { validatedAt ->
                    Text(
                        UiStrings.Map.summaryArrived,
                        style = MaterialTheme.typography.labelSmall,
                        fontWeight = FontWeight.SemiBold,
                        color = AgriColors.TextSecondary,
                        modifier = Modifier.padding(top = 4.dp),
                    )
                    Text(
                        validatedAt,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Medium,
                        color = AgriColors.OnSurface,
                    )
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
