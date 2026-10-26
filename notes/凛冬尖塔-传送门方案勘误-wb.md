# 凛冬尖塔 · 传送门方案勘误 + 双份情报合并

> 2026-10-06 深夜。**wb 逐行反编译核准 ZCode 16:58 那份《传送门方案-树洞机制拆解》**。
> 两份是同一个 mod（**集成战略事件 3747637213，Natsuki，海克斯同作者**），wb 独立读的是
> `lib/0.111.0/IntegratedStrategyEvents.dll`，读到了 ZCode 没读到的**方法体**，故能双向校验。
> 结论：**ZCode 的方向、方案、排期全部正确**；有 4 处事实性偏差需要修正，另有 3 处它没提但会影响动工。

---

## 一、ZCode 稿的 4 处偏差（wb 修正）

> ### ⚠️⚠️ 2026-10-06 17:10 本条勘误部分作废 —— wb 自己搞错了一处，在此自查更正
>
> **原「偏差 1」说 ZCode 把 `StartingMapPoint` 的点类型写成 `MapPointType.Ancient` 是"推测当事实"，这是 wb 判错了。**
>
> 事后查了 publicized `sts2.dll` 的 `MegaCrit.Sts2.Core.Map.MapPointType` 真值：
> ```
> 0 Unassigned / 1 Unknown / 2 Shop / 3 Treasure / 4 RestSite / 5 Monster / 6 Elite / 7 Boss / 8 Ancient
> ```
> **`(MapPointType)8` 就是 `Ancient`，`(MapPointType)7` 就是 `Boss`。ZCode 写的完全正确。**
>
> wb 错在哪：反编译的 **workshop mod dll 里没有枚举定义**（枚举在 sts2.dll 里），
> wb 看到 `(MapPointType)8` 这个裸 int 就推断"ZCode 是猜的"，
> **没有先去把枚举真值查出来就下了结论**。→ **这是本轮 wb 唯一一次实质性误判，记下。**
> 教训：**看到别人给了枚举名字而反编译只显示数字时，先查枚举再判断，别默认真值不可得。**

---

### 偏差 1 ❌ **（本条已作废，见上方自查更正）**

**ZCode 写**：`StartingMapPoint`（`MapPointType.Ancient`）

**实际源码**：
```csharp
StartingMapPoint = CreateSpecialPoint(3, 0, (MapPointType)8);   // 裸 8
BossMapPoint     = CreateSpecialPoint(3, 6, (MapPointType)7);   // 裸 7
```
反编译里是**未解出的裸 int**。`(MapPointType)8` 是"起点"，`(MapPointType)7` 是"BOSS"。
**它是不是 `Ancient` 没有证据** —— ZCode 这个"Ancient"是推测，写成了事实。

**影响**：凛冬尖塔要摆点时**必须先把枚举名字查出来**，不能照抄数字，更不能抄"Ancient"这个结论。
→ 查法：`ilspycmd -t MegaCrit.Sts2.Core.Map.MapPointType <publicized sts2.dll>`

补充：ZCode 提到的**摆点辅助方法名是对的** —— `CreatePathPoint` / `CreateBranchRow` / `AddChildPoint`
/ `CreateSpecialPoint` / `RollPointType` 确实存在，可直接照抄（我这边也确认了）。

---

### 偏差 2 ❌ 「2. 入口 = 事件选项」的入口方法

**ZCode 写**：选「踏入裂隙」→ `EnterProphetHornFragmentFromEvent(owner, …)`

**实际源码**：`EnterProphetHornFragmentFromEvent` 是**通往「先知角碎片」那个特殊终局**的专用入口
（`SpecialFinaleCoordinator.EnterProphetHornFragmentFromEvent`），**不是树洞的通用入口**。

**树洞真正的通用入口是**：
```csharp
IntegratedStrategyTreeHoleController.EnterFromEvent(Player owner)
IntegratedStrategyTreeHoleController.EnterFromEvent(Player owner, string destinationActName)
IntegratedStrategyTreeHoleController.EnterFromEvent(Player owner, string destinationActName, string stageLabel)
```
→ 它内部走 `TreeHoleEntryCoordinator.EnterFromEvent`，单人直通 `EnterFromEventDeferred`，
联机走 `IntegratedStrategyTemporaryMapAction.EnqueueTreeHoleEntry`。**这条才是要抄的主干。**

---

### 偏差 3 ❌ 「4. 出口协调」的钩子

**ZCode 写**：`[HarmonyPatch(typeof(RunManager), "EnterNextAct")]` → `HandleEnterNextAct`

