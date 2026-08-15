# Markstash

Markstash 是一个面向 Windows、macOS、Linux 与 Android 的跨平台资源整理工具。
当前仓库提供可以继续扩展的应用地基，业务方向仍可围绕收藏、搜索、文件、笔记与 AI 能力迭代。

## 技术栈

- C# 14 / .NET 10 LTS
- Avalonia 12.1.1
- FluentAvaloniaUI 3.0.2
- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Extensions.DependencyInjection 10.0.10
- xUnit

所有 NuGet 版本都由 `Directory.Packages.props` 集中管理。共享 UI 只依赖 `net10.0`，
Windows、macOS 和 Linux 共用 Desktop 宿主，Android 使用独立的 `net10.0-android` 宿主。

## 项目结构

```text
src/
  Markstash.Domain/          领域模型，不依赖 UI 或平台
  Markstash.Application/     用例、端口和应用服务
  Markstash.Infrastructure/  JSON 设置与跨平台数据路径
  Markstash.App/             Avalonia + FA3 共享 UI/MVVM
  Markstash.Desktop/         Windows/macOS/Linux 启动宿主
  Markstash.Android/         Android 启动宿主与资源
tests/
  Markstash.Tests/           应用与基础设施测试
```

依赖只允许从外层指向内层：

```text
Desktop / Android -> App -> Infrastructure -> Application -> Domain
                              Application -----------------> Domain
```

## 本地运行

桌面：

```powershell
dotnet restore src/Markstash.Desktop/Markstash.Desktop.csproj --configfile NuGet.Config
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj
```

测试：

```powershell
dotnet restore tests/Markstash.Tests/Markstash.Tests.csproj --configfile NuGet.Config
dotnet test tests/Markstash.Tests/Markstash.Tests.csproj
```

## Android

Windows PowerShell 中以管理员身份准备 workload、JDK 与 Android SDK：

```powershell
.\scripts\setup-android.ps1
```

macOS/Linux：

```bash
bash ./scripts/setup-android.sh
```

完成后构建 APK：

```powershell
dotnet build src/Markstash.Android/Markstash.Android.csproj -f net10.0-android -c Debug
```

默认最低 Android 版本为 API 23（Android 6.0）。调试 APK 输出在
`src/Markstash.Android/bin/Debug/net10.0-android/`。

## 已有地基

- FluentAvalonia 主题与自适应导航壳
- Desktop/Android 双生命周期启动
- 依赖注入与清晰的项目边界
- 系统/浅色/深色主题持久化
- Windows、macOS、Linux、Android 各自适配的数据目录
- 三桌面平台与 Android 的 CI 构建矩阵
- 可复用的 Android 环境安装脚本

项目采用 [GPL-3.0](LICENSE) 许可证。
