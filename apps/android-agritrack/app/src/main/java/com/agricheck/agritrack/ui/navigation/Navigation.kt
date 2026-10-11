package com.agricheck.agritrack.ui.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Article
import androidx.compose.material.icons.automirrored.filled.Assignment
import androidx.compose.material.icons.automirrored.outlined.Assignment
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Map
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.outlined.Article
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.Map
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.FloatingActionButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriColors

sealed class MainTab(val route: String, val label: String) {
    data object Home : MainTab("home", UiStrings.Nav.home)
    data object Deliveries : MainTab("containers", UiStrings.Nav.deliveries)
    data object Scan : MainTab("scan", UiStrings.Nav.scan)
    data object News : MainTab("news", UiStrings.Nav.news)
    data object Settings : MainTab("settings", UiStrings.Nav.settings)
    data object Profile : MainTab("profile", UiStrings.Nav.account)
    data object Operator : MainTab("operator", UiStrings.Nav.operator)
}

object Routes {
    const val LOGIN = "login"
    const val REGISTER = "register"
    const val MAIN = "main"
    const val PERMISSIONS = "permissions"
    const val SETTINGS = "settings"
    const val MAP = "map/{uuid}/{number}"
    const val DELIVERY_QR = "delivery_qr/{uuid}/{number}"

    fun map(uuid: String, number: String) = "map/$uuid/$number"
    fun deliveryQr(uuid: String, number: String) = "delivery_qr/$uuid/$number"
}

enum class ShellSecondTab { Deliveries, Operator, None }

data class MainShellConfig(
    val secondTab: ShellSecondTab,
    val showCenterQr: Boolean,
    val showOperatorInSettings: Boolean,
)

fun shellConfigForRoles(roles: List<String>): MainShellConfig {
    val isDriver = roles.any { it.equals("ROLE_DRIVER", true) || it.equals("ROLE_ADMIN", true) }
    val isOperator = roles.any { it.equals("ROLE_OPERATOR", true) || it.equals("ROLE_ADMIN", true) }
    val secondTab = when {
        isDriver -> ShellSecondTab.Deliveries
        isOperator -> ShellSecondTab.Operator
        else -> ShellSecondTab.None
    }
    return MainShellConfig(
        secondTab = secondTab,
        showCenterQr = isDriver,
        showOperatorInSettings = isOperator && isDriver,
    )
}

fun bottomBarSelection(currentRoute: String): String = when (currentRoute) {
    MainTab.Scan.route, MainTab.Profile.route, MainTab.Operator.route -> ""
    else -> currentRoute
}

@Composable
fun AgriMainBottomBar(
    selectedRoute: String,
    config: MainShellConfig,
    onHome: () -> Unit,
    onSecondTab: () -> Unit,
    onQr: () -> Unit,
    onNews: () -> Unit,
    onSettings: () -> Unit,
) {
    val barHeight = 62.dp
    val fabSize = 56.dp
    Box(Modifier.fillMaxWidth()) {
        Surface(
            modifier = Modifier
                .fillMaxWidth()
                .height(barHeight),
            color = Color.White,
            shadowElevation = 10.dp,
            tonalElevation = 0.dp,
        ) {
            Row(
                Modifier
                    .fillMaxSize()
                    .padding(horizontal = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                BottomBarItem(
                    selected = selectedRoute == MainTab.Home.route,
                    label = MainTab.Home.label,
                    selectedIcon = Icons.Filled.Home,
                    unselectedIcon = Icons.Outlined.Home,
                    onClick = onHome,
                    modifier = Modifier.weight(1f),
                )
                when (config.secondTab) {
                    ShellSecondTab.Deliveries -> BottomBarItem(
                        selected = selectedRoute == MainTab.Deliveries.route,
                        label = MainTab.Deliveries.label,
                        selectedIcon = Icons.Filled.Map,
                        unselectedIcon = Icons.Outlined.Map,
                        onClick = onSecondTab,
                        modifier = Modifier.weight(1f),
                    )
                    ShellSecondTab.Operator -> BottomBarItem(
                        selected = selectedRoute == MainTab.Operator.route,
                        label = MainTab.Operator.label,
                        selectedIcon = Icons.AutoMirrored.Filled.Assignment,
                        unselectedIcon = Icons.AutoMirrored.Outlined.Assignment,
                        onClick = onSecondTab,
                        modifier = Modifier.weight(1f),
                    )
                    ShellSecondTab.None -> Spacer(Modifier.weight(1f))
                }
                Spacer(Modifier.width(fabSize))
                BottomBarItem(
                    selected = selectedRoute == MainTab.News.route,
                    label = MainTab.News.label,
                    selectedIcon = Icons.AutoMirrored.Filled.Article,
                    unselectedIcon = Icons.AutoMirrored.Outlined.Article,
                    onClick = onNews,
                    modifier = Modifier.weight(1f),
                )
                BottomBarItem(
                    selected = selectedRoute == MainTab.Settings.route,
                    label = MainTab.Settings.label,
                    selectedIcon = Icons.Filled.Settings,
                    unselectedIcon = Icons.Outlined.Settings,
                    onClick = onSettings,
                    modifier = Modifier.weight(1f),
                )
            }
        }
        if (config.showCenterQr) {
            FloatingActionButton(
                onClick = onQr,
                modifier = Modifier
                    .align(Alignment.TopCenter)
                    .offset(y = (-18).dp)
                    .size(fabSize),
                shape = CircleShape,
                containerColor = AgriColors.PrimaryDark,
                elevation = FloatingActionButtonDefaults.elevation(defaultElevation = 6.dp),
            ) {
                Icon(
                    Icons.Default.QrCodeScanner,
                    contentDescription = UiStrings.Nav.scan,
                    tint = Color.White,
                    modifier = Modifier.size(28.dp),
                )
            }
        }
    }
}

@Composable
private fun BottomBarItem(
    selected: Boolean,
    label: String,
    selectedIcon: ImageVector,
    unselectedIcon: ImageVector,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val color = if (selected) AgriColors.PrimaryDark else AgriColors.TextSecondary
    androidx.compose.material3.TextButton(
        onClick = onClick,
        modifier = modifier.padding(vertical = 2.dp),
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Icon(
                if (selected) selectedIcon else unselectedIcon,
                contentDescription = label,
                tint = color,
                modifier = Modifier.size(22.dp),
            )
            Spacer(Modifier.height(2.dp))
            Text(
                label,
                color = color,
                fontSize = 11.sp,
                fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                textAlign = TextAlign.Center,
            )
        }
    }
}
