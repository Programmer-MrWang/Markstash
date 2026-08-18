/*
 * Floating Backdrop dock adapted from BiliPai's FloatingBottomBar.
 * The page capture, hidden active-content capture, combined indicator Backdrop,
 * lens shader, and damped drag mechanics remain GPL-3.0-only.
* See android/THIRD_PARTY_NOTICES.md.
 */
package io.github.programmermrwang.markstash.core.designsystem.glass

import android.os.Build
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.EaseOut
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.compositionLocalOf
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.dropShadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.GraphicsLayerScope
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.shadow.Shadow
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.LayoutCoordinates
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.clearAndSetSemantics
import androidx.compose.ui.semantics.selected
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.compose.ui.util.fastCoerceIn
import androidx.compose.ui.util.fastRoundToInt
import androidx.compose.ui.util.lerp
import kotlin.math.abs
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.math.sign
import kotlin.math.sqrt
import kotlinx.coroutines.launch
import top.yukonga.miuix.kmp.blur.Backdrop
import top.yukonga.miuix.kmp.blur.blur
import top.yukonga.miuix.kmp.blur.drawBackdrop
import top.yukonga.miuix.kmp.blur.highlight.BloomStroke
import top.yukonga.miuix.kmp.blur.highlight.Highlight
import top.yukonga.miuix.kmp.blur.highlight.LightPosition
import top.yukonga.miuix.kmp.blur.highlight.LightSource
import top.yukonga.miuix.kmp.blur.layerBackdrop
import top.yukonga.miuix.kmp.blur.rememberLayerBackdrop
import top.yukonga.miuix.kmp.blur.sensor.rememberDeviceTilt

@Immutable
data class LiquidNavigationDestination(
    val label: String,
    val icon: ImageVector,
)

private val LocalNavigationContentColor = compositionLocalOf { Color.Unspecified }
private val LocalNavigationContentScale = compositionLocalOf<() -> Float> { { 1f } }

/** Placeholder backdrop used only by the Android 12 compatibility renderer. */
private object CompatibilityBackdrop : Backdrop {
    override val isCoordinatesDependent: Boolean = false
    override val offsetResidualX: Float = 0f
    override val offsetResidualY: Float = 0f

    override fun DrawScope.drawBackdrop(
        density: Density,
        coordinates: LayoutCoordinates?,
        layerBlock: (GraphicsLayerScope.() -> Unit)?,
        downscaleFactor: Int,
    ) = Unit
}

private val IndicatorSpecular = Highlight(
    width = 1.dp,
    alpha = 0.75f,
    style = BloomStroke(
        color = Color.White.copy(alpha = 0.12f),
        innerBlurRadius = 2.dp,
        primaryLight = LightSource(
            position = LightPosition(0.5f, -0.3f, -0.05f),
            color = Color.White,
            intensity = 1f,
        ),
        secondaryLight = LightSource(
            position = LightPosition(0.5f, 0.8f, -0.5f),
            color = Color.White,
            intensity = 0.4f,
        ),
        dualPeak = true,
    ),
)

private const val LightReferenceX = 0.5f
private const val LightReferenceY = 0.7f
private const val GravityDirectionThresholdSquared = 0.01f

@Composable
private fun rememberGravityRotatedHighlight(extraDegrees: Float): Highlight {
    val base = IndicatorSpecular
    val style = base.style as BloomStroke
    val tilt by rememberDeviceTilt()
    val primary = remember(tilt, style.primaryLight, extraDegrees) {
        val gravityMagnitudeSquared =
            tilt.gravityX * tilt.gravityX + tilt.gravityY * tilt.gravityY
        val (lightX, lightY) = if (gravityMagnitudeSquared > GravityDirectionThresholdSquared) {
            val inverseMagnitude = 1f / sqrt(gravityMagnitudeSquared)
            tilt.gravityX * inverseMagnitude to tilt.gravityY * inverseMagnitude
        } else {
            0f to -1f
        }
        val radians = extraDegrees * PI / 180.0
        val cosine = cos(radians).toFloat()
        val sine = sin(radians).toFloat()
        val rotatedX = cosine * lightX - sine * lightY
        val rotatedY = sine * lightX + cosine * lightY
        style.primaryLight.copy(
            position = LightPosition(
                LightReferenceX + rotatedX,
                LightReferenceY + rotatedY,
                style.primaryLight.position.z,
            ),
        )
    }
    return remember(base, primary) {
        base.copy(style = style.copy(primaryLight = primary))
    }
}

