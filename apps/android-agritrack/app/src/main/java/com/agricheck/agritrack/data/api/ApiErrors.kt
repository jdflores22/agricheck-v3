package com.agricheck.agritrack.data.api

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import retrofit2.HttpException

private val apiErrorJson = Json { ignoreUnknownKeys = true }

fun Throwable.apiErrorMessage(fallback: String): String {
    if (this is HttpException) {
        parseHttpErrorBody(this)?.let { return it }
    }
    val raw = message?.trim()
    if (!raw.isNullOrBlank() && !raw.matches(Regex("HTTP \\d+"))) {
        return raw
    }
    return fallback
}

private fun parseHttpErrorBody(error: HttpException): String? {
    val raw = error.response()?.errorBody()?.string()?.trim()
    if (raw.isNullOrBlank()) return null

    return runCatching {
        val root = apiErrorJson.parseToJsonElement(raw).jsonObject
        root["errors"]?.jsonArray
            ?.firstOrNull()
            ?.jsonObject
            ?.get("message")
            ?.jsonPrimitive
            ?.content
            ?.takeIf { it.isNotBlank() }
    }.getOrNull()
        ?: runCatching {
            val root = apiErrorJson.parseToJsonElement(raw).jsonObject
            root["errors"]?.jsonObject
                ?.values
                ?.firstOrNull()
                ?.jsonArray
                ?.firstOrNull()
                ?.jsonPrimitive
                ?.content
                ?.takeIf { it.isNotBlank() }
        }.getOrNull()
        ?: runCatching {
            apiErrorJson.parseToJsonElement(raw).jsonObject["title"]?.jsonPrimitive?.content
        }.getOrNull()
}
