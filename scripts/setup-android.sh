#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
android_root="$repo_root/android"
android_sdk_directory="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-$HOME/Android/Sdk}}"

if [[ -z "${JAVA_HOME:-}" ]] || [[ ! -x "$JAVA_HOME/bin/java" ]]; then
  printf 'OpenJDK 21 is required and JAVA_HOME must point to it.\n' >&2
  exit 1
fi

if [[ ! -d "$android_sdk_directory" ]]; then
  printf 'Android SDK was not found at %s.\n' "$android_sdk_directory" >&2
  exit 1
fi

export ANDROID_HOME="$android_sdk_directory"
export ANDROID_SDK_ROOT="$android_sdk_directory"

cd "$android_root"
bash ./gradlew :app:assembleDebug -Pandroid.injected.testOnly=false --no-configuration-cache

printf 'Android SDK: %s\n' "$android_sdk_directory"
printf 'Java SDK:    %s\n' "$JAVA_HOME"
printf 'APK:         %s\n' "$android_root/app/build/outputs/apk/debug/app-debug.apk"
