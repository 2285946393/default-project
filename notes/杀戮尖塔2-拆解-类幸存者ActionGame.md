# 拆解 · 假如塔2是类幸存者（local.action_game）

> **工坊** `3804485262` · 内部 id `local.action_game` · **作者** 普通网友Cano · **版本** 0.1.12
> **依赖**：**零依赖**（不依赖 RitsuLib / BaseLib / ModConfig）· **形态**：纯 dll，**无 pck**
> dll 仅 **269KB** → 反编译 14,690 行 / **157 个类型** / **89 个 Harmony 补丁**
>
> **作者自述**：
> > 把《杀戮尖塔 2》原版的回合制战斗改成类幸存者模式。用 WASD 躲避攻击，
> > 瓦库自动出牌，撑过一段时间即胜利。目前仍在测试。
>
> **一句话**：它**没有重写战斗系统**，而是拿一堆**定时器**把"回合推进"改成了"定时推进"。
>
> **取证**：`projects/ref-actiongame/decompiled/local.action_game.decompiled.cs`。结论带行号。

---

## 一、★ 核心手法：把"回合"切碎成持续脉冲

这是整份 mod 最聪明的一招 —— **`RealtimeCombatTicker`（第 10909 行）**。
它挂在 Godot 的 `ProcessFrame` 上，用 5 个计时器驱动一切：

```csharp
private static double _drawTimer;      // 自动抽牌
private static double _energyTimer;    // 自动回能
private static double _enemyTimer;     // 敌人行动
private static double _hookTimer;      // 钩子脉冲
private static double _reviveTimer;    // 复活
private static bool _nextHookIsTurnStart = true;   // ★ 钩子交替
```

时间参数（`RealtimeCombatSettings`，第 10101 行起）：

| 参数 | 公式 / 值 | 默认（TurnLength=4s） |
|---|---|---|
| `DefaultTurnLengthSec` | **4.0**（范围 2.5 ~ 12.0） | 一个原版回合压缩成 4 秒 |
| `DrawIntervalSec` | `TurnLength × 0.1075` | ≈ **0.43 秒抽一张牌** |
| `EnergyIntervalSec` | `TurnLength × 0.155` | ≈ **0.62 秒回 1 点能量** |
| `EnemyIntervalSec` | `TurnLength × EnemyIntervalMultiplier` | 按遗物调整 |
| `HookPulseIntervalSec` | `TurnLength × 0.5` | 每 **2 秒**脉冲一次钩子 |

**最妙的是 `_nextHookIsTurnStart`**：每 2 秒触发一次钩子，而且 **turn-start 和 turn-end 交替**。
于是"回合结束时生效"的效果（中毒、能力、遗物）在实时模式下**会持续循环生效** ——
**不用改任何一个能力/遗物的代码**，它们自己就跑起来了。

> 这就是整份 mod 的哲学：**不改造系统，改造"时钟"。**

---

## 二、胜负条件：撑够时间就赢

`EncounterTimerSystem`（第 3658 行）：

```csharp
public static double LimitSec {
    get {
        string act = SpawnRules.CurrentActName();
        return act switch {
            "Hive"       => 25.0,
            "Glory"      => 30.0,
            "Overgrowth" => 20.0,
            _            => 20.0,
        };
    }
}
```

**按幕给不同的存活时长**（20 / 25 / 30 秒）。撑到点 → 胜利。

配套：`SpawningOpen`（刷怪是否开放）、`BlocksEarlyWin`（阻止提前结算）、
`WinCheckIntercept`(2423) / `DelayedWinCheckPatch`(2493) / `DelayedWinCheckWithTurnStatePatch`(2501)
—— 因为原版的"战斗结束判定"是回合制的，必须改成实时判。

---

## 三、把一个回合制战斗"动作化"要动哪些东西

### 3.1 WASD 移动（第 9036 行）

`PlayerWasdMoveSystem` + `MoveSpeedUtil`(7424) + `PlayerPoseSync`/`RemotePose`(8748/8750)
+ `CreatureFacing`(3011) + `CreatureYSortSystem`(3108)。

`CreatureYSortSystem` 是**按 Y 坐标排序遮挡**（俯视/等距视角的标配）—— 说明战场从"卡牌站位"
变成了**有纵深的场地**。

### 3.2 敌人变成动作游戏 AI（第 4093 行起）

```csharp
internal enum EnemyBehaviorKind { Chase, Charge, PredictiveCharge, Flank, Stationary }
private enum ChargePhase { Approach, Windup, Dash, Recover }   // 冲刺四阶段
```

- `EnemyChaseSystem`(4169) —— 追击
- `EnemyMeleeAttackSystem`(4986) —— 近战（带 `State`）
- `MonsterContactMove`(6879) —— **接触伤害**（`FallbackDamage = 5m`）
- `ProximityDamage`(9437) —— 距离判定伤害
- `EnemyAttackHud`(3964) —— 敌人攻击预警条（动作游戏必备）
- `KaiserCrabCombatSystem`(6290) —— 某个具体 Boss 的专用系统

