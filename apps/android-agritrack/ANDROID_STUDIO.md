# Build AgriTrack sa Android Studio

Ito ang **official** AgriCheck V3 mobile app para sa Android. Hindi `apps/agritrack-mobile` (Expo).

## Open project

1. Android Studio → **File → Open**
2. Piliin: `c:\xampp\htdocs\agricheck-v3\apps\android-agritrack`
3. Hintayin ang Gradle sync.

### Kung lumabas: *Incompatible Gradle JVM version* (Java 25)

Gradle **hindi** pwedeng tumakbo sa **Java 25** sa ngayon. Gawin ang isa:

1. Sa banner, piliin **Change Gradle JDK configuration** → **jbr-21** o **JDK 17** (Embedded JDK ng Android Studio).
2. O manual: **File → Settings** → **Build, Execution, Deployment → Build Tools → Gradle** → **Gradle JDK** → **jbr-21** (hindi *25*).
3. **File → Sync Project with Gradle Files**.

Kung wala sa listahan ang 17/21: **Download JDK…** → **17** (Temurin o Oracle).

Optional sa `gradle.properties` (project root):

```properties
org.gradle.java.home=C:\\Program Files\\Android\\Android Studio\\jbr
```

(I-adjust ang path kung iba ang install mo ng Android Studio.)

## `local.properties`

Copy `local.properties.example` → `local.properties` kung wala pa.

```properties
sdk.dir=...
API_BASE_URL=https://agricheck-v3-production.up.railway.app
GOOGLE_MAPS_API_KEY=your_key
```

Google Maps: tingnan `GOOGLE_MAPS_SETUP.md` sa folder na ito.

**Reference navigation test** (production API — `scripts/agritrack/reset-driver-delivery.ps1`):

| Stop | Coordinates |
|------|-------------|
| Pickup | 14.454427, 120.990610 |
| Warehouse | 14.459128, 120.987523 |

## Run sa phone

1. USB debugging on sa device.
2. Piliin ang device sa toolbar.
3. **Run** (green play) o **Build → Make Project** then Run.

Release APK: **Build → Build Bundle(s) / APK(s) → Build APK(s)**  
Output: `app/build/outputs/apk/...`

## Alamin kung latest build

Sa app: **Higit pa** → `Version 1.2.4 · Native Android` at status ng Google Maps.

Kung lumang UI pa: **Build → Clean Project**, **Rebuild Project**, uninstall old AgriTrack sa phone, Run ulit.

## Demo login

| Role | Email | Password |
|------|-------|----------|
| Driver | driver@agricheck.local | Driver@12345 |
| Operator | operator@agricheck.local | Operator@12345 |
