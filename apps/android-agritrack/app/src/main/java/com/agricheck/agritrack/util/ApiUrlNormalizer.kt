package com.agricheck.agritrack.util

import java.net.URI

object ApiUrlNormalizer {
    fun normalize(raw: String?): String? {
        if (raw.isNullOrBlank()) return null
        var value = raw.trim()
            .replace("\\", "")
            .trimEnd('/')
        if (value.isBlank()) return null
        if (!value.startsWith("http://", ignoreCase = true) &&
            !value.startsWith("https://", ignoreCase = true)
        ) {
            value = "https://$value"
        }
        return runCatching {
            val uri = URI(value)
            val host = uri.host?.trim()
            if (host.isNullOrBlank()) return null
            val scheme = uri.scheme?.lowercase() ?: "https"
            val port = uri.port
            if (port > 0) {
                "$scheme://$host:$port"
            } else {
                "$scheme://$host"
            }
        }.getOrNull()
    }
}
