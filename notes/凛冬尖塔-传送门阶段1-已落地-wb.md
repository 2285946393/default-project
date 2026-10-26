# 凛冬尖塔 · 传送门阶段 1 —— 硬阻塞已解开 + 代码已落地

> 2026-10-06 17:10。**承接《凛冬尖塔-传送门独立地图情报.md》**。
> 上一份的最后一行是"存档怎么塞进 `SerializableRun` —— 反编译没直出，**这是阶段 3 的唯一硬阻塞**"。
> **这一份把它解开了，并且顺手把阶段 1 写完了、编译通过了、部署了。**

---

## 一、★ 硬阻塞解开：不用碰 `SerializableRun`，RitsuLib 有现成的

情报稿里那条线索（`InjectSavedPropertyCaches()`）**方向对了但找错了地方**。
真正的答案是 **RitsuLib 提供了一整套「跟着某一局存档走的自定义数据槽」**：

```csharp
using STS2RitsuLib.RunData;

// 1) 拿一个槽（ModId + key 唯一确定；返回 RunSavedData<T>）
private static readonly RunSavedData<MyState> SavedData =
    RunSavedDataStore.For("FrostSpire").Register(
        "my_state",
        () => new MyState(),
        new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

// 2) 用
SavedData.Set(state, myState);          // 写
SavedData.TryGet(state, out myState);   // 读（不创建）
SavedData.Get(state);                   // 读（没有就建默认）
SavedData.Remove(state);                // 删
SavedData.Modify(state, s => s.X++);    // 改 + 标记变更
```

### 核实过程（可复核）
- `IntegratedStrategyTreeHoleSaveStateStore` 第一行就是 `using STS2RitsuLib.RunData;`，
  字段是 `private static readonly RunSavedData<TreeHolePersistedState> SavedState;`。
- 在 `STS2-RitsuLib.Runtime.dll` 里找到命名空间 `STS2RitsuLib.RunData`（**注意：不在主 dll 里，在 Runtime.dll**）。
- 全量导出后确认 `RunSavedData<T>` 就是 5 个方法：`Get` / `TryGet` / `Set` / `Remove` / `Modify`。
- 同级还有 `PlayerRunSavedData<T>`（按玩家分槽）和 `RunSavedDataLobbyScope`（联机大厅暂存）。
- 官方自带两个用法样板：`SecondaryResourcePersistence.cs`、`ModCardPilePersistence.cs`。

### 这意味着什么（重要性）
**原方案里"Natsuki 用 18 个字段 + 一整套反射注入才搞定的存档层，我们用一个泛型槽就解决了。"**
- 不用反射。不用注入。不用管 `SerializableRun` 的结构。
- 序列化、读档恢复、联机贡献同步，全在 RitsuLib 里。
- **阶段 3 从"最大风险项"降级成"调一次 API"。**

### 附带确认的版本兼容机制
`RunSavedDataOptions` 有 `SchemaVersion` + `Migrations`（`IReadOnlyList<IMigration>`）——
这就是 Natsuki 手写 `Version` 字段 + `ReadLegacyState()` 那套的**库化版本**。我们直接用它。

---

## 二、★ 阶段 1 代码已落地（4 个文件，已编译部署）

### `FrostSpireCode/Portal/` 新增四件

| 文件 | 职责 | 行数级 |
|---|---|---|
| `FrostRiftSavedState.cs` | 跨存档状态（Version / Active / ClearedNodes / 原地图快照 / 入口坐标） | ~90 |
| `FrostRiftActMap.cs` | `: ActMap` 的独立小地图（3 列 × 2 层遭遇 + 起点/终点/BOSS） | ~140 |
| `FrostRiftSession.cs` | ★ 心脏：`EnterRiftAsync` / `ExitRiftAsync` / `RestoreOriginalMap` | ~260 |
| `FrostRiftExitPatch.cs` | 出口双保险（Harmony patch + 事件订阅） | ~120 |
| `OpenFrostRiftCard.cs` | v1 门把手：0 费「开启凛冬裂隙」卡 | ~70 |

### 裂隙地图的形状

