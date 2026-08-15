#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
android_sdk_directory="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-$HOME/Android/Sdk}}"
java_sdk_directory="${JAVA_HOME:-$HOME/.local/share/markstash-jdk-17}"
project="$repo_root/src/Markstash.Android/Markstash.Android.csproj"

dotnet workload install android
dotnet restore "$project" --configfile "$repo_root/NuGet.Config"
dotnet build "$project" \
  --framework net10.0-android \
  --target InstallAndroidDependencies \
  --no-restore \
  --property:AndroidSdkDirectory="$android_sdk_directory" \
  --property:JavaSdkDirectory="$java_sdk_directory" \
  --property:AcceptAndroidSDKLicenses=true

printf 'Android SDK: %s\n' "$android_sdk_directory"
printf 'Java SDK:    %s\n' "$java_sdk_directory"
printf 'Export ANDROID_HOME, ANDROID_SDK_ROOT, and JAVA_HOME in your shell profile.\n'
