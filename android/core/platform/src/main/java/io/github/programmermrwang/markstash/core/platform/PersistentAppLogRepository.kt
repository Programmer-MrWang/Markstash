package io.github.programmermrwang.markstash.core.platform

import android.content.Context
import io.github.programmermrwang.markstash.core.model.AppLogEntry
import io.github.programmermrwang.markstash.core.model.AppLogRepository
import io.github.programmermrwang.markstash.core.model.LogLevel
import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.io.DataInputStream
import java.io.DataOutputStream
import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream
import java.nio.file.AtomicMoveNotSupportedException
import java.nio.file.Files
import java.nio.file.StandardCopyOption
import java.util.concurrent.locks.ReentrantLock
import kotlin.concurrent.withLock
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class PersistentAppLogRepository internal constructor(
    logFile: File,
    private val capacity: Int = DefaultCapacity,
    private val clock: () -> Long = System::currentTimeMillis,
) : AppLogRepository {
    constructor(
        context: Context,
        capacity: Int = DefaultCapacity,
    ) : this(
        logFile = File(context.noBackupFilesDir, "diagnostics/app-logs-v1.bin"),
        capacity = capacity,
    )

    private val lock = ReentrantLock()
    private val store = AppLogFileStore(logFile, capacity)
    private val restoredEntries = store.read()
    private val mutableEntries = MutableStateFlow(restoredEntries)
    private var nextId = (restoredEntries.maxOfOrNull(AppLogEntry::id) ?: 0L) + 1L

    override val entries: StateFlow<List<AppLogEntry>> = mutableEntries.asStateFlow()

    init {
        require(capacity > 0) { "Log capacity must be greater than zero" }
    }

    override fun append(level: LogLevel, category: String, message: String) {
        lock.withLock {
            val entry = AppLogEntry(
                id = nextId++,
                timestampEpochMillis = clock(),
                level = level,
                category = category.take(MaxCategoryLength),
                message = message.take(MaxMessageLength),
            )
            val updated = (listOf(entry) + mutableEntries.value).take(capacity)
            if (store.write(updated)) {
                mutableEntries.value = updated
            }
        }
    }

    override fun clear() {
        lock.withLock {
            if (store.write(emptyList())) {
                mutableEntries.value = emptyList()
            }
        }
    }

    companion object {
        const val DefaultCapacity = 200
        private const val MaxCategoryLength = 128
        private const val MaxMessageLength = 16_384
    }
}

private class AppLogFileStore(
    private val file: File,
    private val capacity: Int,
) {
    init {
        require(capacity in 1..MaxStoredEntryCount) {
            "Log capacity must be between 1 and $MaxStoredEntryCount"
        }
    }

    fun read(): List<AppLogEntry> {
        if (!file.isFile || file.length() == 0L) return emptyList()

        return runCatching {
            DataInputStream(BufferedInputStream(FileInputStream(file))).use { input ->
                require(input.readInt() == Magic) { "Unexpected log file header" }
                require(input.readInt() == FormatVersion) { "Unsupported log file version" }
                val count = input.readInt()
                require(count in 0..MaxStoredEntryCount) { "Invalid log entry count" }

                buildList(count) {
                    repeat(count) {
                        add(
                            AppLogEntry(
                                id = input.readLong(),
                                timestampEpochMillis = input.readLong(),
                                level = LogLevel.valueOf(input.readString()),
                                category = input.readString(),
                                message = input.readString(),
                            ),
                        )
                    }
                }.take(capacity)
            }
        }.getOrElse {
            quarantineCorruptFile()
            emptyList()
        }
    }

    fun write(entries: List<AppLogEntry>): Boolean {
        file.parentFile?.mkdirs()
        val temporaryFile = File(file.parentFile, "${file.name}.tmp")

        return runCatching {
            FileOutputStream(temporaryFile).use { fileOutput ->
                val output = DataOutputStream(BufferedOutputStream(fileOutput))
                output.writeInt(Magic)
                output.writeInt(FormatVersion)
                output.writeInt(entries.size)
                entries.forEach { entry ->
                    output.writeLong(entry.id)
                    output.writeLong(entry.timestampEpochMillis)
                    output.writeString(entry.level.name)
                    output.writeString(entry.category)
                    output.writeString(entry.message)
                }
                output.flush()
                fileOutput.fd.sync()
            }
            replaceWith(temporaryFile)
            true
        }.onFailure {
            temporaryFile.delete()
        }.getOrDefault(false)
    }

    private fun replaceWith(temporaryFile: File) {
        try {
            Files.move(
                temporaryFile.toPath(),
                file.toPath(),
                StandardCopyOption.ATOMIC_MOVE,
                StandardCopyOption.REPLACE_EXISTING,
            )
        } catch (_: AtomicMoveNotSupportedException) {
            Files.move(
                temporaryFile.toPath(),
                file.toPath(),
                StandardCopyOption.REPLACE_EXISTING,
            )
        }
    }

    private fun quarantineCorruptFile() {
        val corruptFile = File(file.parentFile, "${file.name}.corrupt")
        runCatching {
            Files.move(
                file.toPath(),
                corruptFile.toPath(),
                StandardCopyOption.REPLACE_EXISTING,
            )
        }
    }

    private fun DataOutputStream.writeString(value: String) {
        val bytes = value.toByteArray(Charsets.UTF_8)
        require(bytes.size <= MaxEncodedStringBytes) { "Log value is too large" }
        writeInt(bytes.size)
        write(bytes)
    }

    private fun DataInputStream.readString(): String {
        val size = readInt()
        require(size in 0..MaxEncodedStringBytes) { "Invalid encoded log value size" }
        return ByteArray(size).also(::readFully).toString(Charsets.UTF_8)
    }

    private companion object {
        const val Magic = 0x4D534C47
        const val FormatVersion = 1
        const val MaxStoredEntryCount = 10_000
        const val MaxEncodedStringBytes = 64 * 1024
    }
}