`ChargePhase` 是标准的**动作游戏"蓄力→预警→冲锋→硬直"**节奏。这是手写了一套小 AI。

### 3.3 自动出牌（描述里的"瓦库自动出牌"）

`QuickPlaySystem`(9499) + `AutoCardSelectSystem`(444) + `AutoSortHandSystem`(616)：

```csharp
internal enum QuickPlayMode { All, BlockOnly, Off }   // 全自动 / 只自动打防御 / 关
```

还有 `PlayCardBypassPatch` / `EnqueueManualPlayPatch` / `AutoPlayBroadcastPatch` /
`AutoPlayWaitSpeedPatch` —— **绕过原版的"出牌等待玩家确认"流程**，让牌能瞬间打出去。

### 3.4 ★ 把"实时下会失控"的原版卡牌重写

这是最体现功力的一块。有些卡/遗物在实时循环里**会无限累积**，必须重写语义：

| 原版东西 | 处理 | 为什么 |
|---|---|---|
| 壁垒 Barricade | `BarricadeRewrite`(1089) + `BarricadeDisableBlockRetainPatch` | "格挡不消失"在实时循环里会**无限叠** |
| 保留（Retain） | `RetainRewrite`(12010) + `RetainReturnToHandPatch` | 手牌永留 → 手牌爆炸 |
| 精巧的计划 Well-Laid Plans | `WellLaidPlansRewrite`(14570) + `DisableFlushPatch` | 同上 |
| 小提琴 Fiddle | `FiddleRewrite`(5392) | 每回合抽卡的节奏失效 |
| 冰淇淋 Ice Cream | `IceCreamDisableEnergyRetainPatch`(11781) | 能量保留 → 无限能量 |
| 金字塔 Pyramid | `PyramidDisableHandRetainPatch`(11789) | 手牌不弃 |
| 君王之刃 | `SovereignBladeOrbitSystem`(12184) | 改成**环绕飞行的实体**（实时里的飞剑） |
| 充能球 | `OrbOrbitSystem`(7634) | 改成**环绕玩家**的球（`OrbManagerTweenLayoutPatch`） |
| 奥斯蒂 Osty | `OstyMoveSystem`(8186) | 宠物改成会跟着跑的单位 |
| 恶魔形态 | `SelfDamageDemonFormSystem`(12079) | 自伤改成持续掉血 |

→ **规律：凡是"每回合/永久保留"的机制，实时化后都要重新定义"什么时候结算"。**

### 3.5 解阻塞：把回合制的"等待"拆掉

`CombatPatches`(1790) 一整套：

```
SetUpCombatPatch / SetupPlayerTurnPatch / EndTurnPatch / DoTurnEndPatch / FlushPlayerHandPatch
SwitchToEnemyPatch / ExecuteEnemyTurnPatch / EndEnemyTurnPatch
EndTurnButtonClickPatch / EndTurnButtonInitPatch / EndTurnButtonRefreshPatch
```

原版这些地方会**停下来等玩家/等动画**；实时模式下必须让它**立刻返回**。
`TurnCycleBridge`(13609) 负责把"谁在什么时候算一个回合"重新接起来。

---

## 四、刷怪与掉落（幸存者的核心循环）

```
SpawnCatalog(13051) / ActPools(13053) / SpawnRules(13337)
internal enum CombatSpawnBand { Weak, Strong, EliteOrBoss }   // 按强度分档
private const float EarlyIntervalStartSec = 3.4f              // 开局 3.4 秒后开始刷
MonsterSpawnSystem(7238) / AutoSpawnSystem(749) / SpawnPin
GoldDropSystem(5836) + Coin(5836) + GoldDropTier(5827)        // 掉金币，走过去捡
```

**这就是"幸存者"的骨架**：`按幕分池 → 按强度分档 → 定时刷 → 打掉掉金币 → 捡`。

---

## 五、联机（这部分比单机难得多）

回合制联机只要同步"行动顺序"；**实时联机要同步每一帧的位置和伤害**。它写了：

```
ActionGameNet(92) / ActionGameNetSync(299) / ActionGameNetMessage(211)
CardAttackNetSync(1277) / CardPlayNetSync(1310) / CombatOutcomeNetSync(1717)
OrbitHitNetSync(7440) / PlayerStatsNetSync(8943) / HostSettingsNetSync(6247)
RemoteOrbitFxSync(11814) / PlayerPoseSync + RemotePose
```

→ 主机权威 + 广播：出牌、攻击、环绕物命中、玩家状态、姿态、设置，各自一套同步。

---

## 六、那些"改了玩法就必须跟着改"的地方

