# 拆解 · 战斗路线求解器（CombatSolver）

> **工坊** `3790899961` · **作者** Torch · **版本** 0.50.1 · **依赖** `STS2-RitsuLib ≥ 0.6.0`
> **形态**：纯 dll（**无 pck**），7.4MB，另有 `CombatSolver.MemoryCleaner.exe`
> **许可**：MIT（`LICENSE` + `THIRD_PARTY_NOTICES.md` 都在包里）
>
> **一句话**：在游戏里跑一次**异步、限时、可随时中断**的**束搜索**，把当前这一整场战斗的
> 最优出牌路线算出来；算完可以"按路线执行"一次，也可以开全自动连续打完。
>
> **取证方式**：`ilspycmd 8.2.0` 反编译 → `projects/ref-combatsolver/decompiled/CombatSolver.decompiled.cs`
> （213,836 行 / 1504 个类型）。所有结论带行号。许可证与第三方声明见 `projects/ref-combatsolver/`。

---

## 一、它跟"随机数预测"是什么关系（先说清楚，容易搞混）

包里的 `THIRD_PARTY_NOTICES.md` 写得很老实：

> Combat Solver 的内置战斗模拟核心**使用并改造了** Random Foreseer 的部分实现，现已获得原作者的许可。

- Random Foreseer（作者 **hotwords123**，**MIT**，GitHub `hotwords123/StS2.RandomForeseer`，工坊 `3747531952`）
  是个**随机数预测** mod —— 它提前显示"这个随机数状态下会得到什么"，但**不推进真实随机数**。
- CombatSolver 拿的是它那套 **"在不动游戏真实状态的前提下把战斗推演一遍"** 的能力，
  在上面盖了一个**搜索**（不只走一条线，而是**搜一整棵路线树**）。

⚠️ 但**运行时并不依赖 Random Foreseer 的程序集**。启动日志里自己写了：`simulation_engine=embedded rf_dependency=false`
（第 23466 行）。所以你可以只装 CombatSolver，不装 Random Foreseer。

---

## 二、代码分层（`CombatSolver.decompiled.cs`）

```
CombatSolver/              Entry（[ModInitializer]）、设置、性能录制、风险台账、Bug 上报
CombatSolver.Api/          PreCombatForecastApi / PreCombatForecastWorker / PreCombatRunSerialization  ← 对外异步接口
CombatSolver.Replay/       AppendOnlyEventLog / CheckpointArchive  ← 可重放的事件日志 + 检查点存档
CombatSolver.Engine.Common/          PredictionStateStore / PredictionForkContext / PredictionRisk / PredictedCard
CombatSolver.Engine.InCombat.Simulation/   CombatPredictionSimulator ← 核心模拟器
CombatSolver.Engine.InCombat.Mirrors/      ★ 最大的一坨：把游戏机制"镜像重实现"一份
CombatSolver.Engine.InCombat.Extensions/   扩展方法
```

### 2.1 `Mirrors`（镜像层）—— 这个设计是整份代码的骨架

`Mirrors` 下面按游戏机制分了子命名空间：

| 子命名空间 | 镜像的东西 |
|---|---|
| `Mirrors.Cards` / `Cards.OnPlay` | 卡牌本体 + 打出时效果 |
| `Mirrors.Potions.OnUse` | 药水使用 |
| `Mirrors.Orbs` | 充能球 |
| `Mirrors.Enchantments.OnPlay` | 附魔 |
| `Mirrors.Afflictions.OnPlay` | 苦难（诅咒类） |
| `Mirrors.Hooks.{TurnStart,TurnEnd,Damage,Block,Attack,Card,Death,Orb,Resources}` | **游戏的钩子体系**逐类镜像 |

**为什么要这么干**：要搜索就得分叉状态、反复试错。但你不能真的去驱动游戏本体——
那会把真实存档搞乱、还会触发一堆表现层动画。所以它把每个机制**照抄一份能在纯数据上跑的版本**，
让模拟器在"影子世界"里跑。

### 2.2 模拟器与状态

