package io.github.programmermrwang.markstash.core.platform

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStoreFile
import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import io.github.programmermrwang.markstash.core.model.AppPreferences
import io.github.programmermrwang.markstash.core.model.AppPreferencesRepository
import io.github.programmermrwang.markstash.core.model.DefaultApiBaseUrl
import io.github.programmermrwang.markstash.core.model.ThemePreference
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

class DataStoreAppPreferencesRepository(
    context: Context,
    private val defaultApiBaseUrl: String = DefaultApiBaseUrl,
) : AppPreferencesRepository {
    private val dataStore: DataStore<Preferences> = PreferenceDataStoreFactory.create(
        scope = CoroutineScope(SupervisorJob() + Dispatchers.IO),
        produceFile = { context.preferencesDataStoreFile("markstash.preferences_pb") },
    )

    override val preferences: Flow<AppPreferences> = dataStore.data.map { values ->
        AppPreferences(
            apiBaseUrl = values[ApiBaseUrlKey] ?: defaultApiBaseUrl,
            theme = values[ThemeKey]
                ?.let { stored -> ThemePreference.entries.firstOrNull { it.name == stored } }
                ?: ThemePreference.System,
            liquidGlassEnabled = values[LiquidGlassEnabledKey] ?: true,
        )
    }

    override suspend fun setApiBaseUrl(value: String) {
        dataStore.edit { it[ApiBaseUrlKey] = value.trim() }
    }

    override suspend fun setTheme(value: ThemePreference) {
        dataStore.edit { it[ThemeKey] = value.name }
    }

    override suspend fun setLiquidGlassEnabled(value: Boolean) {
        dataStore.edit { it[LiquidGlassEnabledKey] = value }
    }

    private companion object {
        val ApiBaseUrlKey = stringPreferencesKey("api_base_url")
        val ThemeKey = stringPreferencesKey("theme")
        val LiquidGlassEnabledKey = booleanPreferencesKey("liquid_glass_enabled")
    }
}
