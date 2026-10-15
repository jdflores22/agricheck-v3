package com.agricheck.agritrack.ui.strings

import com.agricheck.agritrack.util.DeliveryPhase

/** English UI copy for AgriTrack */
object UiStrings {
    const val appName = "AgriTrack"

    object Brand {
        const val name = "AgriTrack"
        const val tagline = "Container delivery & tracking"
        const val integratedWith = "Integrated with AgriCheck"
        fun copyright(year: Int) = "© $year $name. All rights reserved."
    }

    object Nav {
        const val home = "Home"
        const val deliveries = "Deliveries"
        const val loads = "Deliveries"
        const val scan = "Scan"
        const val news = "News"
        const val settings = "Settings"
        const val account = "Account"
        const val operator = "Operator"
        const val profile = "Profile"
        const val more = "More"
    }

    object News {
        const val title = "News & announcements"
        const val searchHint = "Search news…"
        const val filterAll = "View all"
        const val filterNews = "News"
        const val filterAnnouncements = "Announcements"
        const val filterArticles = "Articles"
        const val regulatory = "Regulatory"
        const val empty = "No news posts yet."
        const val loadFailed = "Could not load news."
        fun published(date: String) = "Published $date"
    }

    object Map {
        const val back = "Back"
        const val drive = "Drive"
        const val driving = "Driving"
        const val continueRoute = "Continue on route"
        const val toward = "Toward"
        const val calculating = "Calculating route…"
        const val loadingRoute = "Loading directions…"
        const val preparingNavigation = "Preparing navigation"
        fun loadingDirectionsTo(destinationName: String) = "Loading route to $destinationName"
        const val routeLoadFailed = "Could not load route. Check GPS and try again."
        const val compass = "Compass · north up / heading up"
        const val recenter = "Recenter"
        const val routePreview = "Route preview"
        const val legToPickup = "Leg 1 · to pickup"
        const val legToDestination = "Leg 2 · to warehouse"
        const val startButton = "Start navigation"
        const val settingsTitle = "Map options"
        const val settingTraffic = "Show traffic on map"
        const val settingSatellite = "Satellite map"
        const val settingDirections = "Turn-by-turn directions"
        const val settingAvatar = "Driving avatar"
        const val avatarTruck = "Truck (default)"
        const val avatarArrow = "Navigation arrow"
        const val mapSettings = "Map settings"
        const val trafficColorLegend = "Route colors: blue = clear, yellow = slow, red = heavy. Tap colored segments for time."
        const val legendJam = "Red ~%d min"
        const val legendSlow = "Yellow ~%d min"
        const val legendClear = "Blue = clear"
        const val segmentDuration = "~%d min · %s"
        const val tapSegmentHint = "Tap a red or yellow segment on the route for details."
        const val startToPickupSub = "Navigate to pickup"
        const val startToWarehouseSub = "Navigate to warehouse"
        const val pickupContainer = "Confirm pickup"
        const val pickupContainerSub = ""
        const val arrivedAtWarehouse = "Arrived at warehouse"
        const val arrivedAtWarehouseSub = ""
        const val nearStopHint = "Move closer to %s (%s) to enable."
        const val locationAccuracyPoor = "Waiting for a more accurate GPS fix. Try outdoors or enable high accuracy location."
        const val mustBeAtPickupForWarehouse = "You must be at the pickup point (accurate GPS) before navigating to the warehouse."
        const val waitingForQrScan = "Waiting for NMIS/Doctor to scan delivery QR."
        const val tripEndsWithQr = ""
        const val overviewHint = ""
        const val routeLegendPickupFirst = ""
        const val etaToPickup = ""
        const val startPoint = "Pickup"
        const val endPoint = "Destination"
        const val deliveryQr = "Delivery QR"
        const val pickup = "Pickup"
        const val destination = "Destination"
        const val trafficOn = "Live traffic on map · ETA includes current road conditions"
        const val trafficDelay = "+%d min traffic delay on route"
        const val you = "You"
        const val muteVoice = "Mute voice"
        const val unmuteVoice = "Unmute voice"
    }

