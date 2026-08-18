package io.github.programmermrwang.markstash.core.platform

import io.github.programmermrwang.markstash.core.model.AppLogEntry
import io.github.programmermrwang.markstash.core.model.AppLogRepository
import io.github.programmermrwang.markstash.core.model.LogLevel
import java.util.concurrent.atomic.AtomicLong
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

class InMemoryAppLogRepository(
    private val capacity: Int = 200,
) : AppLogRepository {
    private val nextId = AtomicLong(1)
    private val mutableEntries = MutableStateFlow<List<AppLogEntry>>(emptyList())

    override val entries: StateFlow<List<AppLogEntry>> = mutableEntries.asStateFlow()

    override fun append(level: LogLevel, category: String, message: String) {
        val entry = AppLogEntry(
            id = nextId.getAndIncrement(),
            timestampEpochMillis = System.currentTimeMillis(),
            level = level,
            category = category,
            message = message,
        )
        mutableEntries.update { current -> (listOf(entry) + current).take(capacity) }
    }

    override fun clear() {
        mutableEntries.value = emptyList()
    }
}
