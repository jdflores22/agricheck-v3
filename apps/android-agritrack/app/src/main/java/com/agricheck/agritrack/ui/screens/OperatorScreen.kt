package com.agricheck.agritrack.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.data.model.ContainerListItemDto
import com.agricheck.agritrack.data.repository.OperatorRepository
import com.agricheck.agritrack.ui.components.*
import com.agricheck.agritrack.ui.strings.UiStrings
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
        title = UiStrings.Operator.title,
        nestedInMainShell = true,
        refreshing = loading,
        onRefresh = { load() },
    ) { padding ->
        LazyColumn(
            Modifier.padding(padding).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            message?.let {
                item { Text(it, color = AgriColors.Success, fontWeight = FontWeight.SemiBold) }
            }
            if (error != null && containers.isEmpty()) {
                item { ErrorState(error ?: "Failed") { load() } }
            }
            if (!loading && containers.isEmpty() && error == null) {
                item { EmptyState(UiStrings.Operator.empty) }
            }
            items(containers, key = { it.uuid }) { container ->
                Surface(
                    shape = RoundedCornerShape(20.dp),
                    color = Color.White,
                    shadowElevation = 1.dp,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                        Text(container.containerNumber, fontWeight = FontWeight.Bold, color = AgriColors.OnSurface)
                        Text(
                            "${UiStrings.Loads.entry} ${container.entryReference}",
                            color = AgriColors.TextSecondary,
                            style = MaterialTheme.typography.bodySmall,
                        )
                        StatusChip(container.status)
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            Button(
                                onClick = {
                                    busy = true
                                    scope.launch {
                                        runCatching { repository.claim(container.uuid) }
                                            .onSuccess {
                                                message = UiStrings.Operator.claimed(container.containerNumber)
                                                load()
                                            }
                                            .onFailure { message = it.message }
                                        busy = false
                                    }
                                },
                                enabled = !busy,
                                shape = RoundedCornerShape(12.dp),
                                colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
                            ) { Text(UiStrings.Operator.claim) }
                            OutlinedButton(
                                onClick = { assignTarget = container },
                                enabled = !busy,
                                shape = RoundedCornerShape(12.dp),
                            ) {
                                Text(UiStrings.Operator.assignDriver)
                            }
                        }
                    }
                }
            }
            assignTarget?.let { target ->
                item {
                    Surface(
                        shape = RoundedCornerShape(20.dp),
                        color = Color.White,
                        shadowElevation = 2.dp,
                        modifier = Modifier.fillMaxWidth(),
                    ) {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text(UiStrings.Operator.assignTo(target.containerNumber), fontWeight = FontWeight.Bold)
                            AgriOutlinedField(driverUuid, { driverUuid = it }, UiStrings.Operator.driverUuidHint)
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                Button(
                                    onClick = {
                                        busy = true
                                        scope.launch {
                                            runCatching { repository.assignDriver(target.uuid, driverUuid.trim()) }
                                                .onSuccess {
                                                    message = UiStrings.Operator.driverAssigned
                                                    assignTarget = null
                                                    driverUuid = ""
                                                    load()
                                                }
                                                .onFailure { message = it.message }
                                            busy = false
                                        }
                                    },
                                    enabled = !busy && driverUuid.isNotBlank(),
                                ) { Text(UiStrings.Operator.confirm) }
                                OutlinedButton(onClick = { assignTarget = null; driverUuid = "" }) {
                                    Text(UiStrings.Operator.cancel)
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

    AgriScreenScaffold(title = UiStrings.Settings.title, onBack = onBack) { padding ->
        Column(
            Modifier.padding(padding).padding(horizontal = 20.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            Surface(
                shape = RoundedCornerShape(20.dp),
                color = Color.White,
                shadowElevation = 1.dp,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    AgriSectionTitle(UiStrings.Settings.apiSection)
                    AgriOutlinedField(apiUrl, { apiUrl = it }, UiStrings.Settings.apiUrlHint)
                    Text(
                        "Production: https://agricheck-v3-production.up.railway.app",
                        style = MaterialTheme.typography.bodySmall,
                        color = AgriColors.TextSecondary,
                    )
                    AgriPrimaryButton(
                        text = UiStrings.Settings.save,
                        onClick = {
                            onSaveApiUrl(apiUrl.trim())
                            saved = true
                        },
                        enabled = apiUrl.isNotBlank(),
                    )
                    if (saved) {
                        Text(UiStrings.Settings.saved, color = AgriColors.Success, fontWeight = FontWeight.Medium)
                    }
                }
            }
        }
    }
}
