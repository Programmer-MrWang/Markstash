# 架构约束

Markstash 由两个独立的本地应用运行时组成。Windows 使用 Avalonia、Application 用例和
本地 Infrastructure；Android 使用 Kotlin/Jetpack Compose 与手机本地实现。两端不共享
进程、数据库、数据目录或网络端点。

```text
Windows
Desktop -> App Shell/Features -> Application -> Domain
                                ^
                                |
                         Infrastructure

Android
app/feature -> ViewModel -> core:model
                         -> core:platform -> core:model
            -> core:designsystem -> core:model
```

## Windows 边界

- `Markstash.Domain`：资源与偏好领域模型，不引用其他 Markstash 项目。
- `Markstash.Application`：用例、Repository 和平台端口，只依赖 Domain。
- `Markstash.Infrastructure`：资源文件、设置、日志、诊断和操作系统能力实现。
- `Markstash.App`：Avalonia Shell、Feature 页面、ViewModel、路由、主题和组合根。
- `Markstash.Desktop`：Windows 入口、打包和最外层启动故障边界。

桌面 UI 直接调用 Application。仓储格式、文件系统 API 和平台类型不得进入 Domain 或 UI。
Application 不得引用 Infrastructure 或 App，Infrastructure 不得引用 App。

页面按 `Markstash.App/Features/<Feature>` 组织；Shell、导航和窗口保留在共享 UI 目录。
新增页面不为每个 Feature 创建单独项目，除非以后出现独立发布或明确的编译边界需求。

## Android 边界

- `app`：Activity、Feature 页面、ViewModel 与依赖装配。
- `core:model`：不可变模型、查询对象和 Repository 端口，不依赖 Compose UI。
- `core:platform`：DataStore、持久化限容量日志、手机本地资源与系统适配器。
- `core:designsystem`：主题和 Miuix Backdrop 液态玻璃组件。

ViewModel 必须显式接收所需端口，不接收 `AppContainer`。`AppContainer` 只允许存在于
Application/Activity 组合根。页面按 `app/.../feature/<feature>` 整理，不拆分额外 Gradle 模块。

## 资源与交换格式

两端遵循 [`resource-model.md`](resource-model.md) 中的概念、字段和查询语义。语言模型和
数据库实现可以不同，但导出行为、标签规范化、时间格式和覆盖规则必须一致。

`.markstash` 包遵循 [`markstash-package.md`](markstash-package.md)。包是版本化交换格式，
不是数据库备份；导入前必须验证版本、条目路径、校验和与资源冲突策略。

## 生命周期与本地能力

Windows Generic Host 随 Avalonia 应用启动和退出。Android 使用 Application、Activity、
ViewModel 和协程生命周期。新增文件选择、通知、凭据、分享等功能时，先定义端口，再由
平台实现；平台 API 和磁盘格式不得进入共享概念规范。

## 数据与诊断隐私

- 资源正文、附件、数据库和访问令牌永不进入默认诊断包。
- 诊断包只从明确允许的日志、崩溃报告和脱敏摘要中取材，不递归扫描数据目录。
- 设置以允许列表生成脱敏摘要，不复制 `preferences.json` 或备份原文。
- 环境摘要不得暴露用户目录、应用数据根目录或其他原始绝对路径。
- `.markstash` 数据导出必须由用户显式触发，与诊断导出完全分离。

Windows 设置继续使用带 schema/revision 的 JSON 原子持久化与备份恢复。Android 设置使用
DataStore，日志使用应用私有目录中的持久化、限容量 Repository。

## 验证门槛

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

发布标签 `vMAJOR.MINOR.PATCH` 只生成 Windows 与 Android 本地应用，不发布独立后端。
