package com.agricheck.agritrack.ui.screens.auth

internal fun isStrongPassword(password: String): Boolean =
    password.length >= 8 &&
        password.any { it.isUpperCase() } &&
        password.any { it.isLowerCase() } &&
        password.any { it.isDigit() }

internal fun passwordHint(): String =
    "Password: at least 8 characters with upper, lower, and number (e.g. Driver@12345)."

internal fun isIsoDate(value: String): Boolean =
    value.matches(Regex("""\d{4}-\d{2}-\d{2}"""))
