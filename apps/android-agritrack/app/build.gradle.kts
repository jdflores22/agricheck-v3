import java.util.Properties

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("org.jetbrains.kotlin.plugin.compose")
    id("org.jetbrains.kotlin.plugin.serialization")
}

val localProperties = Properties().apply {
    val file = rootProject.file("local.properties")
    if (file.exists()) file.inputStream().use { load(it) }
}

val firebaseEnabled = file("google-services.json").exists()
if (firebaseEnabled) {
    apply(plugin = "com.google.gms.google-services")
}

android {
    namespace = "com.agricheck.agritrack"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.agricheck.agritrack"
        minSdk = 26
        targetSdk = 35
        versionCode = 46
        versionName = "1.2.43"

        val apiBaseUrl = localProperties.getProperty("API_BASE_URL") ?: "https://agricheck-v3-production.up.railway.app"
        val googleMapsApiKey = localProperties.getProperty("GOOGLE_MAPS_API_KEY") ?: ""
        val googleRoutingApiKey = localProperties.getProperty("GOOGLE_ROUTING_API_KEY") ?: ""
        val enableGoogleMapTiles =
            localProperties.getProperty("ENABLE_GOOGLE_MAP_TILES")?.equals("true", ignoreCase = true) == true
        buildConfigField("String", "API_BASE_URL", "\"$apiBaseUrl\"")
        buildConfigField("String", "GOOGLE_MAPS_API_KEY", "\"$googleMapsApiKey\"")
        buildConfigField("String", "GOOGLE_ROUTING_API_KEY", "\"$googleRoutingApiKey\"")
        buildConfigField("boolean", "ENABLE_GOOGLE_MAP_TILES", enableGoogleMapTiles.toString())
        buildConfigField("boolean", "FIREBASE_ENABLED", firebaseEnabled.toString())
        manifestPlaceholders["GOOGLE_MAPS_API_KEY"] = googleMapsApiKey.ifBlank { "PLACEHOLDER_SET_IN_local_properties" }
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            // Internal QA APK — replace with release keystore before Play Store upload.
            signingConfig = signingConfigs.getByName("debug")
        }
    }
}

tasks.register("exportAgriTrackReleaseApk") {
    dependsOn("assembleRelease")
    doLast {
        val version = android.defaultConfig.versionName ?: "0"
        val src = layout.buildDirectory.file("outputs/apk/release/app-release.apk").get().asFile
        if (!src.isFile) {
            throw GradleException("Release APK not found at ${src.absolutePath}")
        }
        val destDir = rootProject.layout.projectDirectory.dir("release-dist").asFile
        destDir.mkdirs()
        val dest = destDir.resolve("AgriTrack-$version-release.apk")
        src.copyTo(dest, overwrite = true)
        logger.lifecycle("AgriTrack release APK: ${dest.absolutePath}")
    }
}

dependencies {
    val composeBom = platform("androidx.compose:compose-bom:2024.12.01")
    implementation(composeBom)

    implementation("androidx.core:core-ktx:1.15.0")
    implementation("androidx.activity:activity-compose:1.9.3")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.7")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.8.7")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.8.7")
    implementation("androidx.navigation:navigation-compose:2.8.5")
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.material:material-icons-extended")
    implementation("androidx.datastore:datastore-preferences:1.1.1")
    implementation("androidx.exifinterface:exifinterface:1.3.7")

    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.7.3")
    implementation("com.jakewharton.retrofit:retrofit2-kotlinx-serialization-converter:1.0.0")

    implementation("com.google.android.gms:play-services-location:21.3.0")
    implementation("com.google.android.gms:play-services-maps:19.0.0")
    implementation("com.google.maps.android:maps-compose:6.2.1")
    implementation("org.osmdroid:osmdroid-android:6.1.20")

    val cameraxVersion = "1.4.1"
    implementation("androidx.camera:camera-camera2:$cameraxVersion")
    implementation("androidx.camera:camera-lifecycle:$cameraxVersion")
    implementation("androidx.camera:camera-view:$cameraxVersion")
    implementation("com.google.mlkit:barcode-scanning:17.3.0")
    implementation("io.coil-kt:coil-compose:2.7.0")

    implementation(platform("com.google.firebase:firebase-bom:33.7.0"))
    implementation("com.google.firebase:firebase-messaging-ktx")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-play-services:1.9.0")

    debugImplementation("androidx.compose.ui:ui-tooling")
}
