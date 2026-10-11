# AgriTrack — Native Android (AgriCheck V3)

**Ito ang mobile app na gamitin mo.**  
Path: `apps/android-agritrack` → buksan sa **Android Studio** (hindi Expo, hindi `agritrack-mobile`).

| Doc | Purpose |
|-----|---------|
| [ANDROID_STUDIO.md](./ANDROID_STUDIO.md) | Paano i-open, build, at i-run sa phone |
| [GOOGLE_MAPS_SETUP.md](./GOOGLE_MAPS_SETUP.md) | Google Maps API key sa `local.properties` |

## Tech stack

| Layer | Technology |
|-------|------------|
| Language | Kotlin (JVM 17) |
| UI | Jetpack Compose + Material 3 |
| Maps | Google Maps (may `GOOGLE_MAPS_API_KEY`) · OSMDroid + OSRM fallback |
| GPS | Play Services + foreground service |

## Quick setup

1. `local.properties` — `sdk.dir`, optional `API_BASE_URL`, **`GOOGLE_MAPS_API_KEY`**
2. Android Studio → Open `apps/android-agritrack` → **Run**
3. Sa phone: **Higit pa** → dapat **`Bersyon 1.2.5 · Native Android`**

## Demo accounts

| Role | Email | Password |
|------|-------|----------|
| Driver | `driver@agricheck.local` | `Driver@12345` |
| Operator | `operator@agricheck.local` | `Operator@12345` |

## Maps flow (driver)

**Delivery** → **Delivery map** → **Simulan sa Google Maps** (turn-by-turn) o in-app **Drive** + **Delivery QR** sa warehouse.
