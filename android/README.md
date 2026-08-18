# Markstash Native Android

This directory contains the native Kotlin/Jetpack Compose Android client for Markstash.
It is intentionally isolated from the existing Avalonia projects.

## Modules

- `app`: Android lifecycle, navigation, and the Home, Library, Search, and Settings screens.
- `core:designsystem`: Compose theme and the Miuix Backdrop liquid-glass dock.
- `core:model`: immutable application models and local ports.
- `core:platform`: phone-local runtime, DataStore preferences, and a persistent bounded log repository.

## Local build

The project follows the versions and Miuix dependency pattern used by the local BiliPai
checkout. The default uses the public Miuix `0.9.3` release corresponding to BiliPai's
`0.9.3-a370b370-SNAPSHOT` line. A source checkout can still be substituted for development:

The public Miuix blur artifact requires Android 13 (`minSdk 33`). The project compiles
against Android SDK 37.0 while retaining `targetSdk 35`, matching BiliPai's SDK policy.
The Gradle Wrapper is 9.3.1, Android Gradle Plugin is 9.1.0, Kotlin is 2.4.0, and the
build must run on JDK 21.

```properties
# ~/.gradle/gradle.properties
markstash.miuix.source=E:/path/to/miuix
```

Then run:

```powershell
./gradlew.bat :core:model:testDebugUnitTest `
  :core:platform:testDebugUnitTest `
  :app:assembleDebug
```

For device installation and launch from the repository root:

```powershell
.\scripts\run-android-debug.ps1 -Install -Launch
```

For a debug-signed APK that can be copied to another device and installed from a file
manager, run the same script without switches and share
`artifacts/Markstash-android-debug-installable.apk`. The script rejects APKs marked with
`android:testOnly="true"`; do not distribute the APK left by Android Studio's Run action.
Public releases must use the signed APK produced by the release workflow.

Open `android` as the project root in Android Studio or IntelliJ IDEA/Rider for Kotlin
breakpoints and Compose debugging. The native Android project is intentionally independent from
the Windows solution.

The Android app is self-contained: it starts and operates with phone-local state, without a
computer, LAN endpoint, or cloud service. Debug builds use the
`io.github.programmermrwang.markstash.debug` application id so they can coexist with a signed
release installation.