**实际源码**：`HandleEnterNextAct` 确实存在，但它是 **`SpecialFinaleCoordinator`（终局专用）** 的一部分，
而且它**不是通过 `[HarmonyPatch]` 特性挂在 `EnterNextAct` 上的** ——
它是 `IntegratedStrategyTreeHoleController.HandleEnterNextAct(RunManager, ref Task)`，
被运行时的补丁框架（`IntegratedStrategyPatcher`）统一调度。

**树洞自己真正的出口钩子是这两个**（wb 逐行读到）：
```csharp
// ① 主出口：打完终点、领完奖、点 Proceed 时
[HarmonyPatch(typeof(RunManager), "ProceedFromTerminalRewardsScreen")]
→ IntegratedStrategyTreeHoleTerminalRewardsProceedPatch  (Postfix，包住原 task)

// ② 兜底：进任何房间时检查（防漏网）
RunManager.RoomEntered += OnRoomEntered
→ TreeHoleSessionManager.OnRoomEntered()
   if (CurrentRoom is MapRoom && 已访问过 TerminalCoord && !被抑制)
        RequestRestoreOriginalMap(...)
```

**影响**：ZCode 说的"出口协调器"要改成**上面这两个**。只做①会漏掉玩家在临时地图里乱走的情况；
只做②则体验生硬（会在进地图房的瞬间跳走）。

---

### 偏差 4 ⚠️ 「3. 快照存取」的一个隐晦点没写清

**ZCode 写**：快照"原地图/坐标/访问历史/ActFloor/Act 存档"，`SerializableActMap.FromActMap`。

方向对，但**漏了最关键的一句话**：`state.Map` 的 setter **是 public 的**，
`state.ClearVisitedMapCoordsDebug()` / `state.AddVisitedMapCoord(coord)` 也是**公开可调**的。

**这条为什么重要**：它意味着 **整个方案不需要反射改私有字段** ——
"换地图"是官方允许的操作，不是 hack。反射只用在调 `RunManager` 的私有方法（`ClearScreens`/`FadeIn`/
`ExitCurrentRooms`/`EnterRoomInternal`）上，那几个都是"过场动画"级别的东西，缺一个不致命。
**风险等级因此从"高危"降到"中"**。

---

## 二、ZCode 提了但我这边也要确认的 2 点

### ✅ 1. `IntegratedStrategyEventsInterop` 确实存在，确实是 public

```csharp
public static class IntegratedStrategyEventsInterop
{
    public static void RegisterSecretNodeSkipPredicate(Func<ActMap, bool> predicate);
    public static bool IsTemporaryMapActive();
    public static bool IsTemporaryMap(IRunState? runState, ActMap map);
    public static string? GetCurrentExtraActId(IRunState runState);
}
```
**ZCode 这条判断完全正确，而且价值比它写的还高**：
- `RegisterSecretNodeSkipPredicate` —— **这就是明写的"别的 mod 想放自己的临时地图，来我这儿登记"的口子**。
- `IsTemporaryMap(runState, map)` —— 凛冬尖塔若装了它，可以用这个方法**判断"我是不是正踩在它的临时地图上"**，避免两套地图补丁互相打架。
- ⚠️ **但要注意**：这是**单向粗粒度**的口子，只能"登记跳过谓词"，**不能替代我们自己的 session 管理**。
  凛冬尖塔**必须自己实现一套 session**，装了集成战略就额外调一次它的 Interop，没装也要能跑。

### ✅ 2. 「联机走 FromSyncedAction 模式，v1 建议单机」——正确

`TreeHoleEntryCoordinator.ShouldEnterDirectly(NetGameType)` 里 `(int)gameType == 1` 直通，
**单人确实走的是直通路径，不需要 NetGameAction**。ZCode 的 v1 单机建议成立。

---

## 三、ZCode 没提、但会影响动工的 3 点（补充）

### ★ 补充 A：临时地图**不换 Act**，只换 Map —— 所以**你不需要造新 Act**

```csharp
state.Map = treeHoleMap;   // 换了
// state.Act  没有动 —— ActFloor 也没动（快照存着，返回时才还）
```
Natsuki 的临时地图是**"挂在原 Act 上的一层"**。这对凛冬尖塔是**大利好**：
`ActModel` 是 sealed 的重灾区，如果必须造新 Act 这条路根本走不通。**现在确认不需要。**
→ 顺带：进临时地图**不需要** `EnterNextAct`，ZCode 那份方案里"打完 BOSS → EnterNextAct 还地图"
这个设计**可以简化掉** —— 直接在终点节点接上出口钩子就行。

### ★ 补充 B：`RestoreActRooms` 有一个必须照抄的 **max**，不抄会出真 bug

