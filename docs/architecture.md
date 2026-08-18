# 架构约束

Markstash 采用两个独立前端、一个版本化 HTTP 后端。Windows 保留 Avalonia，Android
使用原生 Kotlin/Jetpack Compose；两端共享的是 ASP.NET Core API 契约，而不是 UI、
生命周期或平台服务实现。

```text
Windows
Desktop -> App -> Infrastructure -> Application -> Domain
              \
               -> ApiClient -> Contracts <- Backend
                                      ^
Android                               |
Compose app -> core:network ----------+
            -> core:platform
            -> core:designsystem
            -> core:model
```

## 共享边界

- `Markstash.Contracts`：`/api/v1` 的 C# DTO 与版本常量，不引用 UI 或平台项目。
- `Markstash.Backend`：中立的 ASP.NET Core Minimal API，只引用 Contracts；不得反向引用
  Avalonia、Windows Application/Infrastructure 或 Android 代码。
- `Markstash.ApiClient`：Windows typed `HttpClient`，把 Avalonia 端接到同一个 API。
- Android `core:network`：Retrofit/OkHttp 客户端，按同一 JSON 契约实现 Kotlin 传输模型。
- API 的新增或破坏性变化必须先更新版本化契约、OpenAPI 和两端契约测试。

当前 API 提供 health、bootstrap 与可选的服务器诊断。`/api/v1/resources` 已在 bootstrap
能力中预留，但在真实共享资源模型落地前明确返回不可用，客户端不得假装数据已经同步。

## Windows 边界

- `Markstash.Domain`：领域值对象与规则，不引用其他 Markstash 项目。
- `Markstash.Application`：Windows 用例、端口、生命周期与诊断契约，只依赖 Domain。
- `Markstash.Infrastructure`：Windows 文件、路径、日志和运行时实现。
- `Markstash.App`：Avalonia 视图、ViewModel、路由、主题和组合根。
- `Markstash.Desktop`：Windows 入口、打包与最外层启动故障边界。
- `BackendConnectivityService` 只在后台探测 API；后端不可用时桌面仍可离线启动。

`ArchitectureTests` 会阻止内层反向引用 UI 或基础设施。桌面 UI、FluentAvalonia 导航和
本地设置格式不因 Android 前端替换而改变。

## Android 边界

- `app`：Activity、Compose 导航、ViewModel 与依赖装配。
- `core:model`：不可变模型和仓储端口，不依赖 Android UI。
- `core:network`：Retrofit、OkHttp、序列化和 endpoint 规范化。
- `core:platform`：DataStore、本地日志及后续 Android 系统能力适配器。
- `core:designsystem`：主题和 Miuix Backdrop 液态玻璃组件。

Android 最低 API 33。Debug 使用 `.debug` applicationId 与正式包并存；Release 关闭明文
HTTP，正式 API 地址在构建时注入。主题、液态玻璃开关、endpoint 和设备日志属于本地
状态，不上传后端。

## 液态玻璃

底部导航不是半透明色块模拟：页面先通过 sibling `layerBackdrop` 提供真实背景采样，
活动内容再进入独立 capture layer，二者由 combined backdrop 交给移动指示器。实现位于
`android-native/core/designsystem/.../glass/`，包括 Miuix blur/lens、色散、重力高光、
内阴影以及可中断的阻尼拖拽。

视觉常量与 BiliPai 对齐：shell 64dp、indicator 56dp、按压比例 78/56、shell blur 4dp、
lens 24/24dp、indicator lens 10/14dp、chromatic aberration 0.5、边缘橡皮筋 4dp。
相关改编保留 GPL-3.0 来源说明和完整许可证。

## 生命周期与本地能力

桌面 Generic Host 随 Windows 应用启动和退出；Android 使用 Activity/ViewModel 生命周期，
不承载 .NET Host。新增平台功能先在各自前端定义端口，再由 Windows 或 Android 实现，
例如文件选择器、通知、凭据存储、分享和系统主题。平台 API 或磁盘格式不得进入共享
HTTP 契约。

Windows 设置继续使用带 schema/revision 的 JSON 原子持久化、备份恢复和只读保护。
Android 设置使用 DataStore。两端日志和诊断默认保留本地；服务器诊断默认关闭，只能由
显式配置开启。

## 验证门槛

提交前至少运行：

```powershell
dotnet build src/Markstash.Desktop/Markstash.Desktop.csproj -c Release
dotnet test tests/Markstash.Tests/Markstash.Tests.csproj -c Release
dotnet test tests/Markstash.Backend.Tests/Markstash.Backend.Tests.csproj -c Release

cd android-native
.\gradlew.bat :core:network:testDebugUnitTest :core:designsystem:testDebugUnitTest `
  :app:lintDebug :app:assembleDebug --no-configuration-cache
```

发布标签 `vMAJOR.MINOR.PATCH` 生成 Windows x64、自包含 Linux 后端和已签名 Android
APK。Release 流水线拒绝非法 SemVer、缺失签名材料或非 HTTPS 的 Android API 地址。