| 问题 | 处理 |
|---|---|
| **存档会坏**（回合制存档塞进实时逻辑） | `SaveRunSanitizePatch`(12038) / `SaveRunSerializeSanitizePatch` / `LoadRunSanitizePatch` |
| **实时特效应接不暇 / 掉帧** | `ReduceCombatVfxPatch`(11470) / `ReduceHitVfxCreatePatch` / `ReduceHitVignettePatch` / `ShuffleVfxMutePatch` / `DamageNumHalfScalePatch` |
| **原版快捷键会打架** | `VanillaHotkeyDisable`(14469) + `HotkeyManagerUnhandledInputPatch` / `ProcessHotkeyInputPatch` / `ProcessFkbInputPatch` / `InputManagerInitPatch` |
| **玩家在选牌时不能被打** | `PlayerChoicePause`(8405) + `PlayerChoiceBegunPausePatch` / `PlayerChoiceEndedPausePatch` |
| **需要一个暂停** | `EscPauseHotkey`(5310) |
| **敌人 hover 提示在实时里没意义** | `HideEnemyHoverTipsPatch` / `HideEnemyCreatureHoverTipsPatch` / `HideHandFullThoughtPatch` |

> 💡 注意 `ReduceCombatVfxPatch` 这一条 —— **和我们的特效线直接相关**：
> 实时战斗下"每张牌都播一套特效"会糊成一团，所以这个 mod 反而要**减特效**。
> 换玩法的时候，特效的"预算"也要跟着换。

---

## 七、UI

`TurnLengthHud`(13834)（剩余存活时间条）· `EnemyAttackHud`(3964) · `ControlsHintPopup`(2814)
（WASD 操作提示）· `EscPauseHotkey` · `LocalPlayerStateHudPatch`(6776) · `CombatScreenClamp`(2761)
（把战斗画面限制在屏幕内）· `BuffPopupVfxScalePatch` · `FloorBackgroundReplace`(5416)。

---

## 八、调试与其它

`DebugCheatSystem`(3260) + `DebugForcedMonster`(3252) —— 自带作弊/调试面板；
`ForcePullNextEventPatch` / `ForceEventRoomCtorPatch` / `ForceCreateRoomEncounterPatch` /
`ForcePullNextEncounterPatch` / `EventOptionAlwaysClickablePatch` —— 一整套**强制跳房间/事件**的调试口子；
`GameDifficulty { Easy, Normal, Hard }`；`BarricadeDescriptionPatch` / `RetainDescriptionPatch` /
`WellLaidPlansDescriptionPatch` / `RelicIntervalDescriptionPatch` —— **改了机制，卡面描述要同步改**
（这条和特效实战 ② 的"配置化"是同一个道理）。

---

## 九、我们能抄什么

| 能抄的 | 为什么值 |
|---|---|
| **★ 用定时器把回合制"切片"成实时** | 想改玩法又不想重写系统，这是成本最低的路；`_nextHookIsTurnStart` 交替脉冲是关键 |
| **把所有时间参数挂在一个 `TurnLengthSec` 上按比例派生** | 一个滑块控制整体节奏（4s 基准 → 抽牌 0.43s / 回能 0.62s / 钩子 2s），调平衡极方便 |
| **按幕给不同的结算条件**（20/25/30s） | 难度曲线用数据表达，不写死在逻辑里 |
| **"实时下会失控"的机制清单式处理** | 保留/壁垒/冰淇淋/金字塔——凡是"永久/每回合"的都要重定义 |
| **改了机制就同步改卡面描述** | 否则玩家看不懂自己手上的牌 |
| **`ReduceCombatVfxPatch`：换玩法要换特效预算** | 表现层跟着玩法走 |
| **存档清洗（Save/Load Sanitize）** | 动了核心玩法就必须做，否则老存档读进来必炸 |
| **89 个 Harmony 补丁的组织方式**（`CombatCompat.PatchAllSafe` + 每个系统一个 `EnsureHook()`） | 补丁多起来以后，模块化注册比散着写清楚得多 |
| **零依赖** | 证明"不改别人东西也能做大改造"（对比 CombatSolver 必须依赖 RitsuLib） |

---

## 十、横向对比：三个样本三种思路

| mod | 思路 | 代价 |
|---|---|---|
| **战斗路线求解器** | **影子世界推演**（镜像重实现一份，不碰本体） | 代码量爆炸（21 万行） |
| **鸣潮先古** | **往既有接口里塞内容**（`CustomAncientModel` + 确定性随机） | 受限于 BaseLib 给的口子 |
| **类幸存者** | **改时钟，不改系统**（定时器 + 89 个解阻塞补丁） | 要逐个处理"实时下会失控"的机制 |

→ **"往接口塞内容"最省，"改时钟"次之，"造影子世界"最贵但能做到最狠的事。**

---

*拆解产物：`projects/ref-actiongame/`（`decompiled/` 14,690 行 + 原包 manifest）*
