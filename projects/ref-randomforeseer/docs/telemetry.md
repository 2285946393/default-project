# 诊断遥测过滤

`ModTelemetry.Register` 为 `diagnostics` 申请项注册 `ModTelemetryFilter.ShouldCapture`，使用 RitsuLib 0.5.14 新增的 `TelemetryRequest.WithCaptureFilter`。筛选发生在诊断 payload 构建和事件入队前；`ModTelemetryAdapter` 仅补充 `random_foreseer_version` 并转发，不再筛选发送队列。

## 过滤规则

- `exception`：本项目 `ModTelemetry.CaptureException` 设置 `capture_source = random_foreseer/<subsystem>`，该来源直接放行。原生上下文没有 `capture_mode`，因此使用与上报方法共享的来源前缀识别主动上报；原有 `capture_mode = manual` 属性仍保留。自动诊断通过 `TelemetryCaptureContext.Exception` 检查异常及其 `InnerException` 链，任一 `StackTrace` 包含大小写敏感的 `RandomForeseer.` 即保留。异常消息、序列化后的同名字段不用于判断。
- `godot_engine_error`：由运行时 RitsuLib 0.6.0+ 采集；其 `SourceData` 是已脱敏的 `engine_error` `JsonObject`，不是完整信封。`script_backtrace` 或 `code` 字符串中任一包含 `RandomForeseer.` 即保留。Godot 可能把 C# 异常栈写入 `code`；两个字段都需检查，与网关规则一致。缺失、空或非字符串字段不匹配；不扫描消息、文件、方法、其他元数据，不对主动来源额外放行。
- 按 RitsuLib 固定的小写事件名分发；其他诊断事件继续放行。筛选器只同步读取传入数据，不修改或保留来源对象，不进行网络或磁盘操作。

项目构建依赖及 manifest 最低版本均为 0.5.14。较旧的受支持运行时只执行异常筛选，0.6.0+ 新增的引擎事件通过同一 `SourceData` API 筛选，无需引用 0.6.x 专有类型。

## 发送队列与网关

原生筛选只作用于新采集，不会重新筛选已持久化的队列。旧版 Mod 和历史积压中的引擎错误继续由现有网关筛选；网关对全部过滤的批次确认成功，让客户端正常清除已处理的队列。此次迁移不清理本地队列或已入库历史事件。

命名空间标记只能证明调用链涉及本模组，不证明错误由本模组引起。缺少堆栈或堆栈被截断时，相关错误也可能被过滤。RitsuLib 自身的诊断授权、去重与限流仍生效。

纯逻辑回归位于 `tests/RandomForeseer.Tests/Telemetry/TelemetryFilterTests.cs`，覆盖原生请求筛选器挂接、异常链、主动来源、两种引擎堆栈、缺失字段和来源数据不可变性。运行方式见 [本地回归测试](testing.md)。
