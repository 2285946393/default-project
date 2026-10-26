using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.RunData;

namespace FrostSpire.Portal;

/// <summary>
/// 凛冬裂隙会话管理——**传送门方案的心脏**。
///
/// ★ 核心手法（从集成战略事件 3747637213 逐行学来）：
///   **直接换 `RunState.Map`**。`RunState.Map` 的 setter 是 public，
///   `ClearVisitedMapCoordsDebug()` / `AddVisitedMapCoord()` 是公开方法 ——
///   **换地图不需要反射改私有字段，这是官方允许的操作**，不是 hack。
///   反射只用在调 `RunManager` 的私有方法（过场动画那四个），缺一个不致命。
///
/// 进裂隙 （EnterRift）：
///   1. 快照原地图三件套（ActIndex / ActFloor / 当前坐标）
///   2. 造 FrostRiftActMap，换掉 state.Map
///   3. 清空访问记录、写上起点
///   4. 存进 RitsuLib 的 RunSavedData 槽（跨存档）
///   5. 进 MapRoom（过场）
///
/// 出裂隙 （ExitRift）：
///   1. 把 state.Map 换回原地图 —— ★ 原地图由"当前幕"重新拿（见 RestoreOriginalMap）
///   2. 清访问记录 + 还原
///   3. 把玩家放回进裂隙时那个坐标
///   4. 清空存档槽
/// </summary>
internal static class FrostRiftSession
{
    // ——— RitsuLib 的「跟着这局存档走的数据槽」———
    // ★ 这是整个方案的存档答案：不用自己碰 SerializableRun。
    //   反编译 RitsuLib 确认 API：RunSavedDataStore.For(modId).Register(key, factory, options)
    //   → RunSavedData<T>.Get/TryGet/Set/Remove/Modify，序列化与读档全自动。
    private static readonly RunSavedData<FrostRiftSavedState> SavedData =
        RunSavedDataStore.For(Entry.ModId).Register(
            "frost_rift_state",
            () => new FrostRiftSavedState(),
            new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });

    // ——— RunManager 的私有方法（只能反射调；拿不到就降级，不影响玩法）———
    //  ★ 可见性已逐个核实（看的是**游戏目录里的原版 sts2.dll**，不是 publicized 那份，
    //    因为 publicized 会把 internal/private 全刷成 public，照它写会在运行时炸）：
    //      ExitCurrentRooms         private
    //      ClearScreens             private
    //      AfterMapLocationChanged  private
    //      FadeOut / FadeIn / EnterRoom  →  **public，直接调，不用反射**
    private static readonly MethodInfo? ExitCurrentRoomsMethod =
        AccessPrivateMethod(typeof(RunManager), "ExitCurrentRooms");

    private static readonly MethodInfo? ClearScreensMethod =
        AccessPrivateMethod(typeof(RunManager), "ClearScreens");

    private static readonly MethodInfo? AfterMapLocationChangedMethod =
        AccessPrivateMethod(typeof(RunManager), "AfterMapLocationChanged");

    private static MethodInfo? AccessPrivateMethod(Type type, string name, params Type[] parameters)
    {
        try
        {
            return type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, parameters, null);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 找不到私有方法 {type.Name}.{name}: {e.Message}");
            return null;
        }
    }

    /// <summary>当前是不是正站在裂隙地图上。</summary>
    public static bool IsActive(RunState? state)
        => state != null && state.Map is FrostRiftActMap;

    // ★ 进裂隙那一刻的「原地图」对象引用（内存缓存，不落盘）。
    //   为什么需要它：`ActModel.ToSave().SavedMap` 恒为 null（见 RestoreOriginalMap 的注释），
    //   而重新生成的地图**不含我们注入的裂隙点**。留个引用最准、最省事。
    //   用 ConditionalWeakTable 而不是 Dictionary：键是 RunState，
    //   用弱引用表能让"这局结束"后条目自动被 GC 回收，不用手动清理、也不会拖住 RunState。
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<RunState, ActMap> OriginalMaps = new();

    /// <summary>记下进裂隙前的原地图。</summary>
    private static void StashOriginalMap(RunState state, ActMap original)
    {
        try { OriginalMaps.Remove(state); OriginalMaps.Add(state, original); }
        catch (Exception e) { Entry.Logger.Warn($"[裂隙] 缓存原地图失败: {e.Message}"); }
    }

    /// <summary>取回进裂隙前的原地图（没有则 null）。</summary>
    private static ActMap? TakeStashedMap(RunState state)
    {
        if (OriginalMaps.TryGetValue(state, out var map))
        {
            OriginalMaps.Remove(state);
            return map;
        }
        return null;
    }

    public static bool TryGetState(RunState state, out FrostRiftSavedState saved)
        => SavedData.TryGet(state, out saved);

    // =====================================================================
    //  「裂隙入口点坐标」的存取
    //
    //  ★★ 必须集中在本类 —— 踩过的坑（实测黑屏的根因）：
    //     `RunSavedDataStore.Register(key, ...)` 的 key 是**全局唯一**的，
    //     同一个 key 注册第二次会抛 `InvalidOperationException: RunSavedData key is already registered`。
    //     而这个注册写在**静态字段初始化器**里 → 抛在 `.cctor()`（静态构造）里 →
    //     **整个类直接变成不可用**，之后碰它的任何静态成员都再抛一次 TypeInitializationException。
    //
    //     当时 `FrostRiftMapNode` 和 `FrostRiftSession` **各自注册了同一个 key**
    //     （`"frost_rift_state"`）→ 谁先被碰谁赢，另一个类的静态构造必炸。
    //     玩家点裂隙点时：Harmony Prefix 已 `return false`（原逻辑被跳过），
    //     但 `EnterRiftAsync` 在第一步就抛 → 传送没发生、原逻辑也没走 → 游戏卡死在半路 → **黑屏**。
    //
    //     ⇒ 规矩：**一个存档槽，只在 FrostRiftSession 里注册一次**，
    //       别的类要用就调下面这两个方法，绝不自己 Register。
    // =====================================================================

    /// <summary>把「裂隙入口点坐标」写进本局存档槽（地图生成时调用）。</summary>
    public static void RememberRiftCoord(RunState state, MapCoord coord)
    {
        try
        {
            var saved = SavedData.TryGet(state, out var s) ? s : new FrostRiftSavedState();
            saved.SetRiftNodeCoord(coord);
            SavedData.Set(state, saved);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙点] 记入口坐标失败: {e.Message}");
        }
    }

    /// <summary>取本局的裂隙入口点坐标（没有 → null，即"本局没有裂隙点"）。</summary>
    public static MapCoord? GetRiftCoord(RunState state)
    {
        try
        {
            if (!SavedData.TryGet(state, out var saved)) return null;
            return saved.GetRiftNodeCoord();
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙点] 读入口坐标失败: {e.Message}");
            return null;
        }
    }

    // =====================================================================
    //  进裂隙
    // =====================================================================

    /// <summary>
    /// 把玩家送进凛冬裂隙。★ 这是"进门"的唯一入口。
    /// 全程 try/catch —— 传送门炸了也绝不能把整局游戏搞崩。
    /// </summary>
    public static async Task EnterRiftAsync(RunState state)
    {
        if (IsActive(state))
        {
            Entry.Logger.Info("[裂隙] 已经在裂隙里了，忽略重复进入。");
            return;
        }

        try
        {
            var runManager = RunManager.Instance;

            // ① 快照原地图三件套
            var saved = new FrostRiftSavedState
            {
                Active = true,
                ClearedNodes = 0,
                OriginalActIndex = state.CurrentActIndex,
                OriginalActFloor = state.ActFloor,
                RiftMapSeed = (uint)state.Rng.Seed.GetHashCode(),
            };
            saved.SetEntryCoord(state.CurrentMapCoord);
            Entry.Logger.Info($"[裂隙] 入口坐标 = {state.CurrentMapCoord}（Act {saved.OriginalActIndex} / 楼层 {saved.OriginalActFloor}）");

            // ② 造裂隙地图并换上
            //    ★ 换之前先把原地图**对象引用**留一份（出裂隙时用，见 RestoreOriginalMap）。
            //      必须在 `state.Map = riftMap` 之前拿，否则拿到的就是裂隙图了。
            StashOriginalMap(state, state.Map);

            var rng = new Rng((ulong)saved.RiftMapSeed, "frostspire_rift_map");
            var riftMap = FrostRiftActMap.Create(rng);

            // ③ 清场。
            //    ★ 顺序照抄原版 `RunManager.EnterMapPointInternal`（点普通地图点的那条路）：
            //       先 `ExitCurrentRooms()` 再 `ClearScreens()`。两个都是 private，走反射。
            await InvokeAsync(ExitCurrentRoomsMethod);
            InvokeSync(ClearScreensMethod);

            state.Map = riftMap;
            state.ClearVisitedMapCoordsDebug();
            state.AddVisitedMapCoord(riftMap.StartingMapPoint.coord);

            // ④ 落盘（RitsuLib 槽；下次读档就知道"我在裂隙里"）
            SavedData.Set(state, saved);

            // ⑤ 刷新"定位同步器"（照集成 TreeHoleSessionManager.RefreshLocationSynchronizers）。
            //    不刷的话顶栏/地图上的"当前所在点"还指着原地图的坐标。
            RefreshLocationSynchronizers(state);

            // ⑥ 过场：淡出 → 指地图屏 → 进地图房 → 清"移动中"标志 → 淡入 → 通知位置变化
            //    ★★ 这一段是「进传送黑屏」的修复核心，逐条说明为什么每一句都不能少：
            //      · FadeOut/FadeIn：原版换房是「淡出 → 清屏 → 进房 → 淡入」四拍。
            //        我们只清了屏没淡入 → **屏幕一片黑**。（`RunManager.FadeOut/FadeIn` 是 public，直接调。）
            //      · EnterRoom(MapRoom)：`MapRoom.EnterInternal` 会重建 NMapRoom 并 SetCurrentRoom，
            //        界面是靠它建起来的。`EnterRoom` 是 public（= ExitCurrentRooms + EnterRoomInternal）。
            //      · ClearTravelingFlag：我们是从**地图屏点击**进来的
            //        （调用链 `NMapScreen.TravelToMapCoord` → `RunManager.EnterMapCoord`），
            //        原版 `EnterMapPointInternal` 结尾会把 `MapScreen.IsTraveling` 置回 false；
            //        我们 Prefix 里 `return false` 跳过了原逻辑 → 这标志一直是 true →
            //        地图屏永远卡在"正在移动"，看起来就是卡死/黑屏。
            //      · NotifyMapLocationChanged：原版同一处的 `AfterMapLocationChanged()`，
            //        通知各处"玩家位置变了"（顶栏、房间图标、音乐…）。
            await runManager.FadeOut();
            SetMapScreen(riftMap, state);
            await runManager.EnterRoom(new MapRoom());
            ClearTravelingFlag();
            await runManager.FadeIn(true);
            NotifyMapLocationChanged();

            // ⑦ ★ 发「战旗单位卡」——用户要求：进了传送点才给。
            //    （没点裂隙点的玩家牌库干净，完全等于原版尖塔。）
            await GrantStarterCardsAsync(state);

            Entry.Logger.Info(
                $"[裂隙] 已进入凛冬裂隙（{FrostRiftActMap.EncounterRows} 层 = " +
                $"{FrostRiftActMap.BattleCount} 场大战斗 + {FrostRiftActMap.InterludeCount} 个幕，终点 {riftMap.TerminalCoord}）。");
            Entry.Toast(
                $"踏入裂隙：{FrostRiftActMap.BattleCount} 场大战斗、{FrostRiftActMap.InterludeCount} 个补给幕，走到底即可返回。",
                "凛冬尖塔");
        }
        catch (Exception e)
        {
            Entry.Logger.Error($"[裂隙] 进入失败（不影响原局）: {e}");
        }
    }

    /// <summary>
    /// 发凛冬单位卡组（首次进裂隙时发一次）。
    /// ★ 用户原话：「你选了进传送点。才会给你那几张需要战旗的牌」——
    ///   所以入口在地图上，但"牌"是在**真正踏进门之后**才到手的。
    /// </summary>
    private static async Task GrantStarterCardsAsync(RunState state)
    {
        try
        {
            var local = Entry.LocalPlayer(state);
            if (local == null) return;

            if (local.GetRelic<Relics.FrostPactRelic>() is not { } pact) return;
            if (pact.StarterCardsGranted) return;   // 只发一次（反复进出裂隙不重发）

            await Entry.GrantStarterUnitCard(state, local);
            pact.StarterCardsGranted = true;

            Entry.Logger.Info("[裂隙] 单位卡已在入门后发放（征召民兵 / 冰垒·前排 / 冰垒·后排 / 箭塔）。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 发单位卡失败（不影响副本）: {e.Message}");
        }
    }

    // =====================================================================
    //  出裂隙 —— 走完终点时调用
    // =====================================================================

    /// <summary>
    /// 把玩家从裂隙送回原地图。★ 这是"出门"的唯一入口。
    /// 由 FrostRiftProceedPatch / FrostRiftRoomEnteredPatch 触发。
    /// </summary>
    public static async Task ExitRiftAsync(RunState state)
    {
        if (!IsActive(state))
        {
            Entry.Logger.Warn("[裂隙] 不在裂隙里，忽略返回请求。");
            return;
        }
        if (!TryGetState(state, out var saved))
        {
            // ★ 兜底：没有存档状态（比如老存档升上来 / 存档槽丢了）——
            //   仍然把地图还回去，但落点只能是当前 Act 的起点（坐标丢了）。
            Entry.Logger.Warn("[裂隙] 没有裂隙存档状态，用降级方式返回。");
            saved = new FrostRiftSavedState { Active = true };
        }

        try
        {
            var runManager = RunManager.Instance;
            var entryCoord = saved.GetEntryCoord();

            // ① 换回原地图
            RestoreOriginalMap(state, saved);
            Entry.Logger.Info($"[裂隙] 已还原原地图，落点 = {entryCoord}");

            // ② 清存档槽
            SavedData.Remove(state);

            // ③ 刷新定位同步器（同进裂隙）
            RefreshLocationSynchronizers(state);

            // ④ 过场 + 回到地图房
            //    ★ 出裂隙**不需要 ClearScreens**（集成的返回流程 `RestoreOriginalMapAndPersist` 也没清屏），
            //      所以只要「淡出 → 进地图房 → 淡入」三拍，别把进裂隙那套整段照搬。
            //    ★ ClearTravelingFlag / NotifyMapLocationChanged 与进裂隙同理：
            //      触发这次返回的是"在地图屏上点了终点"，`IsTraveling` 需要归位。
            var needRoomChange = state.CurrentRoom is not MapRoom;
            if (needRoomChange) await runManager.FadeOut();
            SetMapScreen(state.Map, state);
            if (needRoomChange) await runManager.EnterRoom(new MapRoom());
            ClearTravelingFlag();
            NotifyMapLocationChanged();
            if (needRoomChange) await runManager.FadeIn(true);

            Entry.Logger.Info("[裂隙] 已返回原地图。");
            Entry.Toast("裂隙已闭合，你回到了原来的路线上。", "凛冬尖塔");
        }
        catch (Exception e)
        {
            Entry.Logger.Error($"[裂隙] 返回失败: {e}");
        }
    }

    /// <summary>
    /// 把 state.Map 换回"原来那张地图"。
    ///
    /// ★★ 重要修正（M3.5）：原来的主路径**是死的**，已实测确认。
    ///
    ///   原写法靠 `state.Act.ToSave()?.SavedMap` 取回原图。但反编译 `ActModel.ToSave()`：
    ///   <code>
    ///   public SerializableActModel ToSave() => new() {
    ///       Id = base.Id,
    ///       SerializableRooms = _rooms.ToSave()
    ///   };   // ★ 从不给 SavedMap 赋值
    ///   </code>
    ///   `SavedMap` 只有 `RunState.ToSave()`（真正存档时）那条路才填。
    ///   ⇒ 这个属性在运行时**恒为 null**，每次都静默落到 `TryRebuildActMap` 去**重新生成**地图。
    ///   ⇒ 重新生成的地图**不含我们注入的裂隙点** → 玩家出裂隙后那个点凭空消失。
    ///
    ///   修法（分两级，都不依赖那次死掉的调用）：
    ///     ① **主路径**：进裂隙时把原图**对象引用**留在内存里（`_originalMaps[state]`）。
    ///        同一局内进出裂隙是最常见的情况，这条路 100% 准确，连我们自己注入的点都在。
    ///     ② 兜底：内存引用没有（读档后接着玩）→ 仍然尝试重建，但**重建后补回裂隙点**
    ///        （靠 `FrostRiftSavedState` 里存的 RiftNode 坐标 + `FrostRiftMapNode` 的重新注入）。
    ///
    ///   ★ 为什么不学 Natsuki 把整张 map 序列化存下来：
    ///     内存引用在"同一局"内已经解决 99% 的场景，且零序列化风险；
    ///     跨读档的场景由兜底覆盖。等真出现"读档后裂隙点丢失"再考虑落盘整张图。
    /// </summary>
    private static void RestoreOriginalMap(RunState state, FrostRiftSavedState saved)
    {
        // ① 主路径（最准）：用进裂隙时留的那个**对象引用**。
        //    同一条命里它 100% 是原图，连我们注入的裂隙点都原样还在。
        ActMap? original = TakeStashedMap(state);

        if (original != null)
        {
            state.Map = original;
            Entry.Logger.Info("[裂隙] 已用内存缓存还原原地图（含裂隙入口点）。");
        }
        else
        {
            // ② 兜底：内存引用没了（典型场景 = 读档后接着玩，进程重启过）。
            //    ⚠️ 注意 `TryGetActMap` 这条路**实测恒为 null**（见上方注释），
            //       保留它只是为了万一将来游戏修了 `ActModel.ToSave` 能白捡。
            original = TryGetActMap(state);
            if (original != null)
            {
                state.Map = original;
                Entry.Logger.Info("[裂隙] 已从 Act.SavedMap 还原原地图。");
            }
            else
            {
                Entry.Logger.Warn("[裂隙] 内存缓存与 SavedMap 都没有，退到重建。");
                var rebuilt = TryRebuildActMap(state);
                if (rebuilt != null)
                {
                    state.Map = rebuilt;
                    // ★ 重建的图**不含我们的裂隙点** → 补回来（否则玩家出裂隙后入口消失）。
                    RestoreRiftNode(rebuilt, saved);
                }
                else throw new InvalidOperationException("无法还原原地图（拿不回也重建不了）");
            }
        }

        // 访问记录：只保留"进裂隙那天之前走过的点"。
        // ★ 我们没存 OriginalVisitedMapCoords —— 折中方案：
        //   清空后把入口坐标写回去。走到过的历史点会丢，但**不影响可走性**
        //   （可走性只看"当前坐标 + 地图结构"，不看完整历史）。
        //   ⚠️ 如果实测发现地图上"走过的路径"显示不对，再补存 OriginalVisitedMapCoords。
        state.ClearVisitedMapCoordsDebug();
        if (saved.GetEntryCoord() is { } coord)
            state.AddVisitedMapCoord(coord);

        state.ActFloor = saved.OriginalActFloor;
    }

    /// <summary>
    /// 兜底：重建出来的地图不含裂隙入口点，用存档里存的坐标把它挂回起点下面。
    /// （正常路径走内存缓存，不会到这里。）
    /// </summary>
    private static void RestoreRiftNode(ActMap map, FrostRiftSavedState saved)
    {
        try
        {
            if (saved.GetRiftNodeCoord() is not { } c) return;
            if (map is not MegaCrit.Sts2.Core.Map.StandardActMap std) return;

            var node = std.GetOrCreatePoint(c.col, c.row);
            // ★ 入口点类型 = Unknown（问号房），不是 Treasure（宝箱房）。
            //   理由同 FrostRiftMapNode：我们的点是凭空注入的，没有配套房间配置；
            //   万一拦截失效走了原逻辑，Unknown 会由游戏自己 roll 成事件/商店，不会黑屏。
            node.PointType = MegaCrit.Sts2.Core.Map.MapPointType.Unknown;
            node.CanBeModified = false;
            std.StartingMapPoint.AddChildPoint(node);
            Entry.Logger.Info($"[裂隙] 已把裂隙入口点补回重建地图 {c}。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 补回入口点失败（不影响返回）：{e.Message}");
        }
    }

    /// <summary>试着拿到"当前幕的原地图"。地图被我们换掉后，原图通常还在 Act 的存档里。</summary>
    private static ActMap? TryGetActMap(RunState state)
    {
        try
        {
            // Act 的 SavedMap（进裂隙前的原图）——ActModel 上的存档快照。
            var actSave = state.Act.ToSave();
            var savedMap = actSave?.SavedMap;
            if (savedMap != null)
            {
                Entry.Logger.Info("[裂隙] 从 Act.SavedMap 取回原地图。");
                return new SavedActMap(savedMap);
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 从 Act.SavedMap 取原地图失败: {e.Message}");
        }
        return null;
    }

    /// <summary>降级：让 Act 自己重建地图（返回 null 表示这条路也不通）。</summary>
    private static ActMap? TryRebuildActMap(RunState state)
    {
        try
        {
            // ActMap 的生成入口在 ActModel 上；不同版本可能叫 GenerateMap / CreateMap。
            // 拿不到就返回 null，让上层抛错——绝不用错地图。
            var act = state.Act;
            var method = act.GetType().GetMethod("GenerateMap",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.Invoke(act, null) is ActMap rebuilt)
            {
                Entry.Logger.Info("[裂隙] 已通过 Act.GenerateMap() 重建原地图。");
                return rebuilt;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 重建原地图失败: {e.Message}");
        }
        return null;
    }

    // =====================================================================
    //  通用小工具
    // =====================================================================

    /// <summary>把地图屏指向新地图（拿不到 NMapScreen 就跳过，玩家关掉再开地图也会刷）。</summary>
    private static void SetMapScreen(ActMap map, RunState state)
    {
        try
        {
            // ★ 签名已核实：RunManager 内部就是这么调的（sts2.dll RunManager:835）——
            //   `NMapScreen.Instance?.SetMap(map, State.Rng.Seed, clearDrawings: true)`
            NMapScreen.Instance?.SetMap(map, state.Rng.Seed, clearDrawings: true);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 刷新地图屏失败（不影响进出）: {e.Message}");
        }
    }

    /// <summary>
    /// 刷新「定位同步器」（照集成 `TreeHoleSessionManager.RefreshLocationSynchronizers`）。
    /// 换图之后不刷，顶栏/地图上的「当前所在点」还指着旧地图的坐标。
    /// 拿不到这些 API 就跳过 —— 纯显示问题，不影响进出。
    /// </summary>
    private static void RefreshLocationSynchronizers(RunState state)
    {
        try
        {
            RunManager.Instance.MapSelectionSynchronizer.OnLocationChanged(state.MapLocation);
            RunManager.Instance.RunLocationTargetedBuffer.OnLocationChanged(state.RunLocation);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 刷新定位同步器失败（不影响进出）: {e.Message}");
        }
    }

    /// <summary>
    /// 把地图屏的「正在移动」标志归位。
    ///
    /// ★★ 这是「点传送就黑屏 / 卡死」的第二号原因：
    ///   我们是从地图屏点击进的（调用链 `NMapScreen.TravelToMapCoord` → `RunManager.EnterMapCoord`），
    ///   而原版 `EnterMapPointInternal` 的**最后一句**就是
    ///   `NRun.Instance.GlobalUi.MapScreen.IsTraveling = false;`。
    ///   我们在 Harmony Prefix 里 `return false` 跳过了整段原逻辑
    ///   → 这个标志永远停在 true → 地图屏认为「还在移动中」→ 不刷新、不响应 → 卡死/黑屏。
    /// </summary>
    private static void ClearTravelingFlag()
    {
        try
        {
            if (NRun.Instance != null)
                NRun.Instance.GlobalUi.MapScreen.IsTraveling = false;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 复位移动标志失败（不影响进出）: {e.Message}");
        }
    }

    /// <summary>通知「玩家位置变了」（原版 `AfterMapLocationChanged`，private → 反射）。</summary>
    private static void NotifyMapLocationChanged()
    {
        InvokeSync(AfterMapLocationChangedMethod);
    }

    private static async Task InvokeAsync(MethodInfo? method)
    {
        if (method?.Invoke(RunManager.Instance, null) is Task t)
        {
            try { await t; } catch (Exception e) { Entry.Logger.Warn($"[裂隙] 过场失败（可忽略）: {e.Message}"); }
        }
    }

    private static void InvokeSync(MethodInfo? method)
    {
        try { method?.Invoke(RunManager.Instance, null); }
        catch (Exception e) { Entry.Logger.Warn($"[裂隙] 清屏失败（可忽略）: {e.Message}"); }
    }
}
