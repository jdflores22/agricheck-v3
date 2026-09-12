package com.agricheck.agritrack.data.local

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.agricheck.agritrack.data.model.AuthTokensDto
import com.agricheck.agritrack.data.model.UserSummaryDto
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "agritrack_auth")

class TokenStore(private val context: Context) {
    private val json = Json { ignoreUnknownKeys = true }

    private val accessTokenKey = stringPreferencesKey("access_token")
    private val refreshTokenKey = stringPreferencesKey("refresh_token")
    private val userKey = stringPreferencesKey("user")

    val authState: Flow<AuthState> = context.dataStore.data
        .map { prefs -> readAuthState(prefs) }
        .catch { emit(AuthState()) }

    suspend fun getAccessToken(): String? = context.dataStore.data.first()[accessTokenKey]

    suspend fun getRefreshToken(): String? = context.dataStore.data.first()[refreshTokenKey]

    suspend fun getCurrentUser(): UserSummaryDto? = context.dataStore.data.first()[userKey]?.let { raw ->
        runCatching { json.decodeFromString<UserSummaryDto>(raw) }.getOrNull()
    }

    suspend fun saveAuth(tokens: AuthTokensDto, user: UserSummaryDto) {
        context.dataStore.edit { prefs ->
            prefs[accessTokenKey] = tokens.accessToken
            prefs[refreshTokenKey] = tokens.refreshToken
            prefs[userKey] = json.encodeToString(user)
        }
    }

    suspend fun clear() {
        context.dataStore.edit { it.clear() }
    }

    private fun readAuthState(prefs: Preferences): AuthState {
        val accessToken = prefs[accessTokenKey]
        val refreshToken = prefs[refreshTokenKey]
        val user = prefs[userKey]?.let { raw ->
            runCatching { json.decodeFromString<UserSummaryDto>(raw) }.getOrNull()
        }
        if ((!accessToken.isNullOrBlank() || !refreshToken.isNullOrBlank()) && user == null) {
            return AuthState()
        }
        return AuthState(
            accessToken = accessToken,
            refreshToken = refreshToken,
            user = user,
        )
    }
}

data class AuthState(
    val accessToken: String? = null,
    val refreshToken: String? = null,
    val user: UserSummaryDto? = null,
) {
    val isLoggedIn: Boolean get() = !accessToken.isNullOrBlank() && user != null
}
