package com.agricheck.agritrack.ui.navigation

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Assignment
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.agricheck.agritrack.ui.theme.AgriColors

sealed class MainTab(val route: String, val label: String, val icon: ImageVector) {
    data object Home : MainTab("home", "Home", Icons.Default.Home)
    data object Containers : MainTab("containers", "Loads", Icons.Default.LocalShipping)
    data object Operator : MainTab("operator", "Claim", Icons.AutoMirrored.Filled.Assignment)
    data object Profile : MainTab("profile", "Profile", Icons.Default.Person)
    data object More : MainTab("more", "More", Icons.Default.Menu)
}

object Routes {
    const val LOGIN = "login"
    const val MAIN = "main"
    const val SETTINGS = "settings"
    const val MAP = "map/{uuid}/{number}"

    fun map(uuid: String, number: String) = "map/$uuid/$number"
}

fun tabsForRoles(roles: List<String>): List<MainTab> {
    val isDriver = roles.any { it.equals("ROLE_DRIVER", true) || it.equals("ROLE_ADMIN", true) }
    val isOperator = roles.any { it.equals("ROLE_OPERATOR", true) || it.equals("ROLE_ADMIN", true) }
    return buildList {
        add(MainTab.Home)
        if (isDriver) add(MainTab.Containers)
        if (isOperator) add(MainTab.Operator)
        add(MainTab.Profile)
        add(MainTab.More)
    }
}

@Composable
fun MainBottomBar(tabs: List<MainTab>, currentRoute: String, onTabSelected: (MainTab) -> Unit) {
    NavigationBar(containerColor = AgriColors.Surface, tonalElevation = 3.dp) {
        tabs.forEach { tab ->
            NavigationBarItem(
                selected = currentRoute == tab.route,
                onClick = { onTabSelected(tab) },
                icon = { Icon(tab.icon, contentDescription = tab.label) },
                label = { Text(tab.label) },
                colors = NavigationBarItemDefaults.colors(
                    selectedIconColor = AgriColors.Primary,
                    selectedTextColor = AgriColors.Primary,
                    indicatorColor = AgriColors.Primary.copy(alpha = 0.12f),
                ),
            )
        }
    }
}
