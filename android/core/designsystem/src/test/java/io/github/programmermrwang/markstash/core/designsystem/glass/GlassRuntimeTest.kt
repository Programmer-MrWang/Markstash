package io.github.programmermrwang.markstash.core.designsystem.glass

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class GlassRuntimeTest {
    @Test
    fun android12UsesCompatibilityRenderer() {
        assertFalse(supportsNativeLiquidGlass(31))
        assertFalse(supportsNativeLiquidGlass(32))
    }

    @Test
    fun android13AndNewerUseNativeLiquidGlass() {
        assertTrue(supportsNativeLiquidGlass(33))
        assertTrue(supportsNativeLiquidGlass(37))
    }
}
