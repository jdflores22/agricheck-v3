package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Place
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.agricheck.agritrack.data.model.ContainerTrackDestinationDto
import com.agricheck.agritrack.data.api.OsrmClient
import com.agricheck.agritrack.data.api.OsrmRouteResult
import com.agricheck.agritrack.data.model.ContainerTrackDto
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import com.agricheck.agritrack.util.TripNavLeg

private val RouteGreen = Color(0xFF15803D)

@Composable
fun MapTripPanel(
    drivingMode: Boolean,
    route: OsrmRouteResult?,
    routeLoading: Boolean,
    track: ContainerTrackDto?,
    navLeg: TripNavLeg?,
    towardName: String,
    startButtonSub: String,
    showRouteLegend: Boolean,
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
    actionMessage: String?,
    onBack: () -> Unit,
    onStartNavigation: () -> Unit,
    onConfirmPickup: () -> Unit,
    onArrivedAtWarehouse: () -> Unit,
    onOpenQr: () -> Unit,
) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        shadowElevation = 8.dp,
        color = Color.White,
        tonalElevation = 2.dp,
    ) {
        Column(
            Modifier
                .padding(horizontal = 16.dp, vertical = 12.dp)
                .verticalScroll(rememberScrollState()),
        ) {
            if (routeLoading && route == null) {
                LinearProgressIndicator(modifier = Modifier.fillMaxWidth(), color = AgriColors.Primary)
            }
            if (route != null) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        OsrmClient.formatDurationMinutes(route.durationSeconds),
                        color = RouteGreen,
                        fontWeight = FontWeight.Bold,
                        fontSize = 28.sp,
                    )
                    Spacer(Modifier.width(10.dp))
                    Text(
                        "${OsrmClient.formatDistance(route.distanceMeters)} · ${OsrmClient.formatArrivalTime(route.durationSeconds)}",
                        color = Color.DarkGray,
                        fontSize = 14.sp,
                    )
                }
            } else if (!routeLoading) {
                Text(UiStrings.Map.calculating, color = AgriColors.TextSecondary)
            }

            if (!drivingMode) {
                track?.let { data ->
                    Spacer(Modifier.height(8.dp))
                    MapStopDetailCard(
                        label = UiStrings.Map.pickup,
                        stop = data.pickup,
                        accent = RouteGreen,
                        emphasized = navLeg == TripNavLeg.ToPickup,
                    )
                    Spacer(Modifier.height(8.dp))
                    MapStopDetailCard(
                        label = UiStrings.Map.destination,
                        stop = data.destination,
                        accent = AgriColors.TextSecondary,
                        emphasized = navLeg == TripNavLeg.ToDestination,
                    )
                }
                if (waitingAtWarehouse) {
                    Spacer(Modifier.height(8.dp))
                    Text(
                        UiStrings.Map.waitingForQrScan,
                        style = MaterialTheme.typography.bodySmall,
                        color = AgriColors.Primary,
                    )
                }
                startNavigationHint?.let {
                    Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                    Spacer(Modifier.height(6.dp))
                }
                Spacer(Modifier.height(10.dp))
                Button(
                    onClick = onStartNavigation,
                    enabled = startNavigationEnabled && !startNavigationBusy && !waitingAtWarehouse,
                    modifier = Modifier.fillMaxWidth().height(48.dp),
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
            } else {
                Text(
                    "${UiStrings.Map.toward} $towardName",
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.padding(top = 6.dp),
                )
                if (showPickupButton) {
                    pickupDistanceHint?.let {
                        Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                    }
                    Spacer(Modifier.height(8.dp))
                    Button(
                        onClick = onConfirmPickup,
                        enabled = pickupEnabled && !pickupBusy,
                        modifier = Modifier.fillMaxWidth().height(48.dp),
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
                    arrivedDistanceHint?.let {
                        Text(it, style = MaterialTheme.typography.bodySmall, color = AgriColors.TextSecondary)
                    }
                    Spacer(Modifier.height(8.dp))
                    Button(
                        onClick = onArrivedAtWarehouse,
                        enabled = arrivedEnabled && !arriveBusy,
                        modifier = Modifier.fillMaxWidth().height(48.dp),
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
            actionMessage?.let {
                Text(it, color = AgriColors.Success, style = MaterialTheme.typography.bodySmall, modifier = Modifier.padding(top = 6.dp))
            }
            if (showQr) {
                Spacer(Modifier.height(8.dp))
                OutlinedButton(onClick = onOpenQr, modifier = Modifier.fillMaxWidth()) {
                    Text(UiStrings.Map.deliveryQr)
                }
            }
        }
    }
}

@Composable
private fun MapStopDetailCard(
    label: String,
    stop: ContainerTrackDestinationDto,
    accent: Color,
    emphasized: Boolean,
) {
    val borderColor = if (emphasized) accent.copy(alpha = 0.45f) else AgriColors.TextSecondary.copy(alpha = 0.2f)
    Surface(
        shape = RoundedCornerShape(14.dp),
        color = if (emphasized) accent.copy(alpha = 0.06f) else Color.White,
        border = androidx.compose.foundation.BorderStroke(1.dp, borderColor),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(Modifier.padding(12.dp), verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = if (label == UiStrings.Map.pickup) Icons.Default.Place else Icons.Default.LocalShipping,
                contentDescription = null,
                tint = accent,
                modifier = Modifier.size(22.dp),
            )
            Spacer(Modifier.width(10.dp))
            Column(Modifier.weight(1f)) {
                Text(label, style = MaterialTheme.typography.labelMedium, color = accent, fontWeight = FontWeight.Bold)
                Text(stop.name, fontWeight = FontWeight.SemiBold, color = AgriColors.OnSurface, maxLines = 2)
            }
        }
    }
}
