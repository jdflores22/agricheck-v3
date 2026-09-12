# AgriTrack Native Android (AgriCheck V3)

Production-grade native Android app for AgriCheck V3 transport operations. Built with the **same stack as ECMS `android-trucker`**.

## Tech stack

| Layer | Technology |
|-------|------------|
| Language | Kotlin (JVM 17) |
| UI | Jetpack Compose + Material 3 |
| Navigation | Navigation Compose |
| Networking | Retrofit + OkHttp + kotlinx-serialization |
| Auth storage | DataStore Preferences |
| Maps | OSMDroid + OSRM (`router.project-osrm.org`) |
| GPS | Play Services Location + foreground service |
| Push | Firebase Cloud Messaging (optional) + local assignment alerts |

## Features

- Branded login and dashboard (AgriCheck V3 green theme)
- Driver: assigned loads, status updates, background GPS while **In Transit**
- Live map: GPS trail + OSRM driving route to warehouse destination
- Operator: claim containers and assign drivers
- Profile + driver completion stats
- Configurable API base URL (Settings)
- Assignment notifications (FCM when configured, polling fallback)

## Setup

1. Copy `local.properties.example` → `local.properties`
2. Set `sdk.dir`. Default API is `https://agricheck-v3-production.up.railway.app`. For a local API, set `API_BASE_URL=http://10.0.2.2:5000`.
3. Start AgriCheck V3 API:

```powershell
cd c:\xampp\htdocs\agricheck-v3\backend\src\AgriCheck.Api
dotnet run --urls http://0.0.0.0:5000
```

4. Build / run:

```powershell
cd c:\xampp\htdocs\agricheck-v3\apps\android-agritrack
.\gradlew.bat assembleDebug
```

Open the project in Android Studio for emulator or device deployment.

## Demo accounts

| Role | Email | Password |
|------|-------|----------|
| Driver | `driver@agricheck.local` | `Driver@12345` |
| Operator | `operator@agricheck.local` | `Operator@12345` |

## Firebase push (optional)

Add `app/google-services.json` from your Firebase project to enable FCM token registration. Without it the app still shows local notifications when new assignments appear via sync polling.

## Project structure

```
app/src/main/java/com/agricheck/agritrack/
├── data/          # API, models, DataStore, repositories
├── location/      # Foreground GPS tracking service
├── push/          # FCM + local notifications
├── ui/            # Compose screens, theme, navigation
└── MainActivity.kt
```

## vs Expo prototype

The Expo app at `apps/agritrack-mobile` remains a cross-platform prototype. **This native app is the recommended V3 mobile client** for Android production deployments.
