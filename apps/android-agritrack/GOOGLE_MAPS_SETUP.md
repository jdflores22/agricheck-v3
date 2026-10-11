# AgriTrack (Native Android) — Google Maps setup

**Project path:** `C:\xampp\htdocs\agricheck-v3\apps\android-agritrack`  
**Open in Android Studio** (File → Open → this folder). Hindi ito Expo.

## 1. Google Cloud

1. [Google Cloud Console](https://console.cloud.google.com/) → project with billing.
2. Enable **Maps SDK for Android** and **Directions API**.

## 2. API key

**Credentials → Create API key**

- **Application restriction:** Android apps  
  - Package: `com.agricheck.agritrack`  
  - SHA-1: debug at release keystore (Android Studio: Gradle → `app` → `signingReport`, o `keytool` sa debug keystore).
- **API restriction:** Maps SDK for Android + Directions API.

## 3. `local.properties`

Sa folder `apps/android-agritrack` (kasama ng `local.properties.example`):

```properties
sdk.dir=C\:\\Users\\YourName\\AppData\\Local\\Android\\Sdk
API_BASE_URL=https://agricheck-v3-production.up.railway.app
GOOGLE_MAPS_API_KEY=AIzaSy...your_key
```

**Mahalaga:** Pagkatapos magdagdag o magpalit ng key, **Build → Rebuild Project** (hindi lang Run) para ma-embed sa APK.

## 4. Sa app (lahat sa loob — parang Grab)

1. Driver → **Delivery** → **Delivery map**.
2. **Default:** mapa sa loob ng app (OSM) + ruta/ETA/voice (Google Directions kung may key).
3. **Simulan ang navigation sa app** — hindi automatic na lalabas sa Google Maps app.
4. Opsyonal lang: text link **Buksan sa Google Maps app**.

Google **tiles** sa loob: `ENABLE_GOOGLE_MAP_TILES=true` sa `local.properties` + tamang SHA-1. Kung beige/blank, iwan na false (OSM map).

## 5. Logcat: `Authorization failure` (Maps SDK)

Ibig sabihin: may `GOOGLE_MAPS_API_KEY` at `ENABLE_GOOGLE_MAP_TILES=true`, pero **hindi pa naka-whitelist ang SHA-1** ng APK na naka-install.

**Mabilis na ayos (in-app map agad):** sa `local.properties`:

```properties
ENABLE_GOOGLE_MAP_TILES=false
```

**Rebuild** → mapa ay **OSM sa loob ng app**; **Directions + voice** gumagana pa rin gamit ang key.

**Para gumana ang Google tiles sa loob:**

1. Sa Android Studio: **Gradle** panel → `android-agritrack` → `app` → **Tasks** → `android` → **signingReport** (o terminal):

```powershell
cd C:\xampp\htdocs\agricheck-v3\apps\android-agritrack
.\gradlew.bat signingReport
```

2. Kopyahin ang **SHA-1** sa ilalim ng `Variant: debug` (o `release` kung release APK).
3. [Google Cloud Console](https://console.cloud.google.com/) → **Credentials** → iyong API key → **Application restrictions** → **Android apps** → **Add**:
   - Package: `com.agricheck.agritrack`
   - SHA-1: (mula sa signingReport — pareho sa hinihingi ng logcat, hal. nagtatapos sa `...:F4:F2:9E:C6`)
4. Siguraduhing naka-enable ang **Maps SDK for Android**.
5. Ilagay `ENABLE_GOOGLE_MAP_TILES=true`, **Rebuild Project**, install ulit.

Minsan tumatagal ang Google ng ilang minuto pagkatapos mag-save ng SHA-1.

## 6. Troubleshooting

| Symptom | Fix |
|--------|-----|
| Blank / beige map | `ENABLE_GOOGLE_MAP_TILES` = false (default) → OSM map; o ayusin SHA-1 kung gusto Google tiles |
| May voice/route, walang map | Rebuild **1.2.5+** — dati Google tiles walang SHA-1 |
| "Authorization failure" sa logcat | Package `com.agricheck.agritrack` + SHA-1 |

Sa **Higit pa**, tingnan ang status ng Google Directions / map tiles.
