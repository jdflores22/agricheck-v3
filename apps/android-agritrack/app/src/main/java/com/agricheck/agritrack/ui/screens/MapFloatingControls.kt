package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Explore
import androidx.compose.material.icons.filled.MyLocation
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors

@Composable
fun MapFloatingControls(
    drivingMode: Boolean,
    northUp: Boolean,
    onRecenter: () -> Unit,
    onToggleNorthUp: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Column(
        modifier = modifier.padding(12.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
        horizontalAlignment = Alignment.End,
    ) {
        if (drivingMode) {
            FloatingActionButton(
                onClick = onToggleNorthUp,
                modifier = Modifier.size(48.dp),
                containerColor = MaterialTheme.colorScheme.surface,
                elevation = FloatingActionButtonDefaults.elevation(4.dp),
                shape = CircleShape,
            ) {
                Icon(
                    Icons.Default.Explore,
                    contentDescription = UiStrings.Map.compass,
                    tint = if (northUp) AgriColors.Primary else AgriColors.TextSecondary,
                )
            }
        }
        FloatingActionButton(
            onClick = onRecenter,
            modifier = Modifier.size(48.dp),
            containerColor = MaterialTheme.colorScheme.surface,
            elevation = FloatingActionButtonDefaults.elevation(4.dp),
            shape = CircleShape,
        ) {
            Icon(Icons.Default.MyLocation, contentDescription = UiStrings.Map.recenter, tint = AgriColors.Primary)
        }
    }
}