```
         col 1     col 3     col 5
  row 0          [ 起点 ]              ← MapPointType.Ancient
  row 1    ① ──── ② ──── ③            ← 第 1 层遭遇（战斗/未知）
  row 2    ① ──── ② ──── ③            ← 第 2 层遭遇
  row 3          [ 终点 ]              ← MapPointType.Treasure（走完 = 出裂隙）
  row 4          [ BOSS ]              ← MapPointType.Boss（不走，占位用）
```

**和 Natsuki 的差别（有意为之）**：
- 它是 7×6 一整幕体量；我们 3×4 就够（一个小副本）。
- 它每层三个点互相全连（3×3=9 条线）；我们**只连自己那一列**（一条道走到黑），
  逻辑简单、图上干净、**不可能走死胡同**。
- 它藏起点/BOSS 图标（因为那道线要画）；我们的起点/BOSS 在正中轴线上，
  视觉上本来就自然，阶段 1 **不碰 UI**。

### 出口：双保险（★ 这里 wb 改了自己一版，也纠正了 ZCode）

| 路径 | 钩子 | 状态 |
|---|---|---|
| **① 主** | `RunManager.ProceedFromTerminalRewardsScreen`（Harmony Postfix） | ✅ 已核实：`public async Task`（sts2.dll:1424），包住原 task |
| **② 兜底** | `RunManager.RoomEntered` —— ★ **事件订阅，不是 Harmony 补丁** | ✅ 已核实：是 `public event Action`（sts2.dll:270），在 :1245 被 invoke |
| ~~③ 读档~~ | `RunState.FromSerializable` | ❌ **v1 不做**（见下） |

**★ wb 第一版代码打错了补丁目标** —— 把 `RoomEntered` 当成方法去 `[HarmonyPatch]`。
反编译确认它是**事件**，Harmony 补事件是打不上的 → 改成 `RunManager.Instance.RoomEntered += OnRoomEntered`，
在 `Entry.Initialize` 里 `FrostRiftExitPatch.Install()` 挂一次。**编译通过、已部署。**

### 阶段 1 明确**不做**的事（都是有意取舍）
1. **不做读档恢复** —— 裂隙期间存档退出 = 裂隙作废（`FrostRiftSavedState` 注释里写清了）。
   理由：Natsuki 那套读档逻辑（新鲜度校验 + 抑制标志 + ResumeRoom 三态 + 陈旧快照丢弃）
   是**整个 mod 最脏的一层**，而 v1 完全不需要。真的要做，`RunSavedData` 已经帮我们扛掉一半。
2. **不做地图上的"点"** —— 那是阶段 2 的 UI 活儿，不影响玩法体验。
3. **不做联机** —— 单人直通路径（`NetGameType` 判断那层）我们暂时不需要。
4. **不存整张原地图** —— 见下。

### ★ 一个重要的架构简化（比 Natsuki 省一半）
Natsuki 把 `OriginalMap` **整张序列化**存下来（`TreeHolePersistedState` 里的一大坨就为这个）。
我们**不存** —— 因为原地图是**由当前幕确定性生成的**，只要还在同一幕，
把地图重新拿一遍/重建一遍结果完全一致。

```csharp
// FrostRiftSession.RestoreOriginalMap 的降级链：
//   ① 从 state.Act.ToSave().SavedMap 取回 → new SavedActMap(...)
//   ② 拿不到 → Act.GenerateMap()（反射，名字待实测确认）
//   ③ 都不行 → 抛错（绝不用错地图）
```

**代价**：裂隙只允许**同一幕内**进出。跨幕的裂隙要另开入口。（我们本来也不做跨幕。）

### 顺手修的 ZCode 代码问题
`Cards/FrostCards.cs` 有一条 `CS8602` 警告（`combatState` 可能为空）。
**顺手补了 `if (combatState == null) return;`，现在 build 是 0 错 0 警。**
（真正原因是 `Owner.Creature.CombatState` 返回可空——不是 lambda 写法的问题。）

