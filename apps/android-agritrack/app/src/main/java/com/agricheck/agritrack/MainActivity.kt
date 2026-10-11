package com.agricheck.agritrack

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
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
import com.agricheck.agritrack.ui.navigation.AgriMainBottomBar
import com.agricheck.agritrack.ui.navigation.MainTab
import com.agricheck.agritrack.ui.navigation.Routes
import com.agricheck.agritrack.ui.navigation.bottomBarSelection
import com.agricheck.agritrack.ui.navigation.shellConfigForRoles
import com.agricheck.agritrack.ui.components.AgriBrandTopBar
import com.agricheck.agritrack.ui.screens.*
import com.agricheck.agritrack.ui.screens.auth.LoginScreen
import com.agricheck.agritrack.ui.screens.auth.PermissionsOnboardingScreen
import com.agricheck.agritrack.ui.screens.auth.RegisterScreen
import com.agricheck.agritrack.ui.strings.UiStrings
import com.agricheck.agritrack.ui.theme.AgriTrackTheme
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val container = (application as AgriTrackApp).container
        PushNotificationManager.ensureChannels(this)

        setContent {
            AgriTrackTheme {
                val context = LocalContext.current
                val activity = context as MainActivity
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

                var coldStartReady by remember { mutableStateOf(false) }
                var startDestination by remember { mutableStateOf(Routes.LOGIN) }
                var pendingMapLink by remember { mutableStateOf(parseMapDeepLink(activity.intent)) }

                LaunchedEffect(authState.isLoggedIn) {
                    if (coldStartReady) return@LaunchedEffect
                    startDestination = when {
                        !authState.isLoggedIn -> Routes.LOGIN
                        container.settingsStore.isPermissionsOnboardingComplete() -> Routes.MAIN
                        else -> Routes.PERMISSIONS
                    }
                    coldStartReady = true
                }

                LaunchedEffect(lifecycleOwner) {
                    lifecycleOwner.lifecycle.repeatOnLifecycle(Lifecycle.State.RESUMED) {
                        parseMapDeepLink(activity.intent)?.let { pendingMapLink = it }
                    }
                }

                LaunchedEffect(pendingMapLink, authState.isLoggedIn, coldStartReady) {
                    val link = pendingMapLink ?: return@LaunchedEffect
                    if (!authState.isLoggedIn || !coldStartReady) return@LaunchedEffect
                    if (!container.settingsStore.isPermissionsOnboardingComplete()) return@LaunchedEffect
                    rootNav.navigate(Routes.map(link.uuid, link.number)) {
                        launchSingleTop = true
                    }
                    pendingMapLink = null
                    activity.intent.removeExtra(PushNotificationManager.EXTRA_OPEN_MAP)
                }

                LaunchedEffect(authState.isLoggedIn) {
                    if (!authState.isLoggedIn) return@LaunchedEffect
                    if (!container.settingsStore.isPermissionsOnboardingComplete()) return@LaunchedEffect
                    PushTokenRegistrar.sync(context) { token ->
                        container.pushRepository.register(token, "android")
                    }
                }

                suspend fun navigateAfterAuth() {
                    val dest = if (container.settingsStore.isPermissionsOnboardingComplete()) {
                        Routes.MAIN
                    } else {
                        Routes.PERMISSIONS
                    }
                    rootNav.navigate(dest) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
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

                if (!coldStartReady) {
                    Box(Modifier.fillMaxSize())
                } else {
                NavHost(
                    navController = rootNav,
                    startDestination = startDestination,
                ) {
                    composable(Routes.LOGIN) {
                        LoginScreen(
                            authRepository = container.authRepository,
                            onLoggedIn = { scope.launch { navigateAfterAuth() } },
                            onRegister = { rootNav.navigate(Routes.REGISTER) },
                        )
                    }

                    composable(Routes.REGISTER) {
                        RegisterScreen(
                            authRepository = container.authRepository,
                            driverRepository = container.driverRepository,
                            onRegistered = { scope.launch { navigateAfterAuth() } },
                            onBackToLogin = { rootNav.popBackStack() },
                        )
                    }

                    composable(Routes.PERMISSIONS) {
                        PermissionsOnboardingScreen(
                            settingsStore = container.settingsStore,
                            onRegisterPushToken = { token -> container.pushRepository.register(token, "android") },
                            onFinished = {
                                rootNav.navigate(Routes.MAIN) {
                                    popUpTo(Routes.PERMISSIONS) { inclusive = true }
                                }
                            },
                        )
                    }

                    composable(Routes.MAIN) {
                        val shell = shellConfigForRoles(authState.user?.roles ?: emptyList())
                        val tabNav = rememberNavController()
                        var currentTab by remember { mutableStateOf(MainTab.Home.route) }
                        var lastBottomTab by remember { mutableStateOf(MainTab.Home.route) }
                        val bottomSelected = bottomBarSelection(currentTab).ifBlank { lastBottomTab }.let { route ->
                            if (route == MainTab.Operator.route && shell.secondTab != com.agricheck.agritrack.ui.navigation.ShellSecondTab.Operator) {
                                lastBottomTab
                            } else {
                                route
                            }
                        }

                        LaunchedEffect(currentTab) {
                            if (currentTab != MainTab.Scan.route &&
                                currentTab != MainTab.Profile.route &&
                                currentTab != MainTab.Operator.route
                            ) {
                                lastBottomTab = currentTab
                            }
                        }

                        fun navigateTab(route: String) {
                            currentTab = route
                            tabNav.navigate(route) {
                                popUpTo(tabNav.graph.startDestinationId) { saveState = true }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }

                        Scaffold(
                            topBar = {
                                if (currentTab != MainTab.Scan.route) {
                                    val pageTitle = when (currentTab) {
                                        MainTab.Home.route -> MainTab.Home.label
                                        MainTab.Deliveries.route -> MainTab.Deliveries.label
                                        MainTab.News.route -> MainTab.News.label
                                        MainTab.Settings.route -> MainTab.Settings.label
                                        MainTab.Profile.route -> UiStrings.Nav.account
                                        MainTab.Operator.route -> UiStrings.Nav.operator
                                        else -> null
                                    }
                                    AgriBrandTopBar(pageTitle = pageTitle)
                                }
                            },
                            bottomBar = {
                                AgriMainBottomBar(
                                    selectedRoute = bottomSelected,
                                    config = shell,
                                    onHome = { navigateTab(MainTab.Home.route) },
                                    onSecondTab = {
                                        when (shell.secondTab) {
                                            com.agricheck.agritrack.ui.navigation.ShellSecondTab.Deliveries ->
                                                navigateTab(MainTab.Deliveries.route)
                                            com.agricheck.agritrack.ui.navigation.ShellSecondTab.Operator ->
                                                navigateTab(MainTab.Operator.route)
                                            else -> Unit
                                        }
                                    },
                                    onQr = {
                                        currentTab = MainTab.Scan.route
                                        tabNav.navigate(MainTab.Scan.route) { launchSingleTop = true }
                                    },
                                    onNews = { navigateTab(MainTab.News.route) },
                                    onSettings = { navigateTab(MainTab.Settings.route) },
                                )
                            },
                        ) { padding ->
                            NavHost(
                                navController = tabNav,
                                startDestination = MainTab.Home.route,
                                modifier = Modifier.padding(padding),
                            ) {
                                composable(MainTab.Home.route) {
                                    currentTab = MainTab.Home.route
                                    DashboardScreen(
                                        user = authState.user,
                                        repository = container.driverRepository,
                                        onOpenMap = { item ->
                                            rootNav.navigate(Routes.map(item.uuid, item.containerNumber))
                                        },
                                        onOpenQr = { item ->
                                            rootNav.navigate(Routes.deliveryQr(item.uuid, item.containerNumber))
                                        },
                                    )
                                }
                                composable(MainTab.Deliveries.route) {
                                    currentTab = MainTab.Deliveries.route
                                    ContainersScreen(
                                        repository = container.driverRepository,
                                        onOpenMap = { containerItem ->
                                            rootNav.navigate(Routes.map(containerItem.uuid, containerItem.containerNumber))
                                        },
                                        onOpenQr = { containerItem ->
                                            rootNav.navigate(Routes.deliveryQr(containerItem.uuid, containerItem.containerNumber))
                                        },
                                    )
                                }
                                composable(MainTab.Scan.route) {
                                    QrScanScreen(
                                        repository = container.driverRepository,
                                        onAccepted = {
                                            navigateTab(MainTab.Deliveries.route)
                                        },
                                    )
                                }
                                composable(MainTab.News.route) {
                                    currentTab = MainTab.News.route
                                    NewsScreen(repository = container.newsRepository)
                                }
                                composable(MainTab.Settings.route) {
                                    currentTab = MainTab.Settings.route
                                    SettingsHubScreen(
                                        user = authState.user,
                                        showOperator = shell.showOperatorInSettings,
                                        onOpenProfile = {
                                            currentTab = MainTab.Profile.route
                                            tabNav.navigate(MainTab.Profile.route) { launchSingleTop = true }
                                        },
                                        onOpenOperator = {
                                            currentTab = MainTab.Operator.route
                                            tabNav.navigate(MainTab.Operator.route) { launchSingleTop = true }
                                        },
                                        onOpenApiSettings = { rootNav.navigate(Routes.SETTINGS) },
                                    )
                                }
                                composable(MainTab.Profile.route) {
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
                                composable(MainTab.Operator.route) {
                                    OperatorScreen(container.operatorRepository)
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
                            onOpenQr = { rootNav.navigate(Routes.deliveryQr(uuid, number)) },
                        )
                    }

                    composable(
                        route = Routes.DELIVERY_QR,
                        arguments = listOf(
                            navArgument("uuid") { type = NavType.StringType },
                            navArgument("number") { type = NavType.StringType },
                        ),
                    ) { entry ->
                        val uuid = entry.arguments?.getString("uuid") ?: return@composable
                        val number = entry.arguments?.getString("number") ?: return@composable
                        DeliveryQrScreen(
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
}

private data class MapDeepLink(val uuid: String, val number: String)

private fun parseMapDeepLink(intent: Intent?): MapDeepLink? {
    if (intent?.getBooleanExtra(PushNotificationManager.EXTRA_OPEN_MAP, false) != true) return null
    val uuid = intent.getStringExtra(PushNotificationManager.EXTRA_CONTAINER_UUID)?.takeIf { it.isNotBlank() }
        ?: return null
    val number = intent.getStringExtra(PushNotificationManager.EXTRA_CONTAINER_NUMBER) ?: ""
    return MapDeepLink(uuid, number)
}
