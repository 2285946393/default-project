# 凛冬尖塔 · 「点传送直接黑屏」修复（2026-10-06 21:05）

> 用户报障原话：「进入那个传送会直接黑屏。我建议设置成问号房间而不是宝箱房间，
> 然后传送那块的代码直接抄集成就行了，不用顾虑」

结论：**不是设计问题，是两个实打实的 bug 撞在一起**。日志里有完整现场，一次就定位了。

---

## 一、现场（游戏日志，不用猜）

`AppData/Roaming/SlayTheSpire2/logs/godot.log`：

```
[INFO] [FrostSpire] [裂隙点] 玩家点击 MapCoord (6, 0) → 进入凛冬裂隙。
[INFO] [FrostSpire] [裂隙] 入口坐标 = MapCoord (6, 0)（Act 0 / 楼层 1）
[WARN] [FrostSpire] [裂隙] 缓存原地图失败: The type initializer for 'FrostSpire.Portal.FrostRiftSession' threw an exception.
[ERROR] [FrostSpire] [裂隙] 进入失败（不影响原局）: System.TypeInitializationException
 ---> System.InvalidOperationException: RunSavedData key is already registered: FrostSpire::frost_rift_state
   at FrostSpire.Portal.FrostRiftSession..cctor()
   at FrostSpire.Portal.FrostRiftMapNode.EnterMapCoordPatch.Prefix(RunManager __instance, MapCoord coord)
   at MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen.TravelToMapCoord(MapCoord coord)
...
[ERROR] GameAction MoveToMapCoordAction 1 MapCoord (6, 0) completed with exception:
        System.AggregateException: ... (Object reference not set to an instance of an object.)
```

调用链：`NMapScreen.TravelToMapCoord` → 我们的 Harmony Prefix → `EnterRiftAsync`。

---

## 二、根因（两个，缺一不可）

### ★ 根因 1：存档槽 key 被注册了两次

`RunSavedDataStore.Register(key)` 的 **key 是全局唯一的**，注册第二次直接抛
`InvalidOperationException: RunSavedData key is already registered`。

要命的是——这个注册写在**静态字段初始化器**里，所以异常抛在 `.cctor()`（静态构造）里，
**整个类立刻变成不可用**（后续碰它的任何静态成员都再抛一次 `TypeInitializationException`）。

当时的代码：

| 文件 | 行为 |
|---|---|
| `FrostRiftMapNode.cs` | 自己注册了一份 `"frost_rift_state"` |
| `FrostRiftSession.cs` | **又注册了一份同名 key** |

谁先被碰到谁赢，另一个类的静态构造必炸。玩家点裂隙点 → `EnterRiftAsync` 第一步就崩。

**修法**：一个存档槽只在 `FrostRiftSession` 里注册一次；
`FrostRiftMapNode` 删掉自己的 `SavedData`，改调新加的
`FrostRiftSession.RememberRiftCoord(state, coord)` / `GetRiftCoord(state)`。

> 教训：**同一个 mod 里，RunSavedDataStore 的 key 必须集中管理**，
> 最好所有槽都在一个类里注册，别的类只调方法。

### ★ 根因 2：Harmony Prefix 返回 false 却没给 `__result` 赋值

`RunManager.EnterMapCoord` 的返回类型是 **`Task`**。Harmony 的 Prefix 返回 `false`
跳过原方法时，如果**不设 `__result`**，这个方法就返回 `default(Task)` = **`null`**，
调用方一 `await` 就抛 **NullReferenceException**。

当时的代码是 fire-and-forget：
```csharp
_ = FrostRiftSession.EnterRiftAsync(state);   // ← 自己的 Task 扔了，__result 留空
return false;
```
→ 日志里那条 `MoveToMapCoordAction ... NullReferenceException` 就是它。

**修法**：
```csharp
private static bool Prefix(RunManager __instance, MapCoord coord, ref Task __result)
{
    ...
    __result = FrostRiftSession.EnterRiftAsync(state);   // ★ 接回调用链
    return false;
}
```
顺带把 fire-and-forget 去掉了 —— 异步流程接回调用链后，异常不再「掉在地上」。

> 教训：**Harmony Prefix 返回 false 时，凡是「方法有返回值」就一定要处理 `__result`**。
> 返回 `Task` 的方法尤其危险，因为 null Task 在 `await` 处才炸，栈里看不到 Prefix。

### 为什么表现是「黑屏」而不是报错
Prefix 已经 `return false` → 原逻辑（`ExitCurrentRooms` / `ClearScreens` / 进房间）**全被跳过**，
但我们的替换逻辑在第一步就崩了 → 玩家既没进裂隙、也没进原房间，
地图屏还停在「正在移动」状态 → 界面卡死/全黑。

---

## 三、顺带修的三处（本来也会出问题）

### 3.1 入口点类型：宝箱房 → 问号房（用户建议，已采纳）

```csharp
node.PointType = MapPointType.Treasure;   // 旧
node.PointType = MapPointType.Unknown;    // 新
```

`MapPointType` 枚举（反编译核实）：
`Unassigned=0, Unknown=1, Shop=2, Treasure=3, RestSite=4, Monster=5, Elite=6, Boss=7, Ancient=8`

改的两个理由（写进注释了）：
1. **语义贴切**：问号 = 「是什么，进去才知道」，正好表示「通往另一张地图的门」，
   而且图标本来就是问号，比宝箱更像传送入口。
2. **兜底安全**：我们那个点是**凭空注入**的，**没有配套房间配置**。
   宝箱房需要真实宝箱数据，万一 Harmony 拦截失效走了原逻辑就会黑屏；
   `Unknown` 则由游戏自己 roll 成事件/商店/宝箱，每一种都有完整配置。