    object Permissions {
        const val notificationsTitle = "Notifications"
        const val notificationsBody =
            "We send alerts when a new container is assigned to you so you do not miss a pickup."
        const val locationTitle = "Location while using the app"
        const val locationBody =
            "Your position powers the delivery map, turn-by-turn routing, and pickup or warehouse check-in."
        const val backgroundTitle = "Location in the background"
        const val backgroundBody =
            "While navigation or in-transit tracking is active, AgriTrack updates your route and GPS on the server even when the screen is off."
        const val cameraTitle = "Camera"
        const val cameraBody =
            "The camera scans transport and delivery QR codes during handoff at pickup and the warehouse."
        const val cameraRequired = "Camera permission is required for geotag proof photos."
        const val allow = "Allow permission"
        const val next = "Next"
        const val getStarted = "Continue to app"
        const val requiredHint = "This permission is required to use AgriTrack."
        const val stepCounter = "Step %1\$d of %2\$d"
    }

    object Home {
        const val title = "Home"
        const val greeting = "Hello"
        const val currentDelivery = "Current delivery"
        const val navigationActive = "Navigation active"
        fun navigatingTo(destination: String) = "Toward $destination"
        const val noActive = "No active delivery"
        const val noActiveHint = ""
        const val summary = "Summary"
        const val assigned = "Assigned"
        const val inTransit = "In transit"
        const val profileCompletion = "Profile"
        const val shortcutMap = "Map"
        const val shortcutQr = "QR"
        const val shortcutLoads = "Loads"
        fun continueMap(inTransit: Boolean) = if (inTransit) "Resume" else "Open"
        const val showQr = "QR"
        const val go = "Go"
    }

    object Geotag {
        const val stampLabel = "AgriTrack geotag proof"
        const val pickupTitle = "Pickup geotag photo"
        const val warehouseTitle = "Warehouse geotag photo"
        const val pickupHint = "Take a photo at the pickup point. Coordinates and time will be stamped on the image."
        const val warehouseHint = "Take a photo at the warehouse. Coordinates and time will be stamped on the image."
        const val useMapForPickup = "Open the delivery map to confirm pickup with a geotag photo."
    }

    object Loads {
        const val title = "Deliveries"
        const val subtitle = ""
        const val guidance = ""
        const val empty = "No assigned containers."
        const val confirmPickup = "Confirm pickup"
        const val startDelivery = "Start delivery"
        const val deliveryQr = "Delivery QR"
        const val deliveryMap = "Map"
        const val pickupLabel = "Pickup"
        const val destinationLabel = "Destination"
        const val entry = "Entry"
        const val lastGps = "Last GPS"
        const val locationRequired = "Location required"
        const val pickupConfirmed = "Pickup confirmed. Start navigation to the warehouse."
        const val arrivalConfirmed = "Arrival recorded. Waiting for QR scan."
        const val arrivalOutsideGeofence = "Recorded, but still far from warehouse (%s)."
        fun inTransitMessage(number: String) = "$number — in transit"
    }

    object More {
        const val title = "More"
        const val aboutTitle = "AgriCheck V3"
        const val aboutBody = "AgriTrack mobile app for container transport."
        const val apiSettings = "API settings"
        const val mapsOn = "Google Directions on · in-app map: OSM"
        const val mapsTilesOn = "Google map tiles on"
        const val mapsOff = "OSM map + OSRM routing"
        const val mapsKeyOnly = "Google Directions on · OSM map (default)"
        fun version(name: String) = "Version $name · Android"
    }

    object Profile {
        const val title = "Profile"
        const val driverProfile = "Driver profile"
        const val completion = "Complete"
        const val fleet = "Fleet"
        const val license = "License"
        const val phone = "Phone"
        const val faceVerified = "Face verified"
        const val facePending = "Face verification pending"
        const val apiSettings = "Settings"
        const val driver = "Driver"
        const val logout = "Log out"
        const val details = "Details"
    }

