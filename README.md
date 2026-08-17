# Markstash

Markstash 是一个面向 Windows 与 Android 的跨平台资源整理工具。
当前仓库提供可以继续扩展的应用地基，业务方向仍可围绕收藏、搜索、文件、笔记与 AI 能力迭代。

## 技术栈

- C# 14 / .NET 10 LTS
- Avalonia 12.1.1
- FluentAvaloniaUI 3.0.2
- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Extensions.Hosting / DependencyInjection / Logging 10.0.10
- xUnit

所有 NuGet 版本都由 `Directory.Packages.props` 集中管理。共享 UI 只依赖 `net10.0`，
Windows 使用 Desktop 宿主，Android 使用独立的 `net10.0-android` 宿主。

## 项目结构

```text
src/
  Markstash.Domain/          领域模型，不依赖 UI 或平台
  Markstash.Application/     用例、端口和应用服务
  Markstash.Infrastructure/  JSON 设置与跨平台数据路径
  Markstash.App/             Avalonia + FA3 共享 UI/MVVM
  Markstash.Desktop/         Windows 启动宿主
  Markstash.Android/         Android 启动宿主与资源
tests/
  Markstash.Tests/           应用与基础设施测试
```

依赖只允许从外层指向内层：

```text
Desktop / Android -> App -> Infrastructure -> Application -> Domain
                              Application -----------------> Domain
```

更完整的分层、组合根与扩展约束见 [`docs/architecture.md`](docs/architecture.md)。

## 本地运行

桌面：

```powershell
dotnet restore src/Markstash.Desktop/Markstash.Desktop.csproj --configfile NuGet.Config
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj
```

桌面宿主支持基础启动参数：

```powershell
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj -- --verbose
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj -- --data-dir .\.data
dotnet run --project src/Markstash.Desktop/Markstash.Desktop.csproj -- markstash://app/settings
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

完成后构建 APK：

```powershell
dotnet build src/Markstash.Android/Markstash.Android.csproj -f net10.0-android -c Debug
```

默认最低 Android 版本为 API 23（Android 6.0）。调试 APK 输出在
`src/Markstash.Android/bin/Debug/net10.0-android/`。

## 已有地基

- FluentAvalonia 主题、Windows Mica 托管标题栏、窗口菜单与自适应导航壳
- Generic Host、生命周期状态、依赖注入验证与可控关闭
- 集中路由、返回栈和 `markstash://app/{route}` 启动 URI
- `.resx` 中英文资源基础
- 版本化设置、旧格式迁移、原子替换、备份恢复和未来版本只读保护
- Windows、Android 路径分区与测试/便携目录覆盖
- JSON 控制台日志、文本 `.log` 与 GZip 归档日志、崩溃报告、异常会话检测和诊断包服务
- Windows 日志查看窗口、桌面快捷方式与日志/应用目录快捷入口
- Source Link、统一版本元数据、Dependabot、覆盖率 CI 与标签发布流水线
- 宿主冒烟、分层约束、设置恢复和诊断链路测试
- 可复用的 Android 环境安装脚本

当前会话写入 `log-年-月-日-时-分-秒-序号.log`；日志超过 10 MiB 时立即轮转，
已经结束的会话会在下次启动时原子压缩为 `.log.gz`。日志默认保留 30 天且最多保留
64 份压缩归档，崩溃报告最多保留 20 份。诊断包通过
`IAppDiagnosticsService` 创建，包含运行环境、日志、崩溃报告和当前设置；它不会自动上传。

Android 默认关闭系统云备份，等业务数据分类与加密策略明确后再按目录显式开放。

正式发布使用 `vMAJOR.MINOR.PATCH` SemVer 标签。Release 会先运行测试；Windows 产出 ZIP。
Android Release 必须配置以下 GitHub Actions secrets，
缺少任意一个都会拒绝发布 unsigned APK：

- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_SIGNING_STORE_PASSWORD`
- `ANDROID_SIGNING_KEY_ALIAS`
- `ANDROID_SIGNING_KEY_PASSWORD`

项目采用 [GPL-3.0](LICENSE) 许可证。
