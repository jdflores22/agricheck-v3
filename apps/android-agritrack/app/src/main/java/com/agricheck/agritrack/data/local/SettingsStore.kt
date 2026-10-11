package com.agricheck.agritrack.data.local

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map

private val Context.settingsDataStore: DataStore<Preferences> by preferencesDataStore(name = "agritrack_settings")

class SettingsStore(private val context: Context) {
    private val apiBaseUrlKey = stringPreferencesKey("api_base_url")
    private val permissionsOnboardingDoneKey = booleanPreferencesKey("permissions_onboarding_done")

    val apiBaseUrlFlow = context.settingsDataStore.data.map { it[apiBaseUrlKey] }

    suspend fun getApiBaseUrl(): String? = context.settingsDataStore.data.first()[apiBaseUrlKey]

    suspend fun isPermissionsOnboardingComplete(): Boolean =
        context.settingsDataStore.data.first()[permissionsOnboardingDoneKey] == true

    suspend fun setPermissionsOnboardingComplete(done: Boolean = true) {
        context.settingsDataStore.edit { prefs ->
            if (done) prefs[permissionsOnboardingDoneKey] = true else prefs.remove(permissionsOnboardingDoneKey)
        }
    }

    suspend fun setApiBaseUrl(url: String?) {
        context.settingsDataStore.edit { prefs ->
            if (url.isNullOrBlank()) {
                prefs.remove(apiBaseUrlKey)
            } else {
                prefs[apiBaseUrlKey] = url.trim().trimEnd('/')
            }
        }
    }
}
