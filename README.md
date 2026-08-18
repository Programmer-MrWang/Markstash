# Markstash

Markstash 是一个面向 Windows 与 Android 的本地优先资源整理工具。两端各自拥有本地
运行时、数据目录和平台实现，不依赖云端，也不通过 HTTP 相互连接。

## 技术栈

- Windows：C# 14、.NET 10、Avalonia 12.1.1、FluentAvaloniaUI 3
- Android：Kotlin 2.4、Jetpack Compose、Material 3、Miuix 0.9.3
- Android build：Gradle Wrapper 9.3.1、AGP 9.1.0、OpenJDK 21
- Tests：xUnit、JUnit、Android Lint

NuGet 版本由 `windows/Directory.Packages.props` 集中管理，Android 版本由
`android/gradle/libs.versions.toml` 集中管理。

## 架构

```text
Windows
Desktop -> App/Features -> Application -> Domain
                       \-> Infrastructure -> Application

Android
Compose Features -> ViewModel -> core:model ports
                              -> core:platform implementations
                 -> core:designsystem
```

Windows UI 直接调用 Application 用例。Infrastructure 实现文件、资源仓储、设置、日志、
诊断与系统能力端口。Android 使用独立的 Kotlin/Compose 工程和手机本地实现。

资源概念和行为在两端保持一致，但不共享语言运行时或磁盘模型：

- [`docs/resource-model.md`](docs/resource-model.md)：资源、查询和 Repository 语义
- [`docs/markstash-package.md`](docs/markstash-package.md)：版本化 `.markstash` 导入导出格式
- [`docs/architecture.md`](docs/architecture.md)：依赖方向、生命周期和隐私边界

## 项目结构

```text
windows/
  Markstash.slnx             Windows 解决方案入口
  src/
    Markstash.Domain/          Windows 领域模型
    Markstash.Application/     Windows 用例与平台端口
    Markstash.Infrastructure/  Windows 本地持久化、日志与系统实现
    Markstash.App/             Avalonia Shell、Feature 页面与组合根
    Markstash.Desktop/         Windows 入口和打包宿主
  tests/
    Markstash.Tests/           Windows 单元、架构与集成测试
android/
  app/                       Activity、Compose Feature 页面和依赖装配
  core/designsystem/         Miuix Backdrop 液态玻璃设计系统
  core/model/                Android 不可变模型与本地端口
  core/platform/             DataStore、持久化日志与手机本地实现
```

## Windows

```powershell
cd windows
dotnet restore src/Markstash.Desktop/Markstash.Desktop.csproj --configfile NuGet.Config
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj
```

用户设置、资源索引、日志、崩溃报告和诊断输出都保存在本机应用数据目录。可通过
`MARKSTASH_DATA_DIR` 为开发和测试覆盖数据根目录。

## Android

原生客户端最低 Android 13（API 33）。准备 OpenJDK 21 和 Android SDK 后运行：

```powershell
.\scripts\setup-android.ps1
```

或者构建、安装并启动 Debug APK：

```powershell
.\scripts\run-android-debug.ps1 -Install -Launch
```

需要把测试包发给其他设备安装时，运行同一脚本并分发
`artifacts/Markstash-android-debug-installable.apk`。脚本会在复制前检查 APK，确保它没有
`android:testOnly="true"`。不要分发 Android Studio 点击 Run 后留下的 APK；该流程可能生成
只能由 `adb install -t` 安装的测试包。面向正式用户的版本仍应使用 GitHub Release 中由
release 签名流程生成的 `Markstash-android-signed.apk`。

Android Studio、IntelliJ IDEA 或 Rider 应直接打开 `android`。Debug 使用独立的
`.debug` applicationId，可与正式包并存。

## 数据与隐私

- 两端数据默认仅保存在当前设备。
- `.markstash` 是显式导入导出的版本化 ZIP 容器，不等同于内部数据库格式。
- 默认导出只包含资源元数据；附件必须由用户显式选择。
- 诊断包采用允许列表，不包含资源正文、附件、访问令牌、原始设置文件或敏感绝对路径。

## 验证

```powershell
cd windows
dotnet build src/Markstash.Desktop/Markstash.Desktop.csproj -c Release
dotnet test tests/Markstash.Tests/Markstash.Tests.csproj -c Release

cd ../android
.\gradlew.bat :core:model:testDebugUnitTest `
  :core:platform:testDebugUnitTest `
  :core:designsystem:testDebugUnitTest `
  :app:lintDebug :app:assembleDebug --no-configuration-cache
```

发布标签 `vMAJOR.MINOR.PATCH` 生成 Windows x64 包和已签名 Android APK。项目采用
[GPL-3.0](LICENSE) 许可证；Android 液态玻璃来源说明见
`android/THIRD_PARTY_NOTICES.md`。