还原 Act 的 `RoomSet` 时**不是直接覆盖**，而是取 max：
```csharp
restored.normalEncountersVisited = Math.Max(restored.normalEncountersVisited, current.normalEncountersVisited);
restored.eliteEncountersVisited  = Math.Max(restored.eliteEncountersVisited,  current.eliteEncountersVisited);
```
**不这么做**：临时地图里打过的怪会被"忘记"，出去后可能重复遇到同一组遭遇。
凛冬尖塔如果在临时地图里改了怪池（我们要改），**这个坑一定会踩**。

### ★ 补充 C：存档层是**真命门**，比 ZCode 估的还重

ZCode 写"快照存取：**大，全案命门**"—— 判断对，但它的清单不完整。**实际要存 18 个字段**：
```csharp
int Version; long StartTime; bool PendingArchitectCompletion; string? ResumeRoom; string Kind;
int CurrentActIndex; string? ParentActId; int CurrentActFloor; MapCoord? CurrentMapCoord;
List<int>? CurrentMapPointHistoryCounts;
SerializableActMap? TemporaryMap;      List<MapCoord>? TemporaryUnmodifiableCoords;
SerializableActMap? OriginalMap;       List<MapCoord>? OriginalUnmodifiableCoords;
List<MapCoord>? OriginalVisitedMapCoords; List<int>? OriginalMapPointHistoryCounts;
int OriginalActFloor; SerializableActModel? OriginalActSave;
uint TreeHoleMapSeed;  string? StageLabel; string? DestinationActName; MapCoord TerminalCoord;
```
而且 `OriginalMap` **同时**存完整 map **和** `TreeHoleMapSeed` —— 这是**版本兼容兜底**：
map 对不上时用 seed 重建。

**⚠️ 还有一个 wb 也没解决的**：这些字段**怎么塞进 `SerializableRun`**，反编译没直出。
线索是 `ModEntry.Initialize()` 里那句 **`InjectSavedPropertyCaches()`** —— 名字高度像
"给类型注入一个可保存属性"，**大概率是 RitsuLib 的能力**。
→ 下一轮必须先查这个，否则阶段 3 开不了工。

---

## 四、两稿合并后的最终判断

| 问题 | 结论 |
|---|---|
| 用户要的 mod 是哪个 | **集成战略事件 3747637213（Natsuki）**，机制名「树洞 TreeHole」✅ 两稿一致 |
| 能不能做 | **能**，有已上线实盘样板 ✅ 两稿一致 |
| 核心手法 | 换 `RunState.Map`（public setter，无需反射改字段）+ session 快照 ✅ |
| 要不要改存档 | **要，且是最重的一环**（18 字段 + 塞进 SerializableRun 的方式待查）✅ |
| 要不要造新 Act | **不要，也不能**（ActModel sealed；实际也不需要）★ wb 补充 |
| 出口钩子 | `ProceedFromTerminalRewardsScreen` 主 + `RoomEntered` 兜底 ★ wb 修正 |
| 排期 | **M3.5（工事牌 M3 之后）** ✅ ZCode 建议，wb 同意 |

**排期我同意 ZCode**：工事牌是"房间里玩的东西"，传送门是"门面"。
但补一句 —— **传送门对玩家的感知提升远大于工事牌**（"进房间直接开打"的突兀感是你自己提的痛点），
如果实测后觉得当前玩法"没劲"，**可以先切传送门**。两条路的侦察都做完了，随时可切。

---

## 五、动工前必查（三件，按顺序）

1. **`SerializableRun` 怎么被塞入自定义持久化对象** ← 卡住阶段 3 的唯一硬阻塞
   - `ilspycmd -t IntegratedStrategyEvents.IntegratedStrategyPrivateMembers <dll>`
   - 再搜 RitsuLib 的 `SavedProperty` / `InjectSavedPropertyCaches` 对应 API
2. **`MapPointType` 枚举数值 → 名字**（偏差 1 的修复）
   - `ilspycmd -t MegaCrit.Sts2.Core.Map.MapPointType <publicized sts2.dll>`
3. **`RunManager` 四个私有方法签名**（`ClearScreens`/`FadeIn`/`ExitCurrentRooms`/`EnterRoomInternal`）
   - 决定"过场动画"这层能抄到什么程度

---

## 六、文件索引（本轮 wb 产出）

| 文件 | 内容 |
|---|---|
| `凛冬尖塔-传送门独立地图情报.md` | ★ wb 主稿，26054 B。逐行源码级拆解：进/出/存档/UI 全链路 + `ActMap` 网格构造代码 + 对接方案 + 三阶段落地 |
| `凛冬尖塔-传送门方案-树洞机制拆解.md` | ZCode 原稿，4819 B。方向与排期正确，4 处细节见本文第一节 |
| `凛冬尖塔-传送门方案勘误-wb.md` | 本文 |
