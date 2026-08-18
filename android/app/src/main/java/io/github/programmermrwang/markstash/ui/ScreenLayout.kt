package io.github.programmermrwang.markstash.ui

import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.navigationBars
import androidx.compose.foundation.layout.statusBars
import androidx.compose.runtime.Composable
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.dp

private val ScreenBottomInset = 116.dp

@Composable
internal fun screenPadding(): PaddingValues {
    val density = LocalDensity.current
    val top = with(density) { WindowInsets.statusBars.getTop(density).toDp() }
    val bottom = with(density) { WindowInsets.navigationBars.getBottom(density).toDp() }
    return PaddingValues(
        start = 20.dp,
        top = top + 20.dp,
        end = 20.dp,
        bottom = bottom + ScreenBottomInset,
    )
}
