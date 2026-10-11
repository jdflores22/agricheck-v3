package com.agricheck.agritrack.util

import android.content.Context
import android.content.Intent
import android.net.Uri

fun Context.openGoogleMapsNavigation(
    destLat: Double,
    destLng: Double,
    originLat: Double? = null,
    originLng: Double? = null,
) {
    val navUri = Uri.parse("google.navigation:q=$destLat,$destLng&mode=d")
    val navIntent = Intent(Intent.ACTION_VIEW, navUri).apply {
        setPackage("com.google.android.apps.maps")
    }
    if (navIntent.resolveActivity(packageManager) != null) {
        startActivity(navIntent)
        return
    }

    val web =
        "https://www.google.com/maps/dir/?api=1&travelmode=driving" +
            "&destination=$destLat,$destLng" +
            if (originLat != null && originLng != null) "&origin=$originLat,$originLng" else ""
    startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(web)))
}
