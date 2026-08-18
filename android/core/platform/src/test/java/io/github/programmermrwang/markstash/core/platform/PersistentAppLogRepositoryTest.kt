package io.github.programmermrwang.markstash.core.platform

import io.github.programmermrwang.markstash.core.model.LogLevel
import java.io.File
import java.util.concurrent.Executors
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder

class PersistentAppLogRepositoryTest {
    @get:Rule
    val temporaryFolder = TemporaryFolder()

    @Test
    fun entriesAreCappedAndRestoredInNewestFirstOrder() {
        val file = File(temporaryFolder.root, "logs.bin")
        var timestamp = 100L
        val repository = PersistentAppLogRepository(
            logFile = file,
            capacity = 2,
            clock = { timestamp++ },
        )

        repository.append(LogLevel.Info, "test", "one")
        repository.append(LogLevel.Warning, "test", "two")
        repository.append(LogLevel.Error, "test", "three")

        assertEquals(listOf("three", "two"), repository.entries.value.map { it.message })

        val restored = PersistentAppLogRepository(logFile = file, capacity = 2)
        assertEquals(repository.entries.value, restored.entries.value)
    }

    @Test
    fun clearIsPersisted() {
        val file = File(temporaryFolder.root, "logs.bin")
        val repository = PersistentAppLogRepository(logFile = file)
        repository.append(LogLevel.Info, "test", "one")

        repository.clear()

        assertTrue(repository.entries.value.isEmpty())
        assertTrue(PersistentAppLogRepository(logFile = file).entries.value.isEmpty())
    }

    @Test
    fun concurrentAppendsKeepUniqueIdsAndRespectCapacity() {
        val file = File(temporaryFolder.root, "logs.bin")
        val repository = PersistentAppLogRepository(logFile = file, capacity = 30)
        val executor = Executors.newFixedThreadPool(4)

        val futures = (1..60).map { index ->
            executor.submit {
                repository.append(LogLevel.Info, "worker", "entry-$index")
            }
        }
        futures.forEach { it.get() }
        executor.shutdown()

        val entries = repository.entries.value
        assertEquals(30, entries.size)
        assertEquals(entries.size, entries.map { it.id }.toSet().size)
        assertEquals(entries, PersistentAppLogRepository(logFile = file, capacity = 30).entries.value)
    }
}