@Composable
fun LiquidGlassBackdropScaffold(
    modifier: Modifier = Modifier,
    content: @Composable BoxScope.() -> Unit,
    overlay: @Composable BoxScope.(Backdrop) -> Unit,
) {
    if (!supportsNativeLiquidGlass(Build.VERSION.SDK_INT)) {
        Box(modifier = modifier.fillMaxSize()) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(MaterialTheme.colorScheme.background),
                content = content,
            )
            overlay(CompatibilityBackdrop)
        }
        return
    }

    val backdrop = rememberLayerBackdrop()
    Box(modifier = modifier.fillMaxSize()) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .layerBackdrop(backdrop)
                .background(MaterialTheme.colorScheme.background),
            content = content,
        )
        overlay(backdrop)
    }
}

@Composable
fun LiquidGlassNavigationBar(
    destinations: List<LiquidNavigationDestination>,
    selectedIndex: Int,
    onSelected: (Int) -> Unit,
    backdrop: Backdrop,
    modifier: Modifier = Modifier,
    glassEnabled: Boolean = true,
) {
    if (destinations.isEmpty()) return

    if (!supportsNativeLiquidGlass(Build.VERSION.SDK_INT)) {
        CompatibleNavigationBar(
            destinations = destinations,
            selectedIndex = selectedIndex,
            onSelected = onSelected,
            modifier = modifier,
            glassEnabled = glassEnabled,
        )
        return
    }

    val count = destinations.size
    val maxIndex = count - 1
    val safeSelectedIndex = selectedIndex.coerceIn(0, maxIndex)
    val density = LocalDensity.current
    val isLtr = LocalLayoutDirection.current == LayoutDirection.Ltr
    val scope = rememberCoroutineScope()
    val tabsBackdrop = rememberLayerBackdrop()
    val combinedBackdrop = rememberCombinedBackdrop(backdrop, tabsBackdrop)
    val currentOnSelected by rememberUpdatedState(onSelected)
    var tabWidthPx by remember { mutableFloatStateOf(0f) }
    var totalWidthPx by remember { mutableFloatStateOf(0f) }
    val offsetAnimation = remember { Animatable(0f) }
    val rubberBandPx = with(density) { 4.dp.toPx() }
    val panelOffset by remember(rubberBandPx) {
        derivedStateOf {
            if (totalWidthPx <= 0f) {
                0f
            } else {
                val fraction = (offsetAnimation.value / totalWidthPx).fastCoerceIn(-1f, 1f)
                rubberBandPx * fraction.sign * EaseOut.transform(abs(fraction))
            }
        }
    }

    class Holder {
        var animation: DampedDragAnimation? = null
    }

    val holder = remember { Holder() }
    val drag = remember(scope, count, density, isLtr) {
        DampedDragAnimation(
            animationScope = scope,
            initialValue = safeSelectedIndex.toFloat(),
            valueRange = 0f..maxIndex.toFloat(),
            visibilityThreshold = 0.001f,
            initialScale = 1f,
            pressedScale = LiquidGlassMetrics.PressedScale,
            canDrag = { position ->
                val animation = holder.animation ?: return@DampedDragAnimation true
                if (tabWidthPx <= 0f) return@DampedDragAnimation false
                val indicatorX = animation.value * tabWidthPx
                val padding = with(density) { 4.dp.toPx() }
                val globalTouchX = if (isLtr) {
                    padding + indicatorX + position.x
                } else {
                    totalWidthPx - padding - tabWidthPx - indicatorX + position.x
                }
                globalTouchX in 0f..totalWidthPx
            },
            onDragStopped = {
                val target = targetValue.fastRoundToInt().fastCoerceIn(0, maxIndex)
                animateToValue(target.toFloat())
                currentOnSelected(target)
                scope.launch {
                    offsetAnimation.animateTo(0f, spring(1f, 300f, 0.5f))
                }
            },
            onDrag = { _, delta ->
                if (tabWidthPx > 0f) {
                    val direction = if (isLtr) 1f else -1f
                    updateValue(targetValue + delta.x / tabWidthPx * direction)
                    scope.launch {
                        offsetAnimation.snapTo(offsetAnimation.value + delta.x)
                    }
                }
            },
        ).also { holder.animation = it }
    }

    LaunchedEffect(safeSelectedIndex) {
        if (!drag.isDragging && abs(drag.targetValue - safeSelectedIndex) > 0.001f) {
            drag.animateToValue(safeSelectedIndex.toFloat())
        }
    }

    val isDark = isSystemInDarkTheme()
    val shellColor = MaterialTheme.colorScheme.surface.copy(alpha = 0.40f)
    val activeColor = MaterialTheme.colorScheme.primary
    val inactiveColor = MaterialTheme.colorScheme.onSurfaceVariant
    val idleIndicatorColor = if (isDark) {
        Color.White.copy(alpha = 0.10f)
    } else {
        Color.Black.copy(alpha = 0.10f)
    }
    val baseHighlight = rememberGravityRotatedHighlight(extraDegrees = -45f)
    val indicatorHighlight = rememberGravityRotatedHighlight(extraDegrees = 90f)
    val interactiveHighlight = if (glassEnabled && Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
        remember(scope, tabWidthPx) {
            InteractiveHighlight(
                animationScope = scope,
                position = { size: Size, _ ->
                    Offset(
                        x = if (isLtr) {
                            (drag.value + 0.5f) * tabWidthPx + panelOffset
                        } else {
                            size.width - (drag.value + 0.5f) * tabWidthPx + panelOffset
                        },
                        y = size.height / 2f,
                    )
                },
            )
        }
    } else {
        null
    }

    Box(
        modifier = modifier
            .widthIn(max = 520.dp)
            .fillMaxWidth(),
        contentAlignment = Alignment.CenterStart,
    ) {
        CompositionLocalProvider(LocalNavigationContentColor provides inactiveColor) {
            NavigationRow(
                destinations = destinations,
                selectedIndex = safeSelectedIndex,
                onSelected = { index ->
                    drag.animateToValue(index.toFloat())
                    currentOnSelected(index)
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .onGloballyPositioned { coordinates ->
                        totalWidthPx = coordinates.size.width.toFloat()
                        val inner = totalWidthPx - with(density) { 8.dp.toPx() }
                        tabWidthPx = (inner / count).coerceAtLeast(0f)
                    }
                    .graphicsLayer { translationX = panelOffset }
                    .dropShadow(
                        shape = CircleShape,
                        shadow = Shadow(
                            radius = 10.dp,
                            color = Color.Black,
                            alpha = if (isDark) 0.20f else 0.10f,
                        ),
                    )
                    .then(
                        if (glassEnabled) {
                            Modifier.drawBackdrop(
                                backdrop = backdrop,
                                shape = { CircleShape },
                                effects = {
                                    markstashVibrancy()
                                    blur(
                                        LiquidGlassMetrics.ShellBlurRadius.toPx(),
                                        LiquidGlassMetrics.ShellBlurRadius.toPx(),
                                    )
                                    liquidLens(
                                        LiquidGlassMetrics.ShellRefractionHeight.toPx(),
                                        LiquidGlassMetrics.ShellRefractionAmount.toPx(),
                                    )
                                },
                                highlight = { baseHighlight.copy(alpha = 0.75f) },
                                layerBlock = {
                                    val width = size.width.coerceAtLeast(1f)
                                    val scale = lerp(
                                        1f,
                                        1f + 16.dp.toPx() / width,
                                        drag.pressProgress,
                                    )
                                    scaleX = scale
                                    scaleY = scale
                                },
                                onDrawSurface = { drawRect(shellColor) },
                            )
                        } else {
                            Modifier.background(MaterialTheme.colorScheme.surface, CircleShape)
                        },
                    )
                    .then(
                        if (glassEnabled) {
                            interactiveHighlight?.modifier ?: Modifier
                        } else {
                            Modifier
                        },
                    )
                    .height(LiquidGlassMetrics.ShellHeight)
                    .padding(4.dp),
            )
        }

        if (glassEnabled) {
            CompositionLocalProvider(
                LocalNavigationContentColor provides activeColor,
                LocalNavigationContentScale provides {
                    lerp(1f, 1.2f, drag.pressProgress)
                },
            ) {
                NavigationRow(
                    destinations = destinations,
                    selectedIndex = safeSelectedIndex,
                    onSelected = currentOnSelected,
                    modifier = Modifier
                        .fillMaxWidth()
                        .clearAndSetSemantics {}
                        .alpha(0f)
                        .layerBackdrop(tabsBackdrop)
                        .graphicsLayer { translationX = panelOffset }
                        .drawBackdrop(
                            backdrop = backdrop,
                            shape = { CircleShape },
                            effects = {
                                markstashVibrancy()
                                blur(4.dp.toPx(), 4.dp.toPx())
                                liquidLens(24.dp.toPx(), 24.dp.toPx())
                            },
                            onDrawSurface = { drawRect(shellColor) },
                        )
                        .then(interactiveHighlight?.modifier ?: Modifier)
                        .height(LiquidGlassMetrics.IndicatorHeight)
                        .padding(horizontal = 4.dp),
                )
            }
        }

        if (tabWidthPx > 0f) {
            val tabWidth = with(density) { tabWidthPx.toDp() }
            val progressOffset = drag.value * tabWidthPx
            Box(
                modifier = Modifier
                    .padding(horizontal = 4.dp)
                    .graphicsLayer {
                        translationX = if (isLtr) {
                            progressOffset + panelOffset
                        } else {
                            -progressOffset + panelOffset
                        }
                    }
                    .then(interactiveHighlight?.gestureModifier ?: Modifier)
                    .then(drag.modifier)
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        role = Role.Tab,
                        onClick = { currentOnSelected(drag.targetValue.fastRoundToInt()) },
                    )
                    .clearAndSetSemantics {}
                    .then(
                        if (glassEnabled) {
                            Modifier.drawBackdrop(
                                backdrop = combinedBackdrop,
                                shape = { CircleShape },
                                effects = {
                                    val press = drag.pressProgress
                                    liquidLens(
                                        refractionHeight = LiquidGlassMetrics
                                            .IndicatorRefractionHeight.toPx() * press,
                                        refractionAmount = LiquidGlassMetrics
                                            .IndicatorRefractionAmount.toPx() * press,
                                        depthEffect = true,
                                        chromaticAberration =
                                            LiquidGlassMetrics.IndicatorChromaticAberration,
                                    )
                                },
                                highlight = {
                                    indicatorHighlight.copy(alpha = drag.pressProgress)
                                },
                                layerBlock = {
                                    scaleX = drag.scaleX
                                    scaleY = drag.scaleY
                                    val velocity = drag.velocity / 10f
                                    scaleX /= 1f - (velocity * 0.75f).fastCoerceIn(-0.2f, 0.2f)
                                    scaleY *= 1f - (velocity * 0.25f).fastCoerceIn(-0.2f, 0.2f)
                                },
                                onDrawSurface = {
                                    val press = drag.pressProgress
                                    drawRect(
                                        color = idleIndicatorColor,
                                        alpha = 1f - press,
                                    )
                                    drawRect(Color.Black.copy(alpha = 0.03f * press))
                                },
                            )
                        } else {
                            Modifier.background(activeColor.copy(alpha = 0.16f), CircleShape)
                        },
                    )
                    .then(interactiveHighlight?.modifier ?: Modifier)
                    .innerShadow(CircleShape) {
                        InnerShadow(
                            radius = 8.dp * drag.pressProgress,
                            color = Color.Black.copy(alpha = 0.15f),
                            alpha = drag.pressProgress,
                        )
                    }
                    .height(LiquidGlassMetrics.IndicatorHeight)
                    .width(tabWidth),
            )
        }
    }
}

