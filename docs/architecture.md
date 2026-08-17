# 架构约束

Markstash 的共享代码按稳定性从内向外分层。新增功能应优先延续这些边界，避免把平台 API、UI 类型或磁盘格式带入内层。

```text
Domain <- Application <- Infrastructure <- App <- Desktop / Android
              ^____________________________|
```

## 各层职责

- `Markstash.Domain`：领域值对象与规则，不引用其他 Markstash 项目。
- `Markstash.Application`：用例、端口、生命周期与诊断契约，只依赖 Domain。
- `Markstash.Infrastructure`：文件、路径、日志和运行时实现，依赖 Application。
- `Markstash.App`：Avalonia 视图、ViewModel、路由、主题和组合根。
- `Markstash.Desktop` / `Markstash.Android`：平台入口、打包与最外层启动故障边界。

`ArchitectureTests` 会阻止内层反向引用 UI 或基础设施。

## 组合根与生命周期

`ServiceConfiguration` 是共享组合根。它创建 Generic Host、启用作用域与构造验证，并注册所有 `IHostedService`。应用启动顺序是：

1. 解析宿主参数并应用目录覆盖。
2. 构建、启动 Host。
3. 加载设置并应用主题。
4. 创建根 ViewModel 和视图。
5. 将应用生命周期标记为 Running。

桌面受控生命周期会在退出时停止并释放 Host。Android 的 Activity 重建不等同于进程退出，因此不会在 `OnDestroy` 停止共享 Host。
桌面 `Exit` 会等待 Host 完成停止；新增的 `IHostedService.StopAsync` 不得切回 Avalonia UI 线程，耗时清理应自行使用后台 I/O 并遵守取消令牌。

## 持久化规则

- 配置文件使用独立 `schemaVersion` 和单调 `revision`。
- 保存使用同目录唯一临时文件、落盘 flush、写后解析校验和同卷替换。
- 读取顺序为 primary、backup、默认值；损坏文件隔离并限量保留。
- 高于当前 schema 的文件进入只读模式，旧程序不得覆盖。
- `MARKSTASH_DATA_DIR` 会覆盖整套目录，供测试、便携运行和故障复现使用。

正式数据模型应继续使用 Infrastructure DTO，不要直接把可变磁盘格式暴露为 Domain 契约。

## 导航与本地化

页面注册为 `NavigationRoute`，由 `INavigationService` 解析、缓存并维护返回栈。`NavigationPlacement` 决定路由显示在主导航区、页脚区或作为隐藏子页面；只有隐藏页面会根据返回栈显示返回按钮。外部启动 URI 只映射到已注册 route，不允许从 URI 构造任意类型。

用户可见文字放在 `Localization/Strings*.resx`。新增文字必须同时提供中性资源与英文资源；领域错误码和日志模板保持稳定，不依赖界面语言。

## 诊断与隐私

- 普通日志经 `ILogger` 写往调试、JSON 控制台和本地文件 provider。
- 文件日志使用 `时间|级别|类别|消息` 的稳定文本格式；异常堆栈作为后续行写入，当前会话文件扩展名为 `.log`。
- 单个日志达到 10 MiB 时轮转；应用启动时把已结束会话的 `.log` 原子压缩为 `.log.gz`，并按 30 天、最多 64 份归档执行清理。
- Windows 日志窗口通过 `IAppLogReader` 读取活动 `.log` 与归档 `.log.gz`，不直接依赖文件日志 provider 的内部实现；旧版 `.jsonl` 只作为迁移兼容输入。
- 未处理异常另写独立 JSON 崩溃报告，避免 logger 本身故障时丢失诊断。
- 会话标记按进程隔离，只把已停止进程遗留的标记视为异常退出。
- 日志会遮蔽常见密钥字段和用户主目录；诊断包只由显式调用创建且不会上传。
- 新增敏感数据前，应同步更新日志脱敏、诊断包收集范围和 Android 备份策略。

## 验证门槛

提交前至少运行：

```powershell
dotnet build src/Markstash.Desktop/Markstash.Desktop.csproj -c Release
dotnet test tests/Markstash.Tests/Markstash.Tests.csproj -c Release
```

涉及 Android 宿主时，再构建 `Markstash.Android.csproj`。发布标签 `vMAJOR.MINOR.PATCH` 会生成 Windows x64 自包含压缩包和 Android APK。
Release 工作流会在发布构建前重复运行测试，并拒绝非法 SemVer 标签或未签名 Android 产物。
