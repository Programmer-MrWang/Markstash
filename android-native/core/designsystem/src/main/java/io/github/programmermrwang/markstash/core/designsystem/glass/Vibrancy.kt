/* Adapted from BiliPai. SPDX-License-Identifier: GPL-3.0-only */
package io.github.programmermrwang.markstash.core.designsystem.glass

import top.yukonga.miuix.kmp.blur.BackdropEffectScope
import top.yukonga.miuix.kmp.blur.colorControls

fun BackdropEffectScope.markstashVibrancy() {
    colorControls(brightness = 0f, contrast = 1f, saturation = 1.5f)
}
