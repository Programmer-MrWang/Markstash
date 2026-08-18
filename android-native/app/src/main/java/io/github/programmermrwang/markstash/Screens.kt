package io.github.programmermrwang.markstash

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBars
import androidx.compose.foundation.layout.navigationBars
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Article
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.DeleteSweep
import androidx.compose.material.icons.outlined.Add
import androidx.compose.material.icons.outlined.Refresh
import androidx.compose.material.icons.outlined.Save
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.res.pluralStringResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import io.github.programmermrwang.markstash.core.model.AppLogEntry
import io.github.programmermrwang.markstash.core.model.AppPreferences
import io.github.programmermrwang.markstash.core.model.LogLevel
import io.github.programmermrwang.markstash.core.model.ThemePreference
import java.text.DateFormat
import java.util.Date

private val ScreenBottomInset = 116.dp

@Composable
fun HomeScreen(
    state: HomeUiState,
    onRefresh: () -> Unit,
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = screenPadding(),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        item {
            Text(stringResource(R.string.app_name), fontSize = 32.sp, fontWeight = FontWeight.Bold)
            Text(
                stringResource(R.string.platform_native_android),
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                style = MaterialTheme.typography.titleMedium,
            )
        }
        item {
            StatusCard(state = state, onRefresh = onRefresh)
        }
        item {
            Row(horizontalArrangement = Arrangement.spacedBy(14.dp)) {
                MetricCard(
                    title = stringResource(R.string.metric_saved),
                    value = stringResource(R.string.metric_saved_count, 0),
                    color = MaterialTheme.colorScheme.secondaryContainer,
                    modifier = Modifier.weight(1f),
                )
                MetricCard(
                    title = stringResource(R.string.metric_index),
                    value = stringResource(
                        if (state.health != null) R.string.status_ready else R.string.status_idle,
                    ),
                    color = MaterialTheme.colorScheme.tertiaryContainer,
                    modifier = Modifier.weight(1f),
                )
            }
        }
        item {
            Card(
                shape = RoundedCornerShape(8.dp),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.78f),
                ),
            ) {
                Column(Modifier.padding(18.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text(stringResource(R.string.section_runtime), fontWeight = FontWeight.SemiBold)
                    Text(
                        stringResource(R.string.runtime_stack),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }
    }
}

@Composable
private fun StatusCard(state: HomeUiState, onRefresh: () -> Unit) {
    Card(
        shape = RoundedCornerShape(8.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(18.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Box(
                Modifier
                    .size(10.dp)
                    .background(
                        when {
                            state.loading -> MaterialTheme.colorScheme.tertiary
                            state.health != null -> Color(0xFF2E9B68)
                            else -> MaterialTheme.colorScheme.error
                        },
                        CircleShape,
                    ),
            )
            Column(Modifier.weight(1f)) {
                Text(stringResource(R.string.api_health), fontWeight = FontWeight.SemiBold)
                Text(
                    when {
                        state.loading -> stringResource(R.string.status_checking)
                        state.health != null -> listOfNotNull(
                            state.health.status,
                            state.health.version,
                        ).joinToString(stringResource(R.string.health_status_separator))
                        else -> state.error ?: stringResource(R.string.status_unavailable)
                    },
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
            }
            if (state.loading) {
                CircularProgressIndicator(Modifier.size(24.dp), strokeWidth = 2.dp)
            } else {
                IconButton(onClick = onRefresh) {
                    Icon(
                        Icons.Outlined.Refresh,
                        contentDescription = stringResource(R.string.action_refresh),
                    )
                }
            }
        }
    }
}

@Composable
private fun MetricCard(title: String, value: String, color: Color, modifier: Modifier = Modifier) {
    Card(
        modifier = modifier,
        shape = RoundedCornerShape(8.dp),
        colors = CardDefaults.cardColors(containerColor = color),
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(title, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Text(value, fontSize = 26.sp, fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
fun LibraryScreen() {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = screenPadding(),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(stringResource(R.string.library_title), fontSize = 30.sp, fontWeight = FontWeight.Bold)
                    Text(
                        pluralStringResource(R.plurals.library_item_count, 0, 0),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                Button(onClick = {}) {
                    Icon(Icons.Outlined.Add, contentDescription = null)
                    Text(stringResource(R.string.action_new_item))
                }
            }
        }
        item {
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(260.dp),
                shape = RoundedCornerShape(8.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center,
                ) {
                    Text(stringResource(R.string.library_empty_title), fontWeight = FontWeight.SemiBold)
                    Text(
                        stringResource(R.string.library_empty_body),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }
    }
}

@Composable
fun SearchScreen() {
    var query by remember { mutableStateOf("") }
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = screenPadding(),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        item { Text(stringResource(R.string.search_title), fontSize = 30.sp, fontWeight = FontWeight.Bold) }
        item {
            OutlinedTextField(
                value = query,
                onValueChange = { query = it },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
                label = { Text(stringResource(R.string.search_hint)) },
            )
        }
        item {
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(220.dp),
                shape = RoundedCornerShape(8.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center,
                ) {
                    Text(
                        stringResource(
                            if (query.isBlank()) R.string.search_prompt else R.string.search_no_results,
                        ),
                        fontWeight = FontWeight.SemiBold,
                    )
                    Text(
                        stringResource(R.string.search_backend_note),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }
    }
}

@Composable
fun LogsScreen(entries: List<AppLogEntry>, onClear: () -> Unit) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = screenPadding(),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(stringResource(R.string.logs_title), fontSize = 30.sp, fontWeight = FontWeight.Bold)
                    Text(
                        pluralStringResource(
                            R.plurals.logs_entry_count,
                            entries.size,
                            entries.size,
                        ),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                IconButton(onClick = onClear, enabled = entries.isNotEmpty()) {
                    Icon(
                        Icons.Outlined.DeleteSweep,
                        contentDescription = stringResource(R.string.action_clear_logs),
                    )
                }
            }
        }
        if (entries.isEmpty()) {
            item {
                Card(shape = RoundedCornerShape(8.dp)) {
                    Text(stringResource(R.string.logs_empty), modifier = Modifier.padding(20.dp))
                }
            }
        } else {
            items(entries, key = { it.id }) { entry -> LogRow(entry) }
        }
    }
}

@Composable
private fun LogRow(entry: AppLogEntry) {
    Card(
        shape = RoundedCornerShape(8.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
    ) {
        Column(Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(
                    stringResource(
                        when (entry.level) {
                            LogLevel.Info -> R.string.log_level_info
                            LogLevel.Warning -> R.string.log_level_warning
                            LogLevel.Error -> R.string.log_level_error
                        },
                    ),
                    color = when (entry.level) {
                        LogLevel.Info -> MaterialTheme.colorScheme.primary
                        LogLevel.Warning -> MaterialTheme.colorScheme.tertiary
                        LogLevel.Error -> MaterialTheme.colorScheme.error
                    },
                    fontWeight = FontWeight.Bold,
                    style = MaterialTheme.typography.labelMedium,
                )
                Text(entry.category, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.weight(1f))
                Text(
                    DateFormat.getTimeInstance(DateFormat.MEDIUM).format(Date(entry.timestampEpochMillis)),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    style = MaterialTheme.typography.labelSmall,
                )
            }
            Text(entry.message, fontFamily = FontFamily.Monospace)
        }
    }
}

@Composable
fun SettingsScreen(
    preferences: AppPreferences,
    logEntryCount: Int,
    onApiBaseUrlChanged: (String) -> Unit,
    onThemeChanged: (ThemePreference) -> Unit,
    onLiquidGlassChanged: (Boolean) -> Unit,
    onOpenLogs: () -> Unit,
    onClearLogs: () -> Unit,
) {
    var endpoint by remember(preferences.apiBaseUrl) { mutableStateOf(preferences.apiBaseUrl) }
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = screenPadding(),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        item { Text(stringResource(R.string.settings_title), fontSize = 30.sp, fontWeight = FontWeight.Bold) }
        item {
            SettingsCard(title = stringResource(R.string.settings_api_endpoint)) {
                OutlinedTextField(
                    value = endpoint,
                    onValueChange = { endpoint = it },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    label = { Text(stringResource(R.string.settings_base_url)) },
                    trailingIcon = {
                        IconButton(onClick = { onApiBaseUrlChanged(endpoint) }) {
                            Icon(
                                Icons.Outlined.Save,
                                contentDescription = stringResource(R.string.action_save_endpoint),
                            )
                        }
                    },
                )
                Text(
                    stringResource(
                        R.string.api_health_endpoint_format,
                        endpoint.trimEnd('/'),
                    ),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    style = MaterialTheme.typography.bodySmall,
                )
            }
        }
        item {
            SettingsCard(title = stringResource(R.string.settings_theme)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    ThemePreference.entries.forEach { option ->
                        FilterChip(
                            selected = preferences.theme == option,
                            onClick = { onThemeChanged(option) },
                            label = {
                                Text(
                                    stringResource(
                                        when (option) {
                                            ThemePreference.System -> R.string.theme_system
                                            ThemePreference.Light -> R.string.theme_light
                                            ThemePreference.Dark -> R.string.theme_dark
                                        },
                                    ),
                                )
                            },
                            leadingIcon = if (preferences.theme == option) {
                                { Icon(Icons.Outlined.Check, contentDescription = null) }
                            } else null,
                        )
                    }
                }
            }
        }
        item {
            SettingsCard(title = stringResource(R.string.settings_appearance)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(stringResource(R.string.settings_liquid_glass), modifier = Modifier.weight(1f))
                    Switch(
                        checked = preferences.liquidGlassEnabled,
                        onCheckedChange = onLiquidGlassChanged,
                    )
                }
            }
        }
        item {
            SettingsCard(title = stringResource(R.string.settings_local_diagnostics)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(
                        pluralStringResource(
                            R.plurals.settings_log_entry_count,
                            logEntryCount,
                            logEntryCount,
                        ),
                        modifier = Modifier.weight(1f),
                    )
                    IconButton(onClick = onOpenLogs) {
                        Icon(
                            Icons.AutoMirrored.Outlined.Article,
                            contentDescription = stringResource(R.string.action_view_logs),
                        )
                    }
                    IconButton(onClick = onClearLogs, enabled = logEntryCount > 0) {
                        Icon(
                            Icons.Outlined.DeleteSweep,
                            contentDescription = stringResource(R.string.action_clear_logs),
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun SettingsCard(title: String, content: @Composable ColumnScope.() -> Unit) {
    Card(
        shape = RoundedCornerShape(8.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
    ) {
        Column(
            modifier = Modifier.padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Text(title, fontWeight = FontWeight.SemiBold)
            content()
        }
    }
}

@Composable
private fun screenPadding(): PaddingValues {
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
