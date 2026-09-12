package com.agricheck.agritrack

import android.Manifest
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.repeatOnLifecycle
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.agricheck.agritrack.push.AssignmentNotifier
import com.agricheck.agritrack.push.PushNotificationManager
import com.agricheck.agritrack.push.PushTokenRegistrar
import com.agricheck.agritrack.ui.navigation.MainBottomBar
import com.agricheck.agritrack.ui.navigation.MainTab
import com.agricheck.agritrack.ui.navigation.Routes
import com.agricheck.agritrack.ui.navigation.tabsForRoles
import com.agricheck.agritrack.ui.screens.*
import com.agricheck.agritrack.ui.screens.auth.LoginScreen
import com.agricheck.agritrack.ui.theme.AgriTrackTheme
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val container = (application as AgriTrackApp).container
        PushNotificationManager.ensureChannels(this)

        setContent {
            AgriTrackTheme {
                val context = LocalContext.current
                val authState by container.authRepository.authState.collectAsState(
                    initial = com.agricheck.agritrack.data.local.AuthState(),
                )
                val rootNav = rememberNavController()
                val scope = rememberCoroutineScope()
                val lifecycleOwner = LocalLifecycleOwner.current

                var apiBaseUrl by remember { mutableStateOf(BuildConfig.API_BASE_URL) }
                LaunchedEffect(Unit) {
                    apiBaseUrl = container.resolveApiBaseUrl(container.settingsStore.getApiBaseUrl())
                }

                val notificationPermissionLauncher = rememberLauncherForActivityResult(
                    ActivityResultContracts.RequestPermission(),
                ) { granted ->
                    if (granted && authState.isLoggedIn) {
                        scope.launch {
                            PushTokenRegistrar.sync(context) { token ->
                                container.pushRepository.register(token, "android")
                            }
                        }
                    }
                }

                LaunchedEffect(authState.isLoggedIn) {
                    if (!authState.isLoggedIn) return@LaunchedEffect
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
                    } else {
                        PushTokenRegistrar.sync(context) { token ->
                            container.pushRepository.register(token, "android")
                        }
                    }
                }

                LaunchedEffect(authState.isLoggedIn, lifecycleOwner) {
                    if (!authState.isLoggedIn) return@LaunchedEffect
                    lifecycleOwner.lifecycle.repeatOnLifecycle(Lifecycle.State.RESUMED) {
                        var initial = true
                        while (true) {
                            runCatching {
                                val pull = container.driverRepository.syncPull()
                                AssignmentNotifier.checkForNewAssignments(
                                    context,
                                    pull.containers,
                                    container.trackingStore,
                                    initial,
                                )
                            }
                            initial = false
                            delay(60_000)
                        }
                    }
                }

                NavHost(
                    navController = rootNav,
                    startDestination = if (authState.isLoggedIn) Routes.MAIN else Routes.LOGIN,
                ) {
                    composable(Routes.LOGIN) {
                        LoginScreen(
                            authRepository = container.authRepository,
                            onLoggedIn = {
                                rootNav.navigate(Routes.MAIN) {
                                    popUpTo(Routes.LOGIN) { inclusive = true }
                                }
                            },
                        )
                    }

                    composable(Routes.MAIN) {
                        val tabs = tabsForRoles(authState.user?.roles ?: emptyList())
                        val tabNav = rememberNavController()
                        var currentTab by remember(tabs) { mutableStateOf(tabs.first().route) }

                        Scaffold(
                            bottomBar = {
                                MainBottomBar(tabs, currentTab) { tab ->
                                    currentTab = tab.route
                                    tabNav.navigate(tab.route) {
                                        popUpTo(tabNav.graph.startDestinationId) { saveState = true }
                                        launchSingleTop = true
                                        restoreState = true
                                    }
                                }
                            },
                        ) { padding ->
                            NavHost(
                                navController = tabNav,
                                startDestination = tabs.first().route,
                                modifier = Modifier.padding(padding),
                            ) {
                                composable(MainTab.Home.route) {
                                    currentTab = MainTab.Home.route
                                    DashboardScreen(authState.user, container.driverRepository)
                                }
                                composable(MainTab.Containers.route) {
                                    currentTab = MainTab.Containers.route
                                    ContainersScreen(
                                        repository = container.driverRepository,
                                        onOpenMap = { containerItem ->
                                            rootNav.navigate(Routes.map(containerItem.uuid, containerItem.containerNumber))
                                        },
                                    )
                                }
                                composable(MainTab.Operator.route) {
                                    currentTab = MainTab.Operator.route
                                    OperatorScreen(container.operatorRepository)
                                }
                                composable(MainTab.Profile.route) {
                                    currentTab = MainTab.Profile.route
                                    ProfileScreen(
                                        user = authState.user,
                                        repository = container.driverRepository,
                                        onLogout = {
                                            scope.launch {
                                                PushTokenRegistrar.unregister(context) { token ->
                                                    container.pushRepository.unregister(token)
                                                }
                                                container.authRepository.logout()
                                                rootNav.navigate(Routes.LOGIN) {
                                                    popUpTo(Routes.MAIN) { inclusive = true }
                                                }
                                            }
                                        },
                                        onOpenSettings = { rootNav.navigate(Routes.SETTINGS) },
                                    )
                                }
                                composable(MainTab.More.route) {
                                    currentTab = MainTab.More.route
                                    MoreScreen(onOpenSettings = { rootNav.navigate(Routes.SETTINGS) })
                                }
                            }
                        }
                    }

                    composable(Routes.SETTINGS) {
                        SettingsScreen(
                            currentApiUrl = apiBaseUrl,
                            onSaveApiUrl = { url ->
                                scope.launch {
                                    container.settingsStore.setApiBaseUrl(url)
                                    container.invalidateNetwork()
                                    apiBaseUrl = container.resolveApiBaseUrl(url)
                                }
                            },
                            onBack = { rootNav.popBackStack() },
                        )
                    }

                    composable(
                        route = Routes.MAP,
                        arguments = listOf(
                            navArgument("uuid") { type = NavType.StringType },
                            navArgument("number") { type = NavType.StringType },
                        ),
                    ) { entry ->
                        val uuid = entry.arguments?.getString("uuid") ?: return@composable
                        val number = entry.arguments?.getString("number") ?: return@composable
                        MapScreen(
                            containerUuid = uuid,
                            containerNumber = number,
                            repository = container.driverRepository,
                            onBack = { rootNav.popBackStack() },
                        )
                    }
                }
            }
        }
    }
}
