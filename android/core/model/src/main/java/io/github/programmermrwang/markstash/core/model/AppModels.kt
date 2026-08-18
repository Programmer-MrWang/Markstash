package io.github.programmermrwang.markstash.core.model

import kotlinx.coroutines.flow.Flow

enum class ThemePreference {
    System,
    Light,
    Dark,
}

data class AppPreferences(
    val theme: ThemePreference = ThemePreference.System,
    val liquidGlassEnabled: Boolean = true,
)

interface AppPreferencesRepository {
    val preferences: Flow<AppPreferences>

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

data class RuntimeHealth(
    val status: String,
    val version: String? = null,
    val message: String? = null,
)

sealed interface RuntimeHealthResult {
    data class Success(val health: RuntimeHealth) : RuntimeHealthResult

    data class Failure(val message: String) : RuntimeHealthResult
}

interface HealthRepository {
    suspend fun fetchHealth(): RuntimeHealthResult
}
