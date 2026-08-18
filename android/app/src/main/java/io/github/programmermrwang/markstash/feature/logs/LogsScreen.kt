package io.github.programmermrwang.markstash.feature.logs

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.DeleteSweep
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.pluralStringResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import io.github.programmermrwang.markstash.R
import io.github.programmermrwang.markstash.core.model.AppLogEntry
import io.github.programmermrwang.markstash.core.model.LogLevel
import io.github.programmermrwang.markstash.ui.screenPadding
import java.text.DateFormat
import java.util.Date

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
                    Text(
                        stringResource(R.string.logs_title),
                        fontSize = 30.sp,
                        fontWeight = FontWeight.Bold,
                    )
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
                    DateFormat.getTimeInstance(DateFormat.MEDIUM)
                        .format(Date(entry.timestampEpochMillis)),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    style = MaterialTheme.typography.labelSmall,
                )
            }
            Text(entry.message, fontFamily = FontFamily.Monospace)
        }
    }
}