@Composable
private fun CompatibleNavigationBar(
    destinations: List<LiquidNavigationDestination>,
    selectedIndex: Int,
    onSelected: (Int) -> Unit,
    modifier: Modifier,
    glassEnabled: Boolean,
) {
    val isDark = isSystemInDarkTheme()
    val density = LocalDensity.current
    val isLtr = LocalLayoutDirection.current == LayoutDirection.Ltr
    val safeSelectedIndex = selectedIndex.coerceIn(0, destinations.lastIndex)
    var tabWidthPx by remember { mutableFloatStateOf(0f) }
    val animatedIndex by animateFloatAsState(
        targetValue = safeSelectedIndex.toFloat(),
        animationSpec = spring(dampingRatio = 0.8f, stiffness = 380f),
        label = "android12NavigationIndicator",
    )
    val shellColor = MaterialTheme.colorScheme.surface.copy(
        alpha = if (glassEnabled) {
            if (isDark) 0.78f else 0.88f
        } else {
            1f
        },
    )
    val activeColor = MaterialTheme.colorScheme.primary
    val inactiveColor = MaterialTheme.colorScheme.onSurfaceVariant

    Box(
        modifier = modifier
            .widthIn(max = 520.dp)
            .fillMaxWidth()
            .dropShadow(
                shape = CircleShape,
                shadow = Shadow(
                    radius = 10.dp,
                    color = Color.Black,
                    alpha = if (isDark) 0.20f else 0.10f,
                ),
            )
            .background(shellColor, CircleShape)
            .height(LiquidGlassMetrics.ShellHeight)
            .padding(4.dp),
        contentAlignment = Alignment.CenterStart,
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .onGloballyPositioned { coordinates ->
                    tabWidthPx = coordinates.size.width.toFloat() / destinations.size
                },
        ) {
            if (tabWidthPx > 0f) {
                val tabWidth = with(density) { tabWidthPx.toDp() }
                Box(
                    modifier = Modifier
                        .graphicsLayer {
                            translationX = if (isLtr) {
                                animatedIndex * tabWidthPx
                            } else {
                                (destinations.lastIndex - animatedIndex) * tabWidthPx
                            }
                        }
                        .width(tabWidth)
                        .height(LiquidGlassMetrics.IndicatorHeight)
                        .background(activeColor.copy(alpha = 0.16f), CircleShape)
                        .innerShadow(CircleShape) {
                            InnerShadow(
                                radius = 4.dp,
                                color = Color.Black.copy(alpha = 0.08f),
                            )
                        },
                )
            }

            CompositionLocalProvider(LocalNavigationContentColor provides inactiveColor) {
                NavigationRow(
                    destinations = destinations,
                    selectedIndex = safeSelectedIndex,
                    onSelected = onSelected,
                    selectedContentColor = activeColor,
                    modifier = Modifier.fillMaxSize(),
                )
            }
        }
    }
}

