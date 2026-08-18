package io.github.programmermrwang.markstash.core.network

import io.github.programmermrwang.markstash.core.model.DefaultApiBaseUrl
import org.junit.Assert.assertEquals
import org.junit.Test

class EndpointPolicyTest {
    @Test
    fun appendsRequiredTrailingSlash() {
        assertEquals("https://example.test/", EndpointPolicy.normalizeBaseUrl("https://example.test"))
    }

    @Test
    fun usesEmulatorDefaultForBlankValue() {
        assertEquals(DefaultApiBaseUrl, EndpointPolicy.normalizeBaseUrl("  "))
    }
}
