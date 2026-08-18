package io.github.programmermrwang.markstash

import android.content.Context
import io.github.programmermrwang.markstash.core.model.AppLogRepository
import io.github.programmermrwang.markstash.core.model.AppPreferencesRepository
import io.github.programmermrwang.markstash.core.model.HealthRepository
import io.github.programmermrwang.markstash.core.network.MarkstashHealthRepository
import io.github.programmermrwang.markstash.core.platform.DataStoreAppPreferencesRepository
import io.github.programmermrwang.markstash.core.platform.InMemoryAppLogRepository

class AppContainer(context: Context) {
    val defaultApiBaseUrl: String = BuildConfig.API_BASE_URL
    val logs: AppLogRepository = InMemoryAppLogRepository()
    val preferences: AppPreferencesRepository = DataStoreAppPreferencesRepository(
        context = context,
        defaultApiBaseUrl = defaultApiBaseUrl,
    )
    val health: HealthRepository = MarkstashHealthRepository(logs)
}
