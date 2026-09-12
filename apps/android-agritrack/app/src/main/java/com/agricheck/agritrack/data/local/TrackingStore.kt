package com.agricheck.agritrack.data.local

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map

private val Context.trackingDataStore: DataStore<Preferences> by preferencesDataStore(name = "agritrack_tracking")

class TrackingStore(private val context: Context) {
    private val activeContainerKey = stringPreferencesKey("active_container_uuid")
    private val knownAssignmentsKey = stringPreferencesKey("known_assignment_uuids")

    val activeContainerUuidFlow = context.trackingDataStore.data.map { it[activeContainerKey] }

    suspend fun getActiveContainerUuid(): String? =
        context.trackingDataStore.data.first()[activeContainerKey]

    suspend fun setActiveContainerUuid(uuid: String?) {
        context.trackingDataStore.edit { prefs ->
            if (uuid.isNullOrBlank()) prefs.remove(activeContainerKey) else prefs[activeContainerKey] = uuid
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
