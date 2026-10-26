# ref-danmakuspire · 弹幕尖塔 DanmakuSpire

工坊 id **3779807977** · 作者 **Icnsis** · 版本 **v1.2.1** · 清单 id `DanmakuSpire`

> 这是**反编译参考目录**，不是我们的工程。只读，不改。
> 我们的 mod 源码归国内 WorkBuddy 维护（2026-10-09 起分工）。

## 这是什么

一个《杀戮尖塔 2》的**纯事件监听型模组**：不加角色、不加卡牌，只监听玩家爬塔行为
（打牌、受伤、购买、奖励、事件、火堆、SL 等），在对应时机发射**横向滚动的社区弹幕**
——B 站那种弹幕，**不是弹幕射击游戏**。

**依赖为空**（`"dependencies": []`）：不用 RitsuLib、不用 BaseLib，Harmony、设置 UI、
内容包、联网全部自研。所以它是「**单 mod 自给自足 + 逐项降级**」的活样本。

## 目录

```
DanmakuSpire.json          原始清单（id / 版本 / 依赖 / affects_gameplay=false）
README-作者原文.md          作者说明（中文 + 英文），作者原话，别改
CHANGELOG.md               更新日志（v1.2.1 起，含设计意图）
content/                   随包内容快照（外置 JSON，程序集里只剩兜底）
  danmaku/rules.json       58 组规则词条库（336 条单人 / 58 条多人）
  catalogs/                aliases / characters / collectible-sets / names-zh
decompiled/
  DanmakuSpire.decompiled.cs   44,376 行 / 1.89 MB / 450 类型 / 11 命名空间
```

## 反编译命令（可复现）

```bash
"$USERPROFILE/.dotnet/tools/ilspycmd.exe" \
  -o "projects/ref-danmakuspire/decompiled" \
  -p \
  "D:/software/steam/steamapps/workshop/content/2868840/3779807977/DanmakuSpire.dll"
```

（`-p` = 生成单个 .cs 文件；产物名 `DanmakuSpire.decompiled.cs`）

## 命名空间地图（行号来自 44,376 行版本）

| 行号 | 命名空间 | 内容 |
|---|---|---|
| 107 | `DanmakuSpireCode` | `MainFile` 入口、`CoverageAccumulator`、诊断 |
| 1874 | `.Telemetry` | `RuntimeTraceRecorder`、覆盖度契约、实机 fixture |
| 3484 | `.Emitter` | `DanmakuEmitter`、`RollingDanmakuPlanner/Scheduler/Spawner`、`UgcEmitter`、`RollingLaneAllocator` |
| 8257 | `.Data` | `CardScanner`、`DanmakuLibrary`、`DanmakuRuleCatalog`、`MonsterIds` |
| 9834 | `.Monitor` | 事实总线、`PhaseRouter`、规则消费者 |
| 11564 | `.Monitor.Rules` | **33 个 `[HarmonyPatch]`** 全在这一层 |
| 14425 | `.Core` | `DanmakuHub`、`DanmakuMultiplayerState`、`RuleLifecycleGate`、`ShuffleBag` |
| 19094 | `.Core.Multiplayer` | 4 个网络消息类型 + `DanmakuNetworkForwarder` |
| 19825 | `.Content` | `ContentStore`、`UgcRuntime`、`UgcSyncService`、投稿客户端 |
| 26499 | `.Config` | `DanmakuModConfig`、设置面板宿主、`NSettingsScreen` 注入补丁 |
| 43771 | `.Commands` | 4 个控制台命令（`AbstractConsoleCmd` 子类） |

## 一句话结论（详见笔记）

**这是这个生态里「兼容性工程」做得最好的样本**：补丁逐个装、坏了就丢那一个；
能力探测用 `AccessTools.TypeByName` + `TargetMethod() → null`（Harmony 自动 no-op）；
内容全外置成可下载 JSON；多人只传信号不传文本。

我们 4 个 mod 可以直接抄它的三套写法 —— 见
`notes/杀戮尖塔2-拆解-弹幕尖塔-20261009.md`。
