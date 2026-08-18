package io.github.programmermrwang.markstash

import android.content.Context
import io.github.programmermrwang.markstash.core.model.AppLogRepository
import io.github.programmermrwang.markstash.core.model.AppPreferencesRepository
import io.github.programmermrwang.markstash.core.model.HealthRepository
import io.github.programmermrwang.markstash.core.platform.AndroidLocalRuntime
import io.github.programmermrwang.markstash.core.platform.DataStoreAppPreferencesRepository
import io.github.programmermrwang.markstash.core.platform.PersistentAppLogRepository

class AppContainer(context: Context) {
    val logs: AppLogRepository = PersistentAppLogRepository(context)
    val preferences: AppPreferencesRepository = DataStoreAppPreferencesRepository(
        context = context,
    )
    val health: HealthRepository = AndroidLocalRuntime()
}
