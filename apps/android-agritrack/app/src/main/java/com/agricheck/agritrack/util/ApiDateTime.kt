package com.agricheck.agritrack.util

import java.time.Instant
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeFormatterBuilder
import java.time.temporal.ChronoField

/** Parses API date strings (.NET often omits the `Z` suffix). */
fun parseApiInstant(raw: String?): Instant? {
    if (raw.isNullOrBlank()) return null
    val trimmed = raw.trim()
    runCatching { return Instant.parse(trimmed) }
    runCatching { return OffsetDateTime.parse(trimmed).toInstant() }
    runCatching {
        if (trimmed.endsWith('Z', ignoreCase = true) || trimmed.contains('+')) {
            return Instant.parse(trimmed)
        }
        return Instant.parse("${trimmed}Z")
    }
    val flexible = DateTimeFormatterBuilder()
        .appendPattern("yyyy-MM-dd'T'HH:mm:ss")
        .optionalStart()
        .appendFraction(ChronoField.NANO_OF_SECOND, 0, 9, true)
        .optionalEnd()
        .toFormatter()
    runCatching {
        return LocalDateTime.parse(trimmed, flexible).toInstant(ZoneOffset.UTC)
    }
    return null
}

fun formatApiInstantForDisplay(
    raw: String?,
    zone: java.time.ZoneId,
    formatter: DateTimeFormatter,
): String? {
    val instant = parseApiInstant(raw) ?: return null
    return formatter.withZone(zone).format(instant)
}