    object Operator {
        const val title = "Claim & assign"
        const val subtitle = ""
        const val flowTitle = ""
        const val flow1 = ""
        const val flow2 = ""
        const val flow3 = ""
        const val flow4 = ""
        const val guidance = ""
        const val empty = "No containers to claim."
        const val claim = "Claim"
        const val assignDriver = "Assign driver"
        const val driverUuidHint = "Driver user UUID"
        fun assignTo(number: String) = "Assign driver to $number"
        fun claimed(number: String) = "Claimed $number"
        const val driverAssigned = "Driver assigned"
        const val scanTitle = "Scan transport QR"
        const val confirm = "Confirm"
        const val cancel = "Cancel"
        const val claimedSection = "Your claimed containers"
        const val bookRouteStops = "Pickup & warehouse pins"
        const val bookRouteHint = "Set coordinates on the map for map/navigation testing."
        const val manualTestTitle = "Manual route test"
        const val manualTestSubtitle = "No claim required — test maps & traffic"
        const val manualTestOpen = "Open manual route test"
        const val manualTestHint = "Drop pickup and warehouse pins, preview Google/OSRM route and ETA here first."
        const val manualTestRefreshRoute = "Refresh route"
        const val manualTestSaveOptional = "Optional: save pins to a container (for driver test)"
        const val manualTestPickContainer = "Select container…"
        const val manualTestSaveToContainer = "Save pins to container"
        fun manualTestSavedTo(number: String) = "Saved to $number — assign a driver to test on map."
        const val manualTestNeedGoogle = "Enable Google map tiles in local.properties to preview the route here."
        const val testRoutePins = "Test route pins"
    }

    object OperatorRouteBooking {
        const val title = "Book route stops"
        const val pickupLabel = "Pickup name"
        const val warehouseLabel = "Warehouse name"
        const val tapPickup = "Tap map — set pickup (green)"
        const val tapWarehouse = "Tap map — set warehouse (orange)"
        const val save = "Save coordinates"
        const val saved = "Pickup and warehouse saved for this container."
        const val loading = "Loading stops…"
        const val wizardPickupTitle = "Step 1 — Pickup point"
        const val wizardWarehouseTitle = "Step 2 — Warehouse"
        const val wizardOverviewTitle = "Step 3 — Route overview"
        const val wizardPickupHint = "Tap the map to place the pickup pin (green). Then tap Next."
        const val wizardWarehouseHint = "Pickup is shown in green. Tap the map to place the warehouse pin (orange)."
        const val wizardOverviewHint = "Review both pins and the route. Save when ready."
        const val wizardNext = "Next"
        const val wizardPinPickupFirst = "Tap the map to set pickup before continuing."
        const val wizardPinWarehouseFirst = "Tap the map to set warehouse before continuing."
        const val wizardPickContainerFirst = "Select a container to save these pins."
    }

    object DeliveryQr {
        const val title = "Delivery QR"
        const val entryPrefix = "Entry"
        const val handoffDefault = "Show this QR at the warehouse for NMIS/Doctor to scan."
        const val noEndTrip = ""
    }

    object ScanQr {
        const val title = "Scan QR"
        const val hint = ""
        const val deliverTo = "Deliver to"
        const val scheduled = "Scheduled"
        const val status = "Status"
        const val scanAgain = "Scan again"
        const val accept = "Accept delivery"
        const val acceptSuccess = "Delivery accepted."
        const val cannotAccept = "Cannot accept this delivery."
    }

    object Settings {
        const val title = "Settings"
        const val apiSection = "API connection"
        const val apiUrlHint = "AgriCheck API base URL"
        const val save = "Save API URL"
        const val saved = "Saved. Restart the app if the URL did not update."
    }

    fun deliveryPhaseLabel(phase: DeliveryPhase): String = when (phase) {
        DeliveryPhase.AwaitingPickup -> "Awaiting pickup"
        DeliveryPhase.PickedUp -> "Picked up"
        DeliveryPhase.InTransit -> "In transit"
        DeliveryPhase.AtWarehouse -> "At destination"
        DeliveryPhase.UnderInspection -> "Awaiting scan"
        DeliveryPhase.Completed -> "Completed"
        DeliveryPhase.Assigned -> "Assigned"
    }
}
