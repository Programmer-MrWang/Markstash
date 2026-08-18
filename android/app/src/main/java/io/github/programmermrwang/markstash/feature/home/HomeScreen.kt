package io.github.programmermrwang.markstash.feature.home

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Refresh
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import io.github.programmermrwang.markstash.R
import io.github.programmermrwang.markstash.core.model.RuntimeHealth
import io.github.programmermrwang.markstash.ui.screenPadding

data class HomeUiState(
    val loading: Boolean = false,
    val health: RuntimeHealth? = null,
    val error: String? = null,
)

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
                Text(stringResource(R.string.local_runtime_health), fontWeight = FontWeight.SemiBold)
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
