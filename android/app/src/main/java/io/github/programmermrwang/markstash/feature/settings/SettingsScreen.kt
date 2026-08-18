package io.github.programmermrwang.markstash.feature.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Article
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.DeleteSweep
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.pluralStringResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import io.github.programmermrwang.markstash.R
import io.github.programmermrwang.markstash.core.model.AppPreferences
import io.github.programmermrwang.markstash.core.model.ThemePreference
import io.github.programmermrwang.markstash.ui.screenPadding

@Composable
fun SettingsScreen(
    preferences: AppPreferences,
    logEntryCount: Int,
    onThemeChanged: (ThemePreference) -> Unit,
    onLiquidGlassChanged: (Boolean) -> Unit,
    onOpenLogs: () -> Unit,
    onClearLogs: () -> Unit,
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = screenPadding(),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        item {
            Text(
                stringResource(R.string.settings_title),
                fontSize = 30.sp,
                fontWeight = FontWeight.Bold,
            )
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
                            } else {
                                null
                            },
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
                    Text(
                        stringResource(R.string.settings_liquid_glass),
                        modifier = Modifier.weight(1f),
                    )
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
