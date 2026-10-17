package com.agricheck.agritrack.data.api

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import retrofit2.HttpException
import java.net.UnknownHostException

private val apiErrorJson = Json { ignoreUnknownKeys = true }

private fun Throwable.isDnsOrConnectivityFailure(): Boolean {
    var current: Throwable? = this
    while (current != null) {
        if (current is UnknownHostException) return true
        val msg = current.message.orEmpty()
        if (msg.contains("Unable to resolve host", ignoreCase = true) ||
            msg.contains("No address associated with hostname", ignoreCase = true)
        ) {
            return true
        }
        current = current.cause
    }
    return false
}

private data class ParsedApiError(val code: String?, val message: String?)

fun Throwable.apiErrorMessage(fallback: String): String {
    if (isDnsOrConnectivityFailure()) {
        return "Cannot reach the AgriCheck server. Turn on mobile data or Wi‑Fi, then open Settings and confirm the API URL is https://agricheck-v3-production.up.railway.app"
    }
    if (this is HttpException) {
        when (code()) {
            502, 503, 504 -> return "Server is temporarily unavailable. Please try again in a moment."
            401 -> return "Session expired. Please sign in again."
        }
        parseHttpError(this)?.message?.let { return it }
    }
    val raw = message?.trim()
    if (!raw.isNullOrBlank() && !raw.matches(Regex("HTTP \\d+"))) {
        return raw
    }
    return fallback
}

fun Throwable.apiErrorCode(): String? {
    if (this !is HttpException) return null
    return parseHttpError(this)?.code
}

private fun parseHttpError(error: HttpException): ParsedApiError? {
    val raw = error.response()?.errorBody()?.string()?.trim()
    if (raw.isNullOrBlank()) return null

    return runCatching {
        val root = apiErrorJson.parseToJsonElement(raw).jsonObject
        val first = root["errors"]?.jsonArray?.firstOrNull()?.jsonObject
        val code = first?.get("code")?.jsonPrimitive?.content?.takeIf { it.isNotBlank() }
        val message = first?.get("message")?.jsonPrimitive?.content?.takeIf { it.isNotBlank() }
        if (code != null || message != null) {
            return ParsedApiError(code, message)
        }
        val title = root["title"]?.jsonPrimitive?.content?.takeIf { it.isNotBlank() }
        val rootMessage = root["message"]?.jsonPrimitive?.content?.takeIf { it.isNotBlank() }
        ParsedApiError(code = null, message = title ?: rootMessage)
    }.getOrNull()
}
