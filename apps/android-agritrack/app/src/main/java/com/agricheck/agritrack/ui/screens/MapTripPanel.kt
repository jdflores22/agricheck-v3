package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
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
import com.agricheck.agritrack.util.TripNavLeg
import com.agricheck.agritrack.util.formatApiInstantForDisplay
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

private val RouteGreen = Color(0xFF15803D)
private val MutedCoord = Color(0xFF9CA3AF)
private val PhilippinesZone = ZoneId.of("Asia/Manila")
private val LegTimeFormat = DateTimeFormatter.ofPattern("MMM d, yyyy · h:mm a", Locale.ENGLISH)

private fun formatLegTime(iso: String?): String? =
    formatApiInstantForDisplay(iso, PhilippinesZone, LegTimeFormat)

private fun formatCoordinates(lat: Double?, lng: Double?): String? {
    if (lat == null || lng == null) return null
    return String.format(Locale.US, "%.5f, %.5f", lat, lng)
}

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
                TripAssignmentHeader(
                    driverName = data.assignedDriverName,
                    plate = data.assignedVehiclePlate,
                    bookingId = data.entryReference,
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
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        OsrmClient.formatDurationMinutes(etaSeconds),
                        color = etaColor,
                        fontWeight = FontWeight.Bold,
                        fontSize = 24.sp,
                    )
                    Spacer(Modifier.width(10.dp))
                    Text(
                        "${OsrmClient.formatDistance(etaDistance)} · ${OsrmClient.formatArrivalTime(etaSeconds)}",
                        color = Color.DarkGray,
                        fontSize = 13.sp,
                    )
                }
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
                        modifier = Modifier.fillMaxWidth().height(46.dp),
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
                Spacer(Modifier.height(10.dp))
                Text(
                    UiStrings.Map.tripSummaryTitle,
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold,
                    color = AgriColors.OnSurface,
                )
                Spacer(Modifier.height(6.dp))
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
                    Spacer(Modifier.height(6.dp))
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
private fun TripAssignmentHeader(
    driverName: String?,
    plate: String?,
    bookingId: String,
) {
    if (bookingId.isNotBlank()) {
        Text(
            "${UiStrings.Map.bookingId}: $bookingId",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
            color = AgriColors.OnSurface,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
        )
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
            maxLines = 2,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier.padding(top = 2.dp),
        )
    }
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
    Surface(
        shape = RoundedCornerShape(14.dp),
        color = Color.White,
        border = androidx.compose.foundation.BorderStroke(1.dp, Color(0xFFE8EAED)),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(Modifier.padding(14.dp)) {
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.padding(top = 2.dp),
            ) {
                Box(
                    Modifier
                        .size(12.dp)
                        .clip(CircleShape)
                        .background(Color(0xFF4285F4)),
                )
                Box(
                    Modifier
                        .width(2.dp)
                        .height(42.dp)
                        .padding(vertical = 4.dp)
                        .background(Color(0xFFDADCE0)),
                )
                Icon(
                    Icons.Default.Place,
                    contentDescription = null,
                    tint = Color(0xFFEA4335),
                    modifier = Modifier.size(22.dp),
                )
            }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Text(
                    pickupName,
                    fontWeight = FontWeight.SemiBold,
                    color = AgriColors.OnSurface,
                    fontSize = 15.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
                pickupSub?.let {
                    Text(
                        it,
                        style = MaterialTheme.typography.bodySmall,
                        color = MutedCoord,
                        fontSize = 12.sp,
                        modifier = Modifier.padding(top = 2.dp, bottom = 14.dp),
                    )
                } ?: Spacer(Modifier.height(14.dp))
                Text(
                    warehouseName,
                    fontWeight = FontWeight.SemiBold,
                    color = AgriColors.OnSurface,
                    fontSize = 15.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
                warehouseSub?.let {
                    Text(
                        it,
                        style = MaterialTheme.typography.bodySmall,
                        color = MutedCoord,
                        fontSize = 12.sp,
                        modifier = Modifier.padding(top = 2.dp),
                    )
                }
            }
        }
    }
}

