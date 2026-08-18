package io.github.programmermrwang.markstash.core.model

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotEquals
import org.junit.Test

class ResourceModelsTest {
    @Test
    fun generatedIdsAreCanonicalAndUnique() {
        val first = ResourceId.generate()
        val second = ResourceId.generate()

        assertEquals(first.value.lowercase(), first.value)
        assertNotEquals(first, second)
    }

    @Test(expected = IllegalArgumentException::class)
    fun queryRejectsUnboundedPageSize() {
        ResourceQuery.create(limit = ResourceQuery.MaxLimit + 1)
    }

    @Test(expected = IllegalArgumentException::class)
    fun recordRejectsUpdateBeforeCreation() {
        ResourceRecord.create(
            id = ResourceId.generate(),
            kind = ResourceKind.Note,
            title = "Example",
            createdAtEpochMillis = 2,
            updatedAtEpochMillis = 1,
        )
    }

    @Test
    fun recordNormalizesTextAndKeepsFirstTagCasingAndOrder() {
        val record = ResourceRecord.create(
            id = ResourceId.generate(),
            kind = ResourceKind.Note,
            title = "  Example  ",
            source = "  ",
            description = "  Description  ",
            tags = listOf("  Reading  ", "reading", "  Work"),
            createdAtEpochMillis = 1,
            updatedAtEpochMillis = 1,
        )

        assertEquals("Example", record.title)
        assertEquals(null, record.source)
        assertEquals("Description", record.description)
        assertEquals(listOf("Reading", "Work"), record.tags)
    }
}
