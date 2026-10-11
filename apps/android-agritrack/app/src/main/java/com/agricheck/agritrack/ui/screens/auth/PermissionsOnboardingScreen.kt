package com.agricheck.agritrack.ui.screens.auth

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CameraAlt
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.ShareLocation
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import com.agricheck.agritrack.data.local.SettingsStore
import com.agricheck.agritrack.push.PushTokenRegistrar
import com.agricheck.agritrack.ui.components.AgriBrandLogo
import com.agricheck.agritrack.ui.components.AgriBrandName
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors
import kotlinx.coroutines.launch

private enum class PermissionStep(
    val title: String,
    val body: String,
    val icon: ImageVector,
) {
    Notifications(
        UiStrings.Permissions.notificationsTitle,
        UiStrings.Permissions.notificationsBody,
        Icons.Default.Notifications,
    ),
    Location(
        UiStrings.Permissions.locationTitle,
        UiStrings.Permissions.locationBody,
        Icons.Default.LocationOn,
    ),
    BackgroundLocation(
        UiStrings.Permissions.backgroundTitle,
        UiStrings.Permissions.backgroundBody,
        Icons.Default.ShareLocation,
    ),
    Camera(
        UiStrings.Permissions.cameraTitle,
        UiStrings.Permissions.cameraBody,
        Icons.Default.CameraAlt,
    ),
}

private fun stepsForDevice(): List<PermissionStep> = buildList {
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
        add(PermissionStep.Notifications)
    }
    add(PermissionStep.Location)
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
        add(PermissionStep.BackgroundLocation)
    }
    add(PermissionStep.Camera)
}

@Composable
fun PermissionsOnboardingScreen(
    settingsStore: SettingsStore,
    onRegisterPushToken: suspend (String) -> Unit,
    onFinished: () -> Unit,
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val steps = remember { stepsForDevice() }
    var stepIndex by remember { mutableIntStateOf(0) }
    val step = steps[stepIndex]
    var awaitingGrant by remember { mutableStateOf(false) }

    fun hasPermission(permission: String): Boolean =
        ContextCompat.checkSelfPermission(context, permission) == PackageManager.PERMISSION_GRANTED

    fun stepGranted(s: PermissionStep): Boolean = when (s) {
        PermissionStep.Notifications ->
            Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU ||
                hasPermission(Manifest.permission.POST_NOTIFICATIONS)
        PermissionStep.Location ->
            hasPermission(Manifest.permission.ACCESS_FINE_LOCATION) &&
                hasPermission(Manifest.permission.ACCESS_COARSE_LOCATION)
        PermissionStep.BackgroundLocation ->
            Build.VERSION.SDK_INT < Build.VERSION_CODES.Q ||
                hasPermission(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
        PermissionStep.Camera -> hasPermission(Manifest.permission.CAMERA)
    }

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions(),
    ) { _ ->
        awaitingGrant = false
    }

    fun requestCurrentStep() {
        val permissions = when (step) {
            PermissionStep.Notifications ->
                arrayOf(Manifest.permission.POST_NOTIFICATIONS)
            PermissionStep.Location ->
                arrayOf(
                    Manifest.permission.ACCESS_FINE_LOCATION,
                    Manifest.permission.ACCESS_COARSE_LOCATION,
                )
            PermissionStep.BackgroundLocation ->
                arrayOf(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
            PermissionStep.Camera -> arrayOf(Manifest.permission.CAMERA)
        }
        awaitingGrant = true
        permissionLauncher.launch(permissions)
    }

    LaunchedEffect(stepIndex) {
        if (stepGranted(step)) return@LaunchedEffect
        requestCurrentStep()
    }

    Column(
        Modifier
            .fillMaxSize()
            .background(AgriColors.Background)
            .statusBarsPadding()
            .padding(horizontal = 24.dp, vertical = 20.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            AgriBrandLogo(size = 40.dp)
            Spacer(Modifier.width(12.dp))
            AgriBrandName()
        }
        Spacer(Modifier.height(28.dp))
        LinearProgressIndicator(
            progress = { (stepIndex + 1f) / steps.size },
            modifier = Modifier.fillMaxWidth(),
            color = AgriColors.Primary,
        )
        Spacer(Modifier.height(8.dp))
        Text(
            "Step ${stepIndex + 1} of ${steps.size}",
            style = MaterialTheme.typography.labelMedium,
            color = AgriColors.TextSecondary,
        )
        Spacer(Modifier.height(32.dp))
        Surface(
            shape = RoundedCornerShape(24.dp),
            color = Color.White,
            shadowElevation = 2.dp,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Column(
                Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                Box(
                    Modifier
                        .size(72.dp)
                        .background(AgriColors.Primary.copy(alpha = 0.12f), RoundedCornerShape(20.dp)),
                    contentAlignment = Alignment.Center,
                ) {
                    Icon(step.icon, contentDescription = null, tint = AgriColors.PrimaryDark, modifier = Modifier.size(36.dp))
                }
                Spacer(Modifier.height(20.dp))
                Text(
                    step.title,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center,
                )
                Spacer(Modifier.height(12.dp))
                Text(
                    step.body,
                    style = MaterialTheme.typography.bodyMedium,
                    color = AgriColors.TextSecondary,
                    textAlign = TextAlign.Center,
                )
            }
        }
        Spacer(Modifier.weight(1f))
        if (!stepGranted(step)) {
            Text(
                UiStrings.Permissions.requiredHint,
                style = MaterialTheme.typography.bodySmall,
                color = AgriColors.TextSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(bottom = 12.dp),
            )
            Button(
                onClick = { requestCurrentStep() },
                enabled = !awaitingGrant,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(14.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
            ) {
                Text(UiStrings.Permissions.allow, fontWeight = FontWeight.Bold)
            }
        } else {
            Button(
                onClick = {
                    if (stepIndex < steps.lastIndex) {
                        stepIndex++
                    } else {
                        scope.launch {
                            settingsStore.setPermissionsOnboardingComplete(true)
                            PushTokenRegistrar.sync(context, onRegisterPushToken)
                            onFinished()
                        }
                    }
                },
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(14.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AgriColors.Primary),
            ) {
                Text(
                    if (stepIndex < steps.lastIndex) UiStrings.Permissions.next else UiStrings.Permissions.getStarted,
                    fontWeight = FontWeight.Bold,
                )
            }
        }
    }
}
