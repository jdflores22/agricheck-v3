package com.agricheck.agritrack.ui.screens

import androidx.compose.runtime.*
import com.agricheck.agritrack.data.model.ContainerListItemDto
import com.agricheck.agritrack.data.repository.OperatorRepository
import kotlinx.coroutines.launch

@Composable
fun OperatorManualRouteTestScreen(
    repository: OperatorRepository,
    onBack: () -> Unit,
) {
    val scope = rememberCoroutineScope()
    var containers by remember { mutableStateOf<List<ContainerListItemDto>>(emptyList()) }

    LaunchedEffect(Unit) {
        scope.launch {
            runCatching {
                val claimable = repository.claimableContainers()
                val claimed = repository.claimedContainers()
                containers = (claimed + claimable).distinctBy { it.uuid }
            }
        }
    }

    OperatorRouteBookWizard(
        repository = repository,
        onBack = onBack,
        saveContainerChoices = containers,
    )
}
