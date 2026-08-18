package io.github.programmermrwang.markstash.core.network

import io.github.programmermrwang.markstash.core.model.DefaultApiBaseUrl

object EndpointPolicy {
    fun normalizeBaseUrl(value: String): String {
        val trimmed = value.trim().ifEmpty { DefaultApiBaseUrl }
        return if (trimmed.endsWith('/')) trimmed else "$trimmed/"
    }
}
