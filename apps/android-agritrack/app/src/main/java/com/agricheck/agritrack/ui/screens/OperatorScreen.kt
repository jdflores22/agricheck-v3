package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.data.model.ContainerListItemDto
import com.agricheck.agritrack.data.repository.OperatorRepository
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.theme.AgriColors
import kotlinx.coroutines.launch

@Composable
fun OperatorScreen(repository: OperatorRepository) {
    var containers by remember { mutableStateOf<List<ContainerListItemDto>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var assignTarget by remember { mutableStateOf<ContainerListItemDto?>(null) }
    var driverUuid by remember { mutableStateOf("") }
    var busy by remember { mutableStateOf(false) }
    var message by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    fun load() {
        scope.launch {
            loading = true
            error = null
            runCatching { repository.claimableContainers() }
                .onSuccess { containers = it }
                .onFailure { error = it.message }
            loading = false
        }
    }

    LaunchedEffect(Unit) { load() }

    AgriScreenScaffold(
        title = "Claim & Assign",
        subtitle = "Confirm tagged containers and assign drivers",
        refreshing = loading,
        onRefresh = { load() },
    ) { padding ->
        LazyColumn(
            Modifier.padding(padding).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            item {
                AgriGuidanceBanner("Claim a container first, then assign a driver using their user UUID.")
            }
            message?.let {
                item { Text(it, color = AgriColors.Success, fontWeight = FontWeight.SemiBold) }
            }
            if (error != null && containers.isEmpty()) {
                item { ErrorState(error ?: "Failed") { load() } }
            }
            if (!loading && containers.isEmpty() && error == null) {
                item { EmptyState("No claimable containers right now.") }
            }
            items(containers, key = { it.uuid }) { container ->
                AgriListItemCard(
                    title = container.containerNumber,
                    subtitle = "Entry ${container.entryReference}",
                    status = container.status,
                )
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    Button(
                        onClick = {
                            busy = true
                            scope.launch {
                                runCatching { repository.claim(container.uuid) }
                                    .onSuccess { message = "Claimed ${container.containerNumber}"; load() }
                                    .onFailure { message = it.message }
                                busy = false
                            }
                        },
                        enabled = !busy,
                        colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
                    ) { Text("Claim") }
                    OutlinedButton(onClick = { assignTarget = container }, enabled = !busy) {
                        Text("Assign driver")
                    }
                }
            }
            assignTarget?.let { target ->
                item {
                    ElevatedCard(shape = MaterialTheme.shapes.large, modifier = Modifier.fillMaxWidth()) {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text("Assign driver to ${target.containerNumber}", fontWeight = FontWeight.Bold)
                            AgriOutlinedField(driverUuid, { driverUuid = it }, "Driver user UUID")
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                Button(
                                    onClick = {
                                        busy = true
                                        scope.launch {
                                            runCatching { repository.assignDriver(target.uuid, driverUuid.trim()) }
                                                .onSuccess {
                                                    message = "Driver assigned"
                                                    assignTarget = null
                                                    driverUuid = ""
                                                    load()
                                                }
                                                .onFailure { message = it.message }
                                            busy = false
                                        }
                                    },
                                    enabled = !busy && driverUuid.isNotBlank(),
                                ) { Text("Confirm") }
                                OutlinedButton(onClick = { assignTarget = null; driverUuid = "" }) {
                                    Text("Cancel")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun SettingsScreen(
    currentApiUrl: String,
    onSaveApiUrl: (String) -> Unit,
    onBack: () -> Unit,
) {
    var apiUrl by remember(currentApiUrl) { mutableStateOf(currentApiUrl) }
    var saved by remember { mutableStateOf(false) }

    AgriScreenScaffold(title = "Settings", onBack = onBack) { padding ->
        Column(Modifier.padding(padding).padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            AgriSectionTitle("API connection")
            AgriOutlinedField(apiUrl, { apiUrl = it }, "AgriCheck API base URL")
            Text(
                "Production: https://agricheck-v3-production.up.railway.app",
                style = MaterialTheme.typography.bodySmall,
                color = AgriColors.TextSecondary,
            )
            AgriPrimaryButton(
                text = "Save API URL",
                onClick = {
                    onSaveApiUrl(apiUrl.trim())
                    saved = true
                },
                enabled = apiUrl.isNotBlank(),
            )
            if (saved) {
                Text("Saved. Restart the app if requests still use the old URL.", color = AgriColors.Success)
            }
        }
    }
}