@Composable
private fun MapInlineHandoffQr(
    handoffQr: DriverDeliveryHandoffQrDto?,
    waitingMessage: String?,
) {
    val context = LocalContext.current
    val imageRequest = remember(handoffQr?.qrPayload, handoffQr?.qrCodeData) {
        val raw = handoffQr?.qrCodeData ?: return@remember null
        val uri = if (raw.startsWith("data:")) raw else "data:image/png;base64,$raw"
        val cacheKey = handoffQr.qrPayload ?: handoffQr.expiresAtUtc
        ImageRequest.Builder(context)
            .data(uri)
            .memoryCacheKey(cacheKey)
            .diskCacheKey(cacheKey)
            .build()
    }
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
                imageRequest != null -> {
                    AsyncImage(
                        model = imageRequest,
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
    val accent = when (leg.leg) {
        "pickup" -> RouteGreen
        "warehouse" -> AgriColors.PrimaryDark
        else -> AgriColors.TextSecondary
    }
    val coords = formatCoordinates(
        leg.captureLatitude ?: fallbackLatitude,
        leg.captureLongitude ?: fallbackLongitude,
    )
    Surface(
        shape = RoundedCornerShape(12.dp),
        color = Color(0xFFF9FAFB),
        border = androidx.compose.foundation.BorderStroke(1.dp, Color(0xFFE5E7EB)),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(
            Modifier.padding(12.dp),
            verticalAlignment = Alignment.Top,
        ) {
            Column(Modifier.weight(1f)) {
                Text(leg.title, fontWeight = FontWeight.SemiBold, color = AgriColors.OnSurface, fontSize = 14.sp)
                leg.statusLabel?.let {
                    Text(it, style = MaterialTheme.typography.labelSmall, color = accent, modifier = Modifier.padding(top = 2.dp))
                }
                formatLegTime(leg.arrivedAtUtc)?.let { arrived ->
                    Text(
                        "${UiStrings.Map.summaryArrived}",
                        style = MaterialTheme.typography.labelSmall,
                        fontWeight = FontWeight.SemiBold,
                        color = AgriColors.TextSecondary,
                        modifier = Modifier.padding(top = 6.dp),
                    )
                    Text(
                        arrived,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Medium,
                        color = AgriColors.OnSurface,
                    )
                }
                formatLegTime(leg.departedAtUtc)?.let { departed ->
                    Text(
                        "${UiStrings.Map.summaryDeparted}",
                        style = MaterialTheme.typography.labelSmall,
                        fontWeight = FontWeight.SemiBold,
                        color = AgriColors.TextSecondary,
                        modifier = Modifier.padding(top = 6.dp),
                    )
                    Text(
                        departed,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Medium,
                        color = AgriColors.OnSurface,
                    )
                }
                if (formatLegTime(leg.arrivedAtUtc) == null && formatLegTime(leg.departedAtUtc) == null) {
                    Text(
                        UiStrings.Map.summaryNoTimestamp,
                        style = MaterialTheme.typography.bodySmall,
                        color = AgriColors.TextSecondary,
                        modifier = Modifier.padding(top = 4.dp),
                    )
                }
                coords?.let {
                    Text(
                        it,
                        style = MaterialTheme.typography.labelSmall,
                        color = MutedCoord.copy(alpha = 0.75f),
                        fontSize = 10.sp,
                        modifier = Modifier.padding(top = 4.dp),
                    )
                }
            }
            if (!photoUrl.isNullOrBlank()) {
                OutlinedButton(
                    onClick = { onViewImage(photoUrl) },
                    modifier = Modifier.padding(start = 8.dp),
                    contentPadding = PaddingValues(horizontal = 10.dp, vertical = 8.dp),
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Icon(Icons.Default.Photo, contentDescription = null, modifier = Modifier.size(20.dp), tint = accent)
                        Spacer(Modifier.height(2.dp))
                        Text(UiStrings.Map.summaryViewImage, fontSize = 11.sp, color = accent)
                    }
                }
            }
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
