package io.github.programmermrwang.markstash.core.model

import kotlinx.coroutines.flow.Flow
import kotlinx.serialization.Serializable

const val DefaultApiBaseUrl: String = "http://10.0.2.2:5080/"

enum class ThemePreference {
    System,
    Light,
    Dark,
}

data class AppPreferences(
    val apiBaseUrl: String = DefaultApiBaseUrl,
    val theme: ThemePreference = ThemePreference.System,
    val liquidGlassEnabled: Boolean = true,
)

interface AppPreferencesRepository {
    val preferences: Flow<AppPreferences>

    suspend fun setApiBaseUrl(value: String)

    suspend fun setTheme(value: ThemePreference)

    suspend fun setLiquidGlassEnabled(value: Boolean)
}

enum class LogLevel {
    Info,
    Warning,
    Error,
}

data class AppLogEntry(
    val id: Long,
    val timestampEpochMillis: Long,
    val level: LogLevel,
    val category: String,
    val message: String,
)

interface AppLogRepository {
    val entries: Flow<List<AppLogEntry>>

    fun append(level: LogLevel, category: String, message: String)

    fun clear()
}

@Serializable
data class ApiHealth(
    val status: String,
    val version: String? = null,
    val message: String? = null,
)

sealed interface ApiHealthResult {
    data class Success(val health: ApiHealth) : ApiHealthResult

    data class Failure(val message: String) : ApiHealthResult
}

interface HealthRepository {
    suspend fun fetchHealth(baseUrl: String): ApiHealthResult
}