- `CombatPredictionSimulator`（189446 行）：核心
- `CombatPredictionState`（193290）/ `SimCreatureState` / `SimPlayerCombatState` / `SimOrbQueue` / `SimCardPile`
- `CombatPredictionHistory`（187836）+ 一串 `CombatPrediction*Entry`
  （`CardPlayStarted` / `DamageReceived` / `OrbChanneled` / `CardDrawn` / `CardGenerated` / `CardAfflicted` …）
  → **把推演过程记成事件流**，这样路线能回放、能给别人看、能存成检查点。
- `PredictionForkContext`（208312）+ `IPredictionStateForkable` / `IPredictionForkBoundary`
  → **这就是搜索树的"分叉"**：把某个状态复制一份继续往下试。

---

## 三、搜索算法（这份是从它自己的启动日志里读出来的，等于作者自述）

`Entry.Initialize()` 里有一整条自述日志（**第 23466 行**，原文就是一行超长 key=value），摘关键的：

### 3.1 搜索形态

| 参数 | 值 | 意思 |
|---|---|---|
| `search_session` | `single_anytime` | **Anytime 搜索**：时间到了就返回"目前为止最好的"，不等穷举 |
| `search_budget` | `120s` | 默认预算；档位 `low_60s / medium_120s / high_180s / very_high_300s / custom` |
| `search_beam` | `60` | **束宽 60** |
| `beam_lanes` | `balanced_defense_offense_utility_pareto_delayed` | **多条"泳道"并行搜索**：均衡 / 防御 / 进攻 / 效用 / 帕累托 / 延迟 |
| `beam_partition` | `unified` | 束怎么分给各泳道 |
| `horizon` | `time_or_node_budget` | 终止条件 = 时间或节点预算，谁先到算谁 |
| `state_store_fork` | `eager` | 分叉时机：预先复制（而不是写时复制） |
| `fork_context` | `pooled` | 分叉上下文走对象池，省 GC |
| `search_state_key` | `dual_u64` | 状态去重键 = 两个 u64（省内存、比对快） |
| `cross_turn_reuse` | `exact_state_text` | ★ **跨回合复用**：上下回合状态一致时直接复用已搜到的结果 |
| `full_auto` | `true` | ★ 全自动连续执行 |

### 3.2 剪枝（Anytime 要快，全靠这里）

```
exact_cycle_pruning=true                    精确环路剪枝
repeatable_no_progress_cycle_pruning=16     可重复但无进展的环，重复 16 次就砍
dominance_pruning=true                      支配剪枝（A 全面优于 B 就砍 B）
duplicate_branch_pruning=true               重复分支去重
snapshot_reuse=true                         快照复用
max_actions_per_turn=unbounded              单回合动作数不设上限
```

### 3.3 目标函数（"怎么算最优"）

```
sold_hp_basis=route_loss_minus_minimum_reachable_loss
sold_hp_tracking=cumulative_excess_loss
```

翻译成人话：**先算出这场战斗"理论最少要掉多少血"（最低可达损失），
再拿实际路线的掉血量去减它** —— 多掉的那部分才是"你这条路线差在哪"。
这个口径叫"卖血"（sold hp）。

### 3.4 不抢帧的设计（值得抄）

```
auto_search_min_frames=3        至少等 3 帧再开始搜，避免玩家看不到回合
background_slice_ms=4           后台每片只跑 4ms
yield_check_interval=16         每 16 次检查让一次
background_yield=adaptive_frame_recovery+thread_yield
```

→ **搜索跑在后台切片里，绝不让它把主线程帧率拖下去**。这是"游戏内跑重型计算"的正确答案。

---

## 四、三态药水策略（你问的那个"三态"）

代码里是两套枚举：

```csharp
// 第 90169 行 —— 每一格药水的"指令"，这就是三态
internal enum SolverPotionDirective { Smart, Force, Disabled }

// 第 39587 行 —— 整场的药水总策略
internal enum SolverPotionPolicy { Disabled, Smart, RequireAtLeastOne }

// 第 90175 行 —— 一键预设
internal enum PotionStrategyPreset { AllSmart, AllProtected, AllForced, OnlyForced }
```

翻译：

| 枚举 | 含义 |
|---|---|
| **Smart** | 交给求解器自己判断"这瓶该不该用、什么时候用" |
| **Force** | **强制必须用掉**（搜索找不到能用它的路线就报错 `PotionPolicyUnsatisfiedException`） |
| **Disabled** | **保护起来，绝对不许用**（留给后面的战斗） |