改动位置：`FrostRiftMapNode.OnMapGenerated` + `FrostRiftSession.RestoreRiftNode`（两处都要，后者是重建地图时的兜底路径）。

### 3.2 ★ 裂隙图的「终点」没写进 Grid → 玩家看不到出口

`FrostRiftActMap` 原来把起点/Boss/终点都用 `CreateSpecialPoint` 造（**不写 Grid**）。
反编译 `ActMap` 核实：

```csharp
public MapPoint? GetPoint(int col, int row)
{
    if (col == BossMapPoint.coord.col && row == BossMapPoint.coord.row) return BossMapPoint;
    if (col == StartingMapPoint.coord.col && row == StartingMapPoint.coord.row) return StartingMapPoint;
    return Grid[col, row];      // ← 其余一律从 Grid 拿
}
// 且 GetAllMapPoints() 是双层循环遍历 Grid
```

⇒ **起点/Boss 不写 Grid 是对的**（基类对这两个坐标特殊返回，原版 `StandardActMap` 也这么干），
但**终点两者都不是，不写 Grid 就 `GetPoint` 返回 null、地图屏也不画** → 玩家走完两层看不到出口。

**修法**：拆出 `CreateGridPoint`（写 Grid）与 `CreateSpecialPoint`（不写），
终点和遭遇层用前者，起点/Boss 用后者。跟集成战略事件的写法一致
（它也是 `CreateGridPoint` / `CreateSpecialPoint` 两个方法）。

### 3.3 过场缺失：`ClearScreens` 清了屏却没人淡入回来

照集成战略事件（工坊 3747637213）的树洞流程补全：

```csharp
await runManager.FadeOut();                    // public，直接调
SetMapScreen(riftMap, state);
await runManager.EnterRoom(new MapRoom());     // public（= ExitCurrentRooms + EnterRoomInternal）
ClearTravelingFlag();                          // ★ 见下
await runManager.FadeIn(true);                 // public
NotifyMapLocationChanged();                    // 反射 AfterMapLocationChanged（private）
```

**`ClearTravelingFlag` 为什么必须有**：我们是从**地图屏点击**进的
（`NMapScreen.TravelToMapCoord`），而原版 `EnterMapPointInternal` 的**最后一句**是
`NRun.Instance.GlobalUi.MapScreen.IsTraveling = false;`。
我们 Prefix 里 `return false` 跳过了整段原逻辑 → 这标志永远停在 `true`
→ 地图屏认为「还在移动中」，不刷新不响应。

另外补了 `RefreshLocationSynchronizers`（照集成）：
```csharp
RunManager.Instance.MapSelectionSynchronizer.OnLocationChanged(state.MapLocation);
RunManager.Instance.RunLocationTargetedBuffer.OnLocationChanged(state.RunLocation);
```

出裂隙那边**不需要 ClearScreens**（集成 `RestoreOriginalMapAndPersist` 也没清），
所以只要「淡出 → 进地图房 → 淡入」三拍，且只在真的要换房时才淡。

---

## 四、★ 一个通用的坑：publicized dll 骗人

我们编译引用的是 **publicized 版 sts2.dll**（把 `internal`/`private` 全刷成 `public`），
所以**光看它能编译通过，判断不出运行时能不能直接调**。

这次逐个核实了**游戏目录里的原版 dll**：

| 方法 | 原版可见性 | 结论 |
|---|---|---|
| `FadeIn(bool)` | **public** | 直接调 |
| `FadeOut()` | **public** | 直接调 |
| `EnterRoom(room)` | **public** | 直接调 |
| `ClearScreens()` | private | 反射 |
| `ExitCurrentRooms()` | private | 反射 |
| `AfterMapLocationChanged()` | private | 反射 |

> 教训：**判断可见性一律看原版 dll**（`<游戏目录>/data_sts2_windows_x86_64/sts2.dll`），
> 不看 `.godot/mono/temp/obj/.../PublicizedAssemblies/` 那份。

顺带删掉了原来没用上的 `FadeInMethod` / `EnterRoomInternalMethod` 两个反射字段。

---

## 五、改动清单

| 文件 | 改了什么 |
|---|---|
| `Portal/FrostRiftMapNode.cs` | 删重复注册的 SavedData；改调 Session 的存取方法；入口点类型→`Unknown`；Prefix 加 `ref Task __result` |
| `Portal/FrostRiftSession.cs` | 新增 `RememberRiftCoord`/`GetRiftCoord`（槽的唯一注册点）；重写进出裂隙过场（FadeOut/FadeIn/EnterRoom/清移动标志/通知位置）；新增 3 个辅助方法；删无用反射字段 |
| `Portal/FrostRiftActMap.cs` | 拆 `CreateGridPoint`/`CreateSpecialPoint`，终点改为写 Grid |

构建：**0 错 0 警**，已部署（`FrostSpire.dll` 21:04 / 129024 B）。

---

## 六、待验证（用户重启后）

- [ ] 地图上那个点变成**问号图标**（不再是宝箱）
- [ ] 点它 → **正常进入裂隙地图**（不再黑屏），能看到一条 3 列的上行路线
- [ ] 地图上**能看到终点**（之前看不到）
- [ ] 走完两层遭遇 → 到终点 → 出裂隙 → 回到原地图同一个位置
- [ ] 日志里不再有 `RunSavedData key is already registered` 和 `NullReferenceException`

> 查日志命令（Git Bash）：
> `grep -nE "裂隙|Exception" "C:/Users/有花无实/AppData/Roaming/SlayTheSpire2/logs/godot.log" | tail -30`
