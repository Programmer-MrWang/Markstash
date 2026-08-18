package io.github.programmermrwang.markstash

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import io.github.programmermrwang.markstash.core.model.ApiHealth
import io.github.programmermrwang.markstash.core.model.ApiHealthResult
import io.github.programmermrwang.markstash.core.model.AppLogEntry
import io.github.programmermrwang.markstash.core.model.AppPreferences
import io.github.programmermrwang.markstash.core.model.LogLevel
import io.github.programmermrwang.markstash.core.model.ThemePreference
import io.github.programmermrwang.markstash.core.network.EndpointPolicy
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

data class HomeUiState(
    val loading: Boolean = false,
    val health: ApiHealth? = null,
    val error: String? = null,
)

class MainViewModel(
    private val container: AppContainer,
) : ViewModel() {
    val preferences: StateFlow<AppPreferences> = container.preferences.preferences.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5_000),
        initialValue = AppPreferences(apiBaseUrl = container.defaultApiBaseUrl),
    )

    val logs: StateFlow<List<AppLogEntry>> = container.logs.entries.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5_000),
        initialValue = emptyList(),
    )

    private val mutableHomeState = kotlinx.coroutines.flow.MutableStateFlow(HomeUiState())
    val homeState: StateFlow<HomeUiState> = mutableHomeState

    init {
        refreshHealth()
    }

    fun refreshHealth() {
        if (mutableHomeState.value.loading) return
        viewModelScope.launch {
            mutableHomeState.value = mutableHomeState.value.copy(loading = true, error = null)
            val endpoint = container.preferences.preferences.first().apiBaseUrl
            when (val result = container.health.fetchHealth(endpoint)) {
                is ApiHealthResult.Success -> {
                    mutableHomeState.value = HomeUiState(health = result.health)
                }
                is ApiHealthResult.Failure -> {
                    mutableHomeState.value = HomeUiState(error = result.message)
                }
            }
        }
    }

    fun setApiBaseUrl(value: String) {
        viewModelScope.launch {
            val normalized = EndpointPolicy.normalizeBaseUrl(value)
            container.preferences.setApiBaseUrl(normalized)
            container.logs.append(LogLevel.Info, "settings", "API endpoint changed to $normalized")
        }
    }

    fun setTheme(value: ThemePreference) {
        viewModelScope.launch { container.preferences.setTheme(value) }
    }

    fun setLiquidGlassEnabled(value: Boolean) {
        viewModelScope.launch { container.preferences.setLiquidGlassEnabled(value) }
    }

    fun clearLogs() = container.logs.clear()

    companion object {
        fun factory(container: AppContainer): ViewModelProvider.Factory =
            object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    require(modelClass.isAssignableFrom(MainViewModel::class.java))
                    return MainViewModel(container) as T
                }
            }
    }
}
