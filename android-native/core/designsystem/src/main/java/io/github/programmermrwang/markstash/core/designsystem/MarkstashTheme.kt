package io.github.programmermrwang.markstash.core.designsystem

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import io.github.programmermrwang.markstash.core.model.ThemePreference

private val LightColors = lightColorScheme(
    primary = Color(0xFF176B5B),
    onPrimary = Color.White,
    secondary = Color(0xFF8B4C67),
    tertiary = Color(0xFF48658B),
    background = Color(0xFFF5F7F8),
    surface = Color(0xFFFFFFFF),
    surfaceVariant = Color(0xFFE4EAEC),
    onSurface = Color(0xFF18201E),
    onSurfaceVariant = Color(0xFF53605D),
)

private val DarkColors = darkColorScheme(
    primary = Color(0xFF8BD6C2),
    onPrimary = Color(0xFF00382F),
    secondary = Color(0xFFFFB0CD),
    tertiary = Color(0xFFADC7F2),
    background = Color(0xFF111514),
    surface = Color(0xFF191E1D),
    surfaceVariant = Color(0xFF303735),
    onSurface = Color(0xFFE5ECE9),
    onSurfaceVariant = Color(0xFFB7C3BF),
)

@Composable
fun MarkstashTheme(
    preference: ThemePreference,
    content: @Composable () -> Unit,
) {
    val dark = when (preference) {
        ThemePreference.System -> isSystemInDarkTheme()
        ThemePreference.Light -> false
        ThemePreference.Dark -> true
    }
    MaterialTheme(
        colorScheme = if (dark) DarkColors else LightColors,
        content = content,
    )
}