### 构建与部署实证
```
$ dotnet build FrostSpire.csproj
已成功生成。 0 个警告 0 个错误

部署：D:/.../Slay the Spire 2/mods/FrostSpire/FrostSpire.dll   59392 B   17:09
```
**反编译部署后的 dll 自证**（关键那行真的编进去了）：
```csharp
// FrostSpire.Portal.FrostRiftSession 的静态构造里：
RunSavedDataStore obj = RunSavedDataStore.For("FrostSpire");
SavedData = obj.Register<FrostRiftSavedState>("frost_rift_state", obj2, val);
```
Portal 命名空间 5 个类全部就位。

---

## 三、怎么测（静置一天后照这个走）

1. **先确认 mod 是开的**（`settings.save` 里 FrostSpire 曾经是 false —— 这是上轮的教训）。
2. 开一局，走到**地图界面**。
3. 控制台/调试：给玩家一张 `OpenFrostRiftCard`（v1 它还没有自动获取途径！见下"已知缺口"）。
4. **在地图上打这张卡** → 应当：过场 → 地图变成一张 3 列小图 → 站在起点。
5. 往上走 → 遇到战斗/事件 → 打完。
6. 走到 **终点（Treasure 图标）** → 领奖励 → 点「继续」。
7. **应当自动回到原地图**，并且落回你进裂隙前站的那个坐标。
8. 日志关键字：`[裂隙]` 全部。

### ⚠️ 已知缺口（阶段 1.5 要补）
**这张 `OpenFrostRiftCard` 现在没有获取途径** —— 它注册进了 `FrostCardPool`，
但不会自动出现在任何地方。三条补法（按省事排序）：
1. **挂到凛冬契约遗物上**：拿到契约时塞一张进手牌/牌库（最省事，且和现有 M2 流程咬合）。
2. 做成一张**可获得的稀有卡**（进奖励池，随机开出）。
3. 阶段 2 的"地图上的点"接上后，这张卡就退役。

---

## 四、下一步（阶段 2 / 2.5 / 3）

| 阶段 | 内容 | 风险 |
|---|---|---|
| **1.5** | 给 `OpenFrostRiftCard` 一个获取途径（建议挂契约遗物） | 极低 |
| **2** | 地图上真的放一个点：`ActMap.Grid` 塞自定义 `MapPoint` + `NMapScreen` 图标 + `RunManager.RollRoomTypeFor` Prefix | 中（UI 活儿） |
| **3** | 读档恢复（`RunSavedData` 已扛掉一半，只剩"新鲜度校验 + 恢复时机"） | 中低（**已不是阻塞**） |
| **M3** | 工事牌 8 张（ZCode 探明的爪牙体系） | 低（侦察完毕） |

**排期建议不变**：传送门先做到阶段 1.5 让**能玩**，M3 工事牌接着上；
阶段 2 的地图 UI 等到"玩家真的会反复用裂隙"再补。

---

## 五、本轮实证清单（可复核）

| 项 | 怎么核 |
|---|---|
| RitsuLib `RunSavedData<T>` API 真值 | `ilspycmd -p -o <dir> STS2-RitsuLib.Runtime.dll` → `STS2RitsuLib.RunData/RunSavedData.cs` |
| `MapPointType` 枚举真值 | `ilspycmd -t MegaCrit.Sts2.Core.Map.MapPointType <publicized sts2.dll>` |
| `MapCoord` 字段名小写 | `ilspycmd -t MegaCrit.Sts2.Core.Map.MapCoord <publicized sts2.dll>` → `public int col; public int row;` |
| `NMapScreen.SetMap` 是 3 参 | `RunManager` 第 835 行内部调用：`SetMap(map, State.Rng.Seed, clearDrawings: true)` |
| `RoomEntered` 是事件不是方法 | `RunManager` 第 270 行 `public event Action? RoomEntered;`，第 1245 行 `?.Invoke()` |
| `ProceedFromTerminalRewardsScreen` 存在 | `RunManager` 第 1424 行 `public async Task` |
| 我们的代码真的编进去了 | 反编译 `mods/FrostSpire/FrostSpire.dll` → 见 §二 末尾 |
