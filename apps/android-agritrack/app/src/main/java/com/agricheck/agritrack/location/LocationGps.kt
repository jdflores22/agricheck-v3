package com.agricheck.agritrack.location

import android.location.Location

/** Reject coarse network fixes for pickup / warehouse actions. */
const val MAX_ACCEPTABLE_LOCATION_ACCURACY_METERS = 40f

fun Location.meetsNavigationAccuracy(): Boolean {
    if (!hasAccuracy()) return true
    return accuracy <= MAX_ACCEPTABLE_LOCATION_ACCURACY_METERS
}
