package io.github.programmermrwang.markstash.core.designsystem.glass

internal const val NativeLiquidGlassMinSdk = 33

internal fun supportsNativeLiquidGlass(sdkInt: Int): Boolean =
    sdkInt >= NativeLiquidGlassMinSdk