再加总策略 `RequireAtLeastOne`（这场至少用一瓶）。

**两个很妙的参数**：

```
potion_beam_opportunity_cost_hp=18   把"用掉一瓶药"折算成 18 点血的机会成本
potion_min_hp_saved=9                用一瓶至少要"省下 9 点血"才值得
```

→ 它不把药水当"免费的"，而是**给它标一个血价**，让搜索在同一把尺子上比较"用药 vs 硬吃伤害"。

相关类型：`PotionStrategySnapshot`(90184) / `SolverPotionPolicy` / `ForcedPotionUseEvaluation`(90183) /
`PotionSlotDirective` / `PotionPolicyUnsatisfiedException`。

---

## 五、内存工程（这份代码最"硬"的地方）

213k 行、1504 个类型、按 60 束宽 × 多泳道搜索 —— 内存是头号敌人。它做了三件事：

### 5.1 自己接管 GC

```
gc_latency=combat_scoped_no_gc_region_or_clr_default   ← 战斗中不触发 GC
gc_budget=independent_configurable_default_16gb_retained_when_disabled
gc_partition=soh_five_sixths+loh_one_sixth             ← 小对象堆 5/6、大对象堆 1/6
gc_disabled=steady_upstream_clr_no_new_automatic_policy
gc_auto_reclaim=no_gc_only_background                  ← 自动回收只在后台回合间做
gc_manual_reclaim=both_modes_lifecycle_safe
```

先给一大笔预算（默认 **16GB**）**换掉战斗中卡顿**，再在回合间歇统一回收。
`RuntimeGcStartup.Prepare(...)`（第 23397 行）在入口就配好了。

### 5.2 一个外挂 exe 来"深度释放系统内存"

`SystemMemoryReleaseService`（**第 40524 行**）：

```csharp
private const string HelperFileName = "CombatSolver.MemoryCleaner.exe";
...
ProcessStartInfo startInfo = new ProcessStartInfo {
    FileName = text,
    UseShellExecute = true,
    Verb = "runas",                    // ★ 请求管理员权限（UAC 弹窗）
    WindowStyle = ProcessWindowStyle.Hidden
};
```

- 只支持 Windows，`catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)`
  → 1223 = **用户取消了 UAC**，它把它翻译成 `OperationCanceledException("玩家取消了管理员权限请求")`。
- 为什么需要提权：.NET 把内存还给堆之后，**Windows 的待机内存（standby list）不会立刻回收**。
  要真正清掉得提权调系统 API —— 所以它干脆外挂一个小 exe 专门干这事。

> 💡 这条对**我们自己**有参考价值：重型 mod 光靠 `GC.Collect()` 是**不够**的，
> 系统那一层要另想办法。它也说明了为什么"内存占用"会成为这类 mod 的卖点。
> （UI 里就有 `SolverMemoryUsageBar` + `MemoryPressureTone` / `MemoryDisplayState` 两个枚举，
> 把内存状态直接摆给玩家看。）

---

## 六、怎么跟"别人的 mod"共存（社区 mod 最难的一关）

这游戏 mod 生态很活跃（本机工坊就有 **121 个**），求解器必须能处理**别的 mod 加进来的卡牌/遗物/能力**。
它做了：

| 类型（行号） | 作用 |
|---|---|
| `PredictionModModelSupport` (208574) | 把别的 mod 的模型接进推演 |
| `BaseLibCardModifierAdapter` (208576) | 适配 BaseLib 系的卡牌修改器 |
| `PredictionModPatchAudit` (14249) | **审计**别的 mod 打了哪些补丁 |
| `PredictionModHookSubscriberCapture` (13637) | 抓取钩子订阅者 |
| `PredictionModHookSubscriberInertness` (14171) | ★ 让别的 mod 的钩子在**推演里变成"惰性"**（只记不动） |
| `IncompatibleGameplayModException` (7552) | 实在不兼容就明确报错，而不是算错 |
| `BaseLibCloneConcurrencyPatch` (16768) | 修 BaseLib 克隆时的并发问题 |

**`Inertness`（惰性化）这个思路很关键**：别的 mod 的钩子本来会改真实状态；
在推演里必须让它"登记一下但不生效"，否则一搜索就把存档搞脏了。