@Composable
private fun NavigationRow(
    destinations: List<LiquidNavigationDestination>,
    selectedIndex: Int,
    onSelected: (Int) -> Unit,
    modifier: Modifier,
    selectedContentColor: Color? = null,
) {
    Row(
        modifier = modifier,
        horizontalArrangement = Arrangement.SpaceEvenly,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        destinations.forEachIndexed { index, destination ->
            NavigationItem(
                destination = destination,
                selected = index == selectedIndex,
                onClick = { onSelected(index) },
                selectedContentColor = selectedContentColor,
            )
        }
    }
}

@Composable
private fun RowScope.NavigationItem(
    destination: LiquidNavigationDestination,
    selected: Boolean,
    onClick: () -> Unit,
    selectedContentColor: Color? = null,
) {
    val color = if (selected && selectedContentColor != null) {
        selectedContentColor
    } else {
        LocalNavigationContentColor.current
    }
    val scale = LocalNavigationContentScale.current
    Column(
        modifier = Modifier
            .weight(1f)
            .fillMaxHeight()
            .graphicsLayer {
                val contentScale = scale()
                scaleX = contentScale
                scaleY = contentScale
                clip = false
            }
            .semantics { this.selected = selected }
            .clickable(
                interactionSource = null,
                indication = null,
                role = Role.Tab,
                onClick = onClick,
            )
            .padding(horizontal = 4.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(1.dp, Alignment.CenterVertically),
    ) {
        Icon(
            imageVector = destination.icon,
            contentDescription = destination.label,
            tint = color,
        )
        Text(
            text = destination.label,
            color = color,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Medium,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
        )
    }
}
