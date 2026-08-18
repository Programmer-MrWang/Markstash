package io.github.programmermrwang.markstash

import android.app.Application
import io.github.programmermrwang.markstash.core.model.LogLevel

class MarkstashApplication : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        container = AppContainer(this)
        container.logs.append(LogLevel.Info, "lifecycle", "Native Android local runtime started")
    }
}
