package io.github.programmermrwang.markstash

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import io.github.programmermrwang.markstash.core.model.AppLogEntry
import io.github.programmermrwang.markstash.core.model.AppLogRepository
import io.github.programmermrwang.markstash.core.model.AppPreferences
import io.github.programmermrwang.markstash.core.model.AppPreferencesRepository
import io.github.programmermrwang.markstash.core.model.HealthRepository
import io.github.programmermrwang.markstash.core.model.RuntimeHealthResult
import io.github.programmermrwang.markstash.core.model.ThemePreference
import io.github.programmermrwang.markstash.feature.home.HomeUiState
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class MainViewModel(
    private val preferencesRepository: AppPreferencesRepository,
    private val logRepository: AppLogRepository,
    private val healthRepository: HealthRepository,
) : ViewModel() {
    val preferences: StateFlow<AppPreferences> = preferencesRepository.preferences.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5_000),
        initialValue = AppPreferences(),
    )

    val logs: StateFlow<List<AppLogEntry>> = logRepository.entries.stateIn(
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
            when (val result = healthRepository.fetchHealth()) {
                is RuntimeHealthResult.Success -> {
                    mutableHomeState.value = HomeUiState(health = result.health)
                }
                is RuntimeHealthResult.Failure -> {
                    mutableHomeState.value = HomeUiState(error = result.message)
                }
            }
        }
    }

    fun setTheme(value: ThemePreference) {
        viewModelScope.launch { preferencesRepository.setTheme(value) }
    }

    fun setLiquidGlassEnabled(value: Boolean) {
        viewModelScope.launch { preferencesRepository.setLiquidGlassEnabled(value) }
    }

    fun clearLogs() = logRepository.clear()

    companion object {
        fun factory(
            preferencesRepository: AppPreferencesRepository,
            logRepository: AppLogRepository,
            healthRepository: HealthRepository,
        ): ViewModelProvider.Factory =
            object : ViewModelProvider.Factory {
                @Suppress("UNCHECKED_CAST")
                override fun <T : ViewModel> create(modelClass: Class<T>): T {
                    require(modelClass.isAssignableFrom(MainViewModel::class.java))
                    return MainViewModel(
                        preferencesRepository = preferencesRepository,
                        logRepository = logRepository,
                        healthRepository = healthRepository,
                    ) as T
                }
            }
    }
}
