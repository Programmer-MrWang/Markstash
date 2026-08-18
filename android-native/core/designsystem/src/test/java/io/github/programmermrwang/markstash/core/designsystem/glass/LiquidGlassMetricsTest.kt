package io.github.programmermrwang.markstash.core.designsystem.glass

import org.junit.Assert.assertEquals
import org.junit.Test

class LiquidGlassMetricsTest {
    @Test
    fun keepsBiliPaiParityValues() {
        assertEquals(64f, LiquidGlassMetrics.ShellHeight.value)
        assertEquals(56f, LiquidGlassMetrics.IndicatorHeight.value)
        assertEquals(4f, LiquidGlassMetrics.ShellBlurRadius.value)
        assertEquals(24f, LiquidGlassMetrics.ShellRefractionHeight.value)
        assertEquals(24f, LiquidGlassMetrics.ShellRefractionAmount.value)
        assertEquals(78f / 56f, LiquidGlassMetrics.PressedScale)
        assertEquals(0.5f, LiquidGlassMetrics.IndicatorChromaticAberration)
    }
}
