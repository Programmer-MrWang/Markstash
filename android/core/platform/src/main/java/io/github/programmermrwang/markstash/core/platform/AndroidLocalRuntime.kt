package io.github.programmermrwang.markstash.core.platform

import io.github.programmermrwang.markstash.core.model.HealthRepository
import io.github.programmermrwang.markstash.core.model.RuntimeHealth
import io.github.programmermrwang.markstash.core.model.RuntimeHealthResult

/** Phone-local runtime boundary for resource, search, and import implementations. */
class AndroidLocalRuntime : HealthRepository {
    override suspend fun fetchHealth(): RuntimeHealthResult =
        RuntimeHealthResult.Success(
            RuntimeHealth(
                status = "local",
    version = "android",
                message = "Phone-local runtime",
            ),
        )
}