---

## 七、界面（它不是一个后台黑盒）

`Solver*` 一大家子，是游戏内的面板：

- `SolverOverlay` (171997) —— 主浮层（可拖拽 `overlay_draggable=true`，坐标按视口存）
- `SolverActionBar` / `SolverActionPill` / `SolverRouteActionFlow` / `SolverRouteRow`(179456)
- `SolverPotionStrategyPanel`(177587) / `SolverGrowthStrategyPanel`(169488) / `SolverRelicStrategyPanel`(178429)
- `SolverMemoryUsageBar`(170915) / `SolverLoopGroup` / `SolverDetailsButton` / `SolverSettingsPanel`
- `SolverUiTokens` + `Spacing`/`Radius`/`Type`/`Size`/`Palette` —— **自己一套设计 token**（换肤靠它）

自述日志里那一大串 `..._ui_...` / `..._badge...` / `..._highlight` 就是它的界面开关，
数量级在**上百个**（`action_badges` / `semantic_action_pills` / `three_column_routes` /
`full_kill_highlight` / `battle_hp_in_route_heading` / `type_scale=12_13_14_15` / `minimum_font_size=12` …）。

---

## 八、全自动与离线（`Unattended*` 一大套）

想"连续全自动执行"就得能无人值守地把一整段跑完：

- `UnattendedTestRunner` / `UnattendedTestRequest` / `UnattendedCardSelector` / `PlannedCardSelector`
- `UnattendedHeadlessFtuePatch`（跳过新手引导那些必须点的地方）
- `UnattendedTestIsolationPatch`（隔离，别污染真实存档）
- 各种注入器：`UnattendedCardInjection` / `PotionInjection` / `RelicInjection` / `OrbInjection` / `PowerInjection`
- 指标：`UnattendedSolverMetrics` / `UnattendedStageTiming` / `UnattendedEarlyTurnExplorationMetrics`

还有 `PreCombatForecastWorker`（211773）——**能开一个独立进程去搜**：
`Entry.IsPreCombatWorker` 读环境变量 `COMBATSOLVER_PRECOMBAT_WORKER=1` 判断自己是不是那个 worker（第 23382 行）。

---

## 九、我们能抄什么

| 能抄的 | 为什么值 |
|---|---|
| **Anytime + 时间预算** | 重计算不能阻塞游戏："跑 120 秒，随时给我当前最好的"。比赛"算到完"实用得多 |
| **束 + 多泳道**（防御/进攻/效用/帕累托） | 单一评价函数容易陷局部最优；多泳道各自保留最优再合并 |
| **后台切片**（4ms 一片 + 每 16 次让一次） | 游戏内跑重计算的正确姿势 |
| **状态分叉 + pooled 上下文** | 搜索树必备；对象池是省 GC 的关键 |
| **状态去重键用 two-u64** | 便宜、够用 |
| **给资源标"机会成本"**（药水 = 18 血） | 让搜索用统一尺度比较不同资源 |
| **跨回合复用**（状态文本一致就复用） | 省掉大量重复搜索 |
| **把机制"镜像重实现"一份** | 只要你想"推演但不真跑"，这是唯一干净的路 |
| **让别人的补丁"惰性化"** | 多 mod 共存下安全推演的核心 |
| **内存：先给预算换流畅，再回合间统一回收** | 比"到处 GC.Collect()"可控 |

---

## 十、和其它拆解的关系

- 它**依赖 RitsuLib**（`RitsuLibFramework.CreatePatcher` / `ApplyRequiredPatcher` / `SubscribeLifecycle`）
  → 这是本机 **4 个未拆 mod 之一**，值得单独拆。
- 它大量使用 `MegaCrit.Sts2.Core.*`（本机 `sts2.dll`）与 BaseLib。
- 与特效线的交集：`Mirrors.Hooks.*` 的思路（**照抄一份不改本体**）和特效手册里
  "表现层不阻塞游戏 / 自己在影子层跑"是同一套工程哲学。

---

*拆解产物：`projects/ref-combatsolver/`（`decompiled/` 213,836 行 + 原包 LICENSE / THIRD_PARTY_NOTICES / manifest）*
