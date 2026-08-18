# Markstash

Markstash 是一个面向 Windows 与 Android 的资源整理工具。Windows 使用 Avalonia，
Android 使用原生 Kotlin/Jetpack Compose 与 Miuix，两端通过同一个 ASP.NET Core API
访问共享业务能力；主题、日志和设备设置由各平台本地实现。

## 技术栈

- Windows：C# 14、.NET 10、Avalonia 12.1.1、FluentAvaloniaUI 3
- Backend：ASP.NET Core 10 Minimal API、OpenAPI、ProblemDetails
- Android：Kotlin 2.4、Compose BOM 2026.06、Material 3、Miuix 0.9.3
- Android build：Gradle 9.5、AGP 9.3.1、OpenJDK 21
- Tests：xUnit、ASP.NET Core TestServer、JUnit

NuGet 版本由 `Directory.Packages.props` 集中管理，Android 版本由
`android-native/gradle/libs.versions.toml` 集中管理。

## 项目结构

```text
src/
  Markstash.Domain/          Windows 领域模型
  Markstash.Application/     Windows 用例与平台端口
  Markstash.Infrastructure/  Windows 本地设置、日志与系统实现
  Markstash.Contracts/       版本化 ASP.NET Core API DTO
  Markstash.ApiClient/       Windows HTTP 客户端
  Markstash.Backend/         共享 ASP.NET Core 后端
  Markstash.App/             Avalonia UI、MVVM 与组合根
  Markstash.Desktop/         Windows 宿主
android-native/
  app/                       原生 Android 壳与四个主页面
  core/designsystem/         Miuix Backdrop 液态玻璃导航
  core/model/                Android 不可变模型与端口
  core/network/              Retrofit/OkHttp API 实现
  core/platform/             DataStore 与 Android 本地日志
tests/
  Markstash.Tests/           Windows、应用层与 API Client 测试
  Markstash.Backend.Tests/   ASP.NET Core 集成测试
```

```text
Windows Avalonia ---- Markstash.ApiClient ----┐
                                               ├---- ASP.NET Core /api/v1
Android Compose ----- Retrofit/OkHttp --------┘

Windows local: JSON settings + file diagnostics
Android local: DataStore + Android process diagnostics
```

更完整的依赖方向、共享契约和平台能力边界见 [`docs/architecture.md`](docs/architecture.md)。

## 启动后端

后端开发地址统一为 `http://localhost:5080`：

```powershell
.\scripts\run-backend.ps1
```

或者使用容器：

```powershell
docker compose up --build backend
```

OpenAPI 位于 `/openapi/v1.json`。诊断 API 默认关闭；本地排查时可运行：

```powershell
.\scripts\run-backend.ps1 -ExposeDiagnostics
```

## Windows

```powershell
dotnet restore src/Markstash.Desktop/Markstash.Desktop.csproj --configfile NuGet.Config
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj
```

桌面 UI、路由和 FluentAvalonia 结构保持原样。启动时会后台探测同一个后端，失败只记录
日志，不阻塞离线启动。通过 `MARKSTASH_API_URL` 覆盖默认地址：

```powershell
$env:MARKSTASH_API_URL = "https://api.example.com/"
```

## Android

原生客户端最低 Android 13（API 33），这是公开版 Miuix Backdrop 的完整液态玻璃要求。
它使用页面 sibling capture、活动内容独立 capture、combined Backdrop、24dp 折射、
色散、重力高光、弹簧拖动与 56/78dp 指示器形变。

准备 OpenJDK 21 和 Android SDK 后运行：

```powershell
.\scripts\setup-android.ps1
```

或者直接构建：

```powershell
cd android-native
.\gradlew.bat :app:assembleDebug --no-configuration-cache
```

调试 APK 默认访问模拟器宿主 `http://10.0.2.2:5080/`，也可以在 Android 设置页修改。
Debug 使用独立的 `.debug` applicationId，可与正式包并存。Release 禁止明文 HTTP，并通过
`-Pmarkstash.apiBaseUrl=https://.../` 注入正式地址。

## 验证

```powershell
dotnet test tests/Markstash.Tests/Markstash.Tests.csproj -c Release
dotnet test tests/Markstash.Backend.Tests/Markstash.Backend.Tests.csproj -c Release

cd android-native
.\gradlew.bat :core:network:testDebugUnitTest :core:designsystem:testDebugUnitTest `
  :app:lintDebug :app:assembleDebug --no-configuration-cache
```

## 发布配置

Android Release 需要以下 GitHub Actions secrets：

- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_SIGNING_STORE_PASSWORD`
- `ANDROID_SIGNING_KEY_ALIAS`
- `ANDROID_SIGNING_KEY_PASSWORD`

还需要仓库 Actions variable：

- `MARKSTASH_API_BASE_URL`，必须是正式 HTTPS API 地址

项目采用 [GPL-3.0](LICENSE) 许可证。Android 液态玻璃实现的 BiliPai 来源与许可证
说明见 `android-native/THIRD_PARTY_NOTICES.md`。
