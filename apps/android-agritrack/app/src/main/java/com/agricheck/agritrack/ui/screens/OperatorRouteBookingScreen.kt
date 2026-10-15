package com.agricheck.agritrack.ui.screens

import androidx.compose.runtime.Composable
import com.agricheck.agritrack.data.repository.OperatorRepository

@Composable
fun OperatorRouteBookingScreen(
    containerUuid: String,
    containerNumber: String,
    repository: OperatorRepository,
    onBack: () -> Unit,
) {
    OperatorRouteBookWizard(
        repository = repository,
        onBack = onBack,
        fixedContainerUuid = containerUuid,
        fixedContainerNumber = containerNumber,
    )
}
