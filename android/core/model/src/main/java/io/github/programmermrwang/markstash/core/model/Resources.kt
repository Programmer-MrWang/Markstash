package io.github.programmermrwang.markstash.core.model

import java.util.UUID
import java.util.Locale

/** Stable identifier serialized as an opaque, non-empty string on every Markstash platform. */
@JvmInline
value class ResourceId(val value: String) {
    init {
        require(value.isNotBlank()) { "ResourceId must not be blank" }
    }

    companion object {
        fun generate(): ResourceId = ResourceId(UUID.randomUUID().toString())
    }
}

enum class ResourceKind {
    Link,
    File,
    Note,
}

@ConsistentCopyVisibility
data class ResourceRecord private constructor(
    val id: ResourceId,
    val kind: ResourceKind,
    val title: String,
    val source: String?,
    val description: String?,
    val tags: List<String>,
    val isFavorite: Boolean,
    val createdAtEpochMillis: Long,
    val updatedAtEpochMillis: Long,
    val contentHash: String?,
) {
    init {
        require(title.isNotBlank()) { "Resource title must not be blank" }
        require(updatedAtEpochMillis >= createdAtEpochMillis) {
            "Resource update time must not precede its creation time"
        }
    }

    companion object {
        fun create(
            id: ResourceId,
            kind: ResourceKind,
            title: String,
            source: String? = null,
            description: String? = null,
            tags: Iterable<String> = emptyList(),
            isFavorite: Boolean = false,
            createdAtEpochMillis: Long,
            updatedAtEpochMillis: Long,
            contentHash: String? = null,
        ): ResourceRecord = ResourceRecord(
            id = id,
            kind = kind,
            title = title.trim(),
            source = source.normalizeOptionalText(),
            description = description.normalizeOptionalText(),
            tags = tags.normalizeTags(),
            isFavorite = isFavorite,
            createdAtEpochMillis = createdAtEpochMillis,
            updatedAtEpochMillis = updatedAtEpochMillis,
            contentHash = contentHash.normalizeOptionalText(),
        )

        private fun String?.normalizeOptionalText(): String? = this?.trim()?.takeIf(String::isNotEmpty)

        private fun Iterable<String>.normalizeTags(): List<String> {
            val normalized = ArrayList<String>()
            val seen = HashSet<String>()
            for (tag in this) {
                val trimmed = tag.trim()
                if (trimmed.isNotEmpty() && seen.add(trimmed.lowercase(Locale.ROOT))) {
                    normalized += trimmed
                }
            }
            return normalized
        }
    }
}

@ConsistentCopyVisibility
data class ResourceQuery private constructor(
    val text: String?,
    val kinds: Set<ResourceKind>,
    val tags: List<String>,
    val favoritesOnly: Boolean,
    val limit: Int,
    val offset: Int,
) {
    init {
        require(limit in 1..MaxLimit) { "Resource query limit must be between 1 and $MaxLimit" }
        require(offset >= 0) { "Resource query offset must not be negative" }
    }

    companion object {
        const val DefaultLimit = 100
        const val MaxLimit = 500

        fun create(
            text: String? = null,
            kinds: Iterable<ResourceKind> = emptyList(),
            tags: Iterable<String> = emptyList(),
            favoritesOnly: Boolean = false,
            limit: Int = DefaultLimit,
            offset: Int = 0,
        ): ResourceQuery = ResourceQuery(
            text = text.normalizeOptionalText(),
            kinds = kinds.toSet(),
            tags = tags.normalizeTags(),
            favoritesOnly = favoritesOnly,
            limit = limit,
            offset = offset,
        )

        private fun String?.normalizeOptionalText(): String? = this?.trim()?.takeIf(String::isNotEmpty)

        private fun Iterable<String>.normalizeTags(): List<String> {
            val normalized = ArrayList<String>()
            val seen = HashSet<String>()
            for (tag in this) {
                val trimmed = tag.trim()
                if (trimmed.isNotEmpty() && seen.add(trimmed.lowercase(Locale.ROOT))) {
                    normalized += trimmed
                }
            }
            return normalized
        }
    }
}

data class ResourceBatchWriteResult(
    val addedCount: Int,
    val updatedCount: Int,
)

interface ResourceRepository {
    suspend fun get(id: ResourceId): ResourceRecord?

    suspend fun list(query: ResourceQuery = ResourceQuery.create()): List<ResourceRecord>

    /**
     * Writes the complete batch atomically. With [overwriteExisting] false, an existing ID is a
     * conflict and no resource in the batch may be written.
     */
    suspend fun upsert(
        resources: Collection<ResourceRecord>,
        overwriteExisting: Boolean,
    ): ResourceBatchWriteResult

    suspend fun delete(id: ResourceId): Boolean
}
