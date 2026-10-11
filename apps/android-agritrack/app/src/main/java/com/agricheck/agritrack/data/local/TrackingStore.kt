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

data class PersistedNavigationSession(
    val containerUuid: String,
    val containerNumber: String,
    val destinationLabel: String,
)

private val Context.trackingDataStore: DataStore<Preferences> by preferencesDataStore(name = "agritrack_tracking")

class TrackingStore(private val context: Context) {
    private val activeContainerKey = stringPreferencesKey("active_container_uuid")
    private val knownAssignmentsKey = stringPreferencesKey("known_assignment_uuids")
    private val navDrivingKey = booleanPreferencesKey("nav_driving")
    private val navUuidKey = stringPreferencesKey("nav_container_uuid")
    private val navNumberKey = stringPreferencesKey("nav_container_number")
    private val navDestKey = stringPreferencesKey("nav_destination_label")

    val activeContainerUuidFlow = context.trackingDataStore.data.map { it[activeContainerKey] }

    val navigationSessionFlow = context.trackingDataStore.data.map { prefs ->
        val driving = prefs[navDrivingKey] == true
        val uuid = prefs[navUuidKey]
        if (!driving || uuid.isNullOrBlank()) return@map null
        PersistedNavigationSession(
            containerUuid = uuid,
            containerNumber = prefs[navNumberKey] ?: "",
            destinationLabel = prefs[navDestKey] ?: "",
        )
    }

    suspend fun getActiveContainerUuid(): String? =
        context.trackingDataStore.data.first()[activeContainerKey]

    suspend fun setActiveContainerUuid(uuid: String?) {
        context.trackingDataStore.edit { prefs ->
            if (uuid.isNullOrBlank()) prefs.remove(activeContainerKey) else prefs[activeContainerKey] = uuid
        }
    }

    suspend fun setNavigationSession(session: PersistedNavigationSession?) {
        context.trackingDataStore.edit { prefs ->
            if (session == null) {
                prefs.remove(navDrivingKey)
                prefs.remove(navUuidKey)
                prefs.remove(navNumberKey)
                prefs.remove(navDestKey)
            } else {
                prefs[navDrivingKey] = true
                prefs[navUuidKey] = session.containerUuid
                prefs[navNumberKey] = session.containerNumber
                prefs[navDestKey] = session.destinationLabel
            }
        }
    }

    suspend fun getNavigationSession(): PersistedNavigationSession? = navigationSessionFlow.first()

    suspend fun clearNavigationSession(containerUuid: String? = null) {
        context.trackingDataStore.edit { prefs ->
            if (containerUuid != null && prefs[navUuidKey] != containerUuid) return@edit
            prefs.remove(navDrivingKey)
            prefs.remove(navUuidKey)
            prefs.remove(navNumberKey)
            prefs.remove(navDestKey)
        }
    }

    suspend fun loadKnownAssignmentUuids(): Set<String> {
        val raw = context.trackingDataStore.data.first()[knownAssignmentsKey] ?: return emptySet()
        return raw.split(',').filter { it.isNotBlank() }.toSet()
    }

    suspend fun saveKnownAssignmentUuids(uuids: Set<String>) {
        context.trackingDataStore.edit { prefs ->
            prefs[knownAssignmentsKey] = uuids.joinToString(",")
        }
    }
}
