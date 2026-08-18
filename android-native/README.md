# Markstash Native Android

This directory contains the native Kotlin/Jetpack Compose Android client for Markstash.
It is intentionally isolated from the existing Avalonia projects.

## Modules

- `app`: Android lifecycle, navigation, and the Home, Library, Search, and Settings screens.
- `core:designsystem`: Compose theme and the Miuix Backdrop liquid-glass dock.
- `core:model`: immutable application and API models.
- `core:network`: Retrofit/OkHttp client for `/api/v1`.
- `core:platform`: DataStore preferences and the in-process log repository.

## Local build

The project follows the versions and Miuix dependency pattern used by the local BiliPai
checkout. The default uses the public Miuix `0.9.3` release corresponding to BiliPai's
`0.9.3-a370b370-SNAPSHOT` line. A source checkout can still be substituted for development:

The public Miuix blur artifact requires Android 13 (`minSdk 33`). The project compiles
against Android SDK 37.0 while retaining `targetSdk 35`, matching BiliPai's SDK policy.

```properties
# ~/.gradle/gradle.properties
markstash.miuix.source=E:/path/to/miuix
```

Then run:

```powershell
./gradlew.bat :app:assembleDebug
```

The emulator default API endpoint is `http://10.0.2.2:5080/`; it can be changed in Settings.
Debug builds use the `io.github.programmermrwang.markstash.debug` application id so they can
coexist with a signed release installation.
