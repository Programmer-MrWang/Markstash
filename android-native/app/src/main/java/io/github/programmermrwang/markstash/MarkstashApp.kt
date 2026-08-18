package io.github.programmermrwang.markstash

import androidx.activity.compose.BackHandler
import androidx.compose.animation.Crossfade
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.navigationBars
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Bookmarks
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.Search
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import io.github.programmermrwang.markstash.core.designsystem.MarkstashTheme
import io.github.programmermrwang.markstash.core.designsystem.glass.LiquidGlassBackdropScaffold
import io.github.programmermrwang.markstash.core.designsystem.glass.LiquidGlassNavigationBar
import io.github.programmermrwang.markstash.core.designsystem.glass.LiquidNavigationDestination
import top.yukonga.miuix.kmp.blur.Backdrop

private enum class MainDestination {
    Home,
    Library,
    Search,
    Settings,
}

@Composable
fun MarkstashApp(viewModel: MainViewModel) {
    val preferences by viewModel.preferences.collectAsStateWithLifecycle()
    val homeState by viewModel.homeState.collectAsStateWithLifecycle()
    val logs by viewModel.logs.collectAsStateWithLifecycle()
    var destinationName by rememberSaveable { androidx.compose.runtime.mutableStateOf(MainDestination.Home.name) }
    var showLogs by rememberSaveable { androidx.compose.runtime.mutableStateOf(false) }
    val destination = runCatching { MainDestination.valueOf(destinationName) }
        .getOrDefault(MainDestination.Home)

    BackHandler(enabled = showLogs || destination != MainDestination.Home) {
        if (showLogs) {
            showLogs = false
        } else {
            destinationName = MainDestination.Home.name
        }
    }

    MarkstashTheme(preference = preferences.theme) {
        Surface(color = MaterialTheme.colorScheme.background) {
            LiquidGlassBackdropScaffold(
                content = {
                    if (showLogs) {
                        LogsScreen(entries = logs, onClear = viewModel::clearLogs)
                    } else {
                        Crossfade(targetState = destination, label = "mainDestination") { target ->
                            when (target) {
                                MainDestination.Home -> HomeScreen(
                                    state = homeState,
                                    onRefresh = viewModel::refreshHealth,
                                )
                                MainDestination.Library -> LibraryScreen()
                                MainDestination.Search -> SearchScreen()
                                MainDestination.Settings -> SettingsScreen(
                                    preferences = preferences,
                                    logEntryCount = logs.size,
                                    onApiBaseUrlChanged = viewModel::setApiBaseUrl,
                                    onThemeChanged = viewModel::setTheme,
                                    onLiquidGlassChanged = viewModel::setLiquidGlassEnabled,
                                    onOpenLogs = { showLogs = true },
                                    onClearLogs = viewModel::clearLogs,
                                )
                            }
                        }
                    }
                },
                overlay = { backdrop ->
                    MainBottomBar(
                        destination = destination,
                        onDestinationChanged = {
                            showLogs = false
                            destinationName = it.name
                        },
                        backdrop = backdrop,
                        glassEnabled = preferences.liquidGlassEnabled,
                    )
                },
            )
        }
    }
}

@Composable
private fun BoxScope.MainBottomBar(
    destination: MainDestination,
    onDestinationChanged: (MainDestination) -> Unit,
    backdrop: Backdrop,
    glassEnabled: Boolean,
) {
    val density = LocalDensity.current
    val navigationBarBottom = with(density) {
        WindowInsets.navigationBars.getBottom(density).toDp()
    }
    val destinations = listOf(
        LiquidNavigationDestination(stringResource(R.string.nav_home), Icons.Outlined.Home),
        LiquidNavigationDestination(stringResource(R.string.nav_library), Icons.Outlined.Bookmarks),
        LiquidNavigationDestination(stringResource(R.string.nav_search), Icons.Outlined.Search),
        LiquidNavigationDestination(stringResource(R.string.nav_settings), Icons.Outlined.Settings),
    )
    LiquidGlassNavigationBar(
        destinations = destinations,
        selectedIndex = destination.ordinal,
        onSelected = { onDestinationChanged(MainDestination.entries[it]) },
        backdrop = backdrop,
        glassEnabled = glassEnabled,
        modifier = Modifier
            .align(Alignment.BottomCenter)
            .padding(horizontal = 20.dp)
            .padding(
                bottom = navigationBarBottom + 12.dp,
            ),
    )
}
