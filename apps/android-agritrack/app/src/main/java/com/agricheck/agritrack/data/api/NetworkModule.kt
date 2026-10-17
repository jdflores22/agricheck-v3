package com.agricheck.agritrack.data.api

import com.agricheck.agritrack.BuildConfig
import com.agricheck.agritrack.data.local.TokenStore
import com.agricheck.agritrack.data.model.AuthResponseDto
import com.agricheck.agritrack.util.TokenExpiry
import kotlinx.coroutines.runBlocking
import kotlinx.serialization.json.Json
import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import com.jakewharton.retrofit2.converter.kotlinx.serialization.asConverterFactory
import java.util.concurrent.TimeUnit
import java.util.concurrent.locks.ReentrantLock
import kotlin.concurrent.withLock

class AuthInterceptor(
    private val tokenStore: TokenStore,
    private val baseUrl: String,
) : Interceptor {
    private val lock = ReentrantLock()
    private var refreshPermanentlyInvalid = false
    private val json = Json { ignoreUnknownKeys = true }

    fun resetSession() {
        lock.withLock { refreshPermanentlyInvalid = false }
    }

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        if (request.url.encodedPath.contains("/auth/")) {
            return chain.proceed(request)
        }

        val token = runBlocking { tokenStore.getAccessToken() }
        val authedRequest = if (!token.isNullOrBlank()) {
            val bearer = if (TokenExpiry.isJwtExpired(token)) refreshAccessToken() ?: token else token
            request.newBuilder().header("Authorization", "Bearer $bearer").build()
        } else {
            request
        }

        var response = chain.proceed(authedRequest)
        if (response.code == 401 && !request.url.encodedPath.contains("/auth/")) {
            response.close()
            val refreshed = refreshAccessToken()
            if (refreshed != null) {
                val retry = request.newBuilder().header("Authorization", "Bearer $refreshed").build()
                response = chain.proceed(retry)
            }
        }
        return response
    }

    /**
     * Renews the access token. Only clears stored credentials when the server rejects the refresh token.
     * Network errors leave the session intact so the user is not logged out while offline.
     */
    fun refreshAccessToken(): String? = lock.withLock {
        if (refreshPermanentlyInvalid) return null
        val refresh = runBlocking { tokenStore.getRefreshToken() } ?: return null

        val client = OkHttpClient.Builder()
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .build()
        val body = """{"refreshToken":"$refresh"}"""
        val refreshRequest = Request.Builder()
            .url("${baseUrl.trimEnd('/')}/api/v1/auth/refresh")
            .post(body.toRequestBody("application/json".toMediaType()))
            .build()

        return try {
            client.newCall(refreshRequest).execute().use { response ->
                val responseBody = response.body?.string() ?: ""
                if (!response.isSuccessful) {
                    if (shouldInvalidateSession(response.code, responseBody)) {
                        refreshPermanentlyInvalid = true
                        runBlocking { tokenStore.clear() }
                    }
                    return null
                }
                val envelope = json.decodeFromString<com.agricheck.agritrack.data.model.ApiEnvelope<AuthResponseDto>>(responseBody)
                val auth = envelope.data ?: return null
                runBlocking { tokenStore.saveAuth(auth.tokens, auth.user) }
                auth.tokens.accessToken
            }
        } catch (_: Exception) {
            null
        }
    }

    private fun shouldInvalidateSession(httpCode: Int, body: String): Boolean {
        if (httpCode != 400 && httpCode != 401 && httpCode != 403) return false
        val lower = body.lowercase()
        return lower.contains("invalid_refresh") ||
            lower.contains("refresh token is invalid") ||
            lower.contains("invalid or expired")
    }
}

data class NetworkServices(
    val api: AgriCheckApiService,
    val authInterceptor: AuthInterceptor,
    val baseUrl: String,
)

object NetworkModule {
    fun create(tokenStore: TokenStore, baseUrl: String): NetworkServices {
        val normalizedBase = if (baseUrl.endsWith("/")) baseUrl else "$baseUrl/"
        val authInterceptor = AuthInterceptor(tokenStore, normalizedBase)
        val logging = HttpLoggingInterceptor().apply {
            // HEADERS only: BODY logging consumes the response stream and breaks API error parsing.
            level = if (BuildConfig.DEBUG) {
                HttpLoggingInterceptor.Level.HEADERS
            } else {
                HttpLoggingInterceptor.Level.BASIC
            }
        }
        val client = OkHttpClient.Builder()
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(60, TimeUnit.SECONDS)
            .writeTimeout(60, TimeUnit.SECONDS)
            .addInterceptor(authInterceptor)
            .addInterceptor(logging)
            .build()

        val json = Json {
            ignoreUnknownKeys = true
            coerceInputValues = true
            isLenient = true
        }

        val api = Retrofit.Builder()
            .baseUrl(normalizedBase)
            .client(client)
            .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
            .build()
            .create(AgriCheckApiService::class.java)

        return NetworkServices(api, authInterceptor, normalizedBase.trimEnd('/'))
    }
}
