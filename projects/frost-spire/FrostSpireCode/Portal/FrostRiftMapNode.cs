using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace FrostSpire.Portal;

/// <summary>
/// 在**原版地图**上放一个开局即可达的「凛冬裂隙」入口点。
///
/// ★ 这是用户的核心诉求：
///   「单独地图上创建一个开局能直接联通的点。（这样你打原来的尖塔就是原来的尖塔。
///     不改变其他的体验。我们只用走我们的点进别的地图，然后去打我们的游戏）」
///
/// 设计（为什么不改原版体验）：
///   - 原地图**一个现存节点类型都不改**，只在起点旁边多挂一个点。
///   - 玩家不点它 → 完全就是原来的尖塔，一点差别都没有。
///   - 玩家点它 → 换图进裂隙；打完结完 → 回到原地图的同一个位置继续走。
///
/// ============================================================================
///  ★ 关键机制（全部反编译核实过，不是猜的）
/// ============================================================================
///
/// ① 地图屏只画 `Grid` 里的点 —— `NMapScreen.SetMap()` 用 `map.GetAllMapPoints()` 遍历，
///    而 `ActMap.GetAllMapPoints()` 就是**双层循环走 `Grid[col, row]`**。
///    ⇒ 我们的点**必须写进 `Grid`**，否则屏幕上根本不显示。
///
/// ② `Grid` 是 `protected abstract MapPoint?[,]`，尺寸在构造时就定死（标准图是 `[7, mapLength]`），
///    不能扩容。⇒ 只能找**一个本来就空着的格子**放。
///
/// ③ 标准图的 `row 0` **只放起点一个点**（`StandardActMap.GenerateMap()`：
///    真正的路径点全在 `row >= 1`，起点 `StartingMapPoint` 单独在 row 0）。
///    ⇒ `row 0` 上除起点列以外的格子天然是空的，是放我们点的最佳位置。
///
/// ④ 可达性 = `MapTravel.GetTravelablePointsFrom()` 返回 `currentPoint.Children`。
///    地图屏 `RecalculateTravelability()` 也是按 `Children` 判"能不能走"。
///    ⇒ 只要 `StartingMapPoint.AddChildPoint(ourPoint)`，它开局就是**可直接点**的。
///
/// ⑤ 点击 → 进房间的唯一入口是 `RunManager.EnterMapCoord(MapCoord)`（public Task）。
///    链路：`NMapPoint.OnRelease()` → `_screen.OnMapPointSelectedLocally(node)`
///          → …… → `RunManager.EnterMapCoord(coord)`。
///    ⇒ 用 Harmony **Prefix** 拦它：坐标是我们的点 → 走 `EnterRiftAsync`，
///      并 return false 跳过原逻辑（这样它**不会**去掷房间类型、不会生成一个真房间）。
///
/// ⑥ 时机：`MapGeneratedEvent` 由 RitsuLib 在 `Hook.AfterMapGenerated` 之后发布
///    （见 `AfterMapGeneratedLifecyclePatch`）。而 `RunManager.GenerateMap()` 的顺序是：
///      `ModifyGeneratedMap` → `AfterMapGenerated`(=发事件) → `State.Map = map`
///      → `RemoveStaleVisitedMapCoords(map)` → `NMapScreen.SetMap(map, ...)`。
///    ⇒ 我们在事件里改 map，**后面刷屏时就已经带上我们的点了**，不用自己刷 UI。
///    ⚠️ 而且此时 `MapPostProcessing.CenterGrid` 已经跑完（它在 StandardActMap 构造里），
///       所以我们的坐标不会被居中算法挪走 —— 但仍以**当前** `StartingMapPoint.coord` 为基准算，
///       不写死数字，这样才能适配任何列宽/居中偏移。
///
/// ⑦ 每幕只放一个，且只在**第一幕**放（`ActIndex == 0`），
///    这样"只有开局能进裂隙"符合用户说的"开局能直接联通"。
///    （想让每幕都有，把 `OnlyFirstAct` 改成 false 即可。）
/// </summary>
internal static class FrostRiftMapNode
{
    /// <summary>只在第一幕（Act 0）放这个点。改 false = 每幕都放。</summary>
    private const bool OnlyFirstAct = true;

    private static bool _patched;

    // ★ 裂隙入口坐标存「跟着这局走」的槽，不用静态字段。
    //   静态字段跨局残留会误拦真地图点（详见 OnMapGenerated 里的说明）。
    //
    // ★★ 但槽本身**必须只在 FrostRiftSession 里注册一次** ——
    //    `RunSavedDataStore.Register(key)` 的 key 是全局唯一的，同一个 key 注册第二次会抛
    //    `InvalidOperationException: RunSavedData key is already registered`，
    //    而且它抛在**静态构造（.cctor）**里 → 整个类直接不可用。
    //    （这里原来自己又注册了一份，正是「点传送就黑屏」的根因，详见 FrostRiftSession 的注释。）
    //    ⇒ 这里只调方法，绝不碰 RunSavedDataStore。

    /// <summary>装好事件订阅 + EnterMapCoord 拦路。在 Entry.Initialize 里调一次。</summary>
    public static void Install()
    {
        // 地图生成时注入我们的点。
        RitsuLibFramework.SubscribeLifecycle<MapGeneratedEvent>(OnMapGenerated);

        // 拦住"点我们的点"这件事，改成开门而不是进房间。
        if (!_patched)
        {
            try
            {
                new Harmony(Entry.ModId + ".riftmapnode").PatchAll(typeof(FrostRiftMapNode).Assembly);
                _patched = true;
            }
            catch (Exception e)
            {
                // 打不上补丁也不致命：点还在图上，只是点了会照常进房间（退化成"没事发生"）。
                Entry.Logger.Warn($"[裂隙点] Harmony 补丁挂载失败: {e.Message}");
            }
        }

        Entry.Logger.Info("[裂隙点] 地图入口点注入器已就绪。");
    }

    // =====================================================================
    //  ① 地图生成 → 往 Grid 的空白格里塞一个点，并挂到起点下面
    // =====================================================================

    private static void OnMapGenerated(MapGeneratedEvent evt)
    {
        try
        {
            if (evt.Map is not StandardActMap map) return;      // 只改标准图（裂隙自己的图不走这条路）
            if (OnlyFirstAct && evt.ActIndex != 0) return;

            var start = map.StartingMapPoint;
            int row = start.coord.row;

            // 起点所在行找第一个空列（标准图 row 0 只有起点自己）。
            int? col = FindFreeColumn(map, row);
            if (col == null)
            {
                Entry.Logger.Warn($"[裂隙点] 第 {row} 行没有空位，本幕不放裂隙入口。");
                return;
            }

            // ★ GetOrCreatePoint 是 StandardActMap 的 public 方法，会**同时写进 Grid**
            //   —— 这是"能显示在屏幕上"的关键（NMapScreen 只遍历 Grid）。
            var node = map.GetOrCreatePoint(col.Value, row);
            // ★ 类型用 Unknown（问号房），**不是 Treasure（宝箱房）**。两个理由：
            //   ① 语义贴切：问号 = "是什么，进去才知道"，正好表示"通往另一张地图的门"，
            //      而且它的图标本来就是问号，比宝箱图标更像传送入口。
            //   ② 兜底安全：我们这个点是**凭空注入**的，没有配套的房间配置。
            //      宝箱房需要真实宝箱数据，万一 Harmony 拦截失效走了原逻辑就会黑屏；
            //      Unknown 则由游戏自己 roll 成事件 / 商店 / 宝箱，每一种都有完整配置。
            node.PointType = MapPointType.Unknown;
            node.CanBeModified = false;               // 别让后续的地图算法动它

            // ★ 挂到起点下面 = 开局就能点（可达性只看 Children）。
            start.AddChildPoint(node);

            // ★ 坐标存进「跟着这局走」的存档槽，而不是静态字段——
            //   静态字段会跨局残留：上一局在 (6,0) 放了个点，新一局 (6,0) 恰好是张真地图点，
            //   就会被我们误拦成"开门"，把玩家手里的普通节点吃掉。存进 run 槽则天然随局清理。
            RememberRiftCoord(evt.RunState, node.coord);
            Entry.Logger.Info($"[裂隙点] 已在本幕地图 {node.coord} 注入凛冬裂隙入口（起点 {start.coord} 可达）。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙点] 注入失败（不影响原版地图）: {e.Message}");
        }
    }

    /// <summary>在指定行找一个没被占用的列（从离起点最远的边开始找，避免盖住主路径）。</summary>
    private static int? FindFreeColumn(ActMap map, int row)
    {
        int cols = map.GetColumnCount();
        if (row < 0 || row >= map.GetRowCount()) return null;

        // 从最右往左扫：标准图起点居中，右边通常比左边更空。
        for (int c = cols - 1; c >= 0; c--)
        {
            if (map.GetPoint(c, row) == null) return c;
        }
        return null;
    }

    // =====================================================================
    //  裂隙入口坐标的存取（跟着这局走）
    // =====================================================================

    /// <summary>把入口坐标写进本局存档槽（真正的存储在 FrostRiftSession，这里只转发）。</summary>
    private static void RememberRiftCoord(IRunState runState, MapCoord coord)
    {
        if (runState is not RunState state) return;
        FrostRiftSession.RememberRiftCoord(state, coord);
    }

    /// <summary>取本局的入口坐标（没有/读不到 → null，即"本局没有裂隙点"）。</summary>
    private static MapCoord? GetRiftCoord(IRunState runState)
        => runState is RunState state ? FrostRiftSession.GetRiftCoord(state) : null;

    // =====================================================================
    //  ② 点我们的点 → 进裂隙（而不是进房间）
    // =====================================================================

    /// <summary>
    /// 拦 `RunManager.EnterMapCoord`。
    /// ★ 只在我们那个点上动手；其余坐标一律放过（原版体验零改动）。
    /// </summary>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapCoord))]
    internal static class EnterMapCoordPatch
    {
        /// <summary>
        /// 返回 false = 跳过原方法（我们自己处理了）。
        ///
        /// ★★ `__result` 必须赋值 —— 踩过的坑（实测黑屏的第二号原因）：
        ///    `RunManager.EnterMapCoord` 的返回类型是 `Task`。Harmony 的 Prefix 返回 false
        ///    跳过原方法时，**如果不设 `__result`，这个方法就会返回 `default(Task)` 也就是 `null`**，
        ///    调用方（`NMapScreen.TravelToMapCoord` 那条链）一 `await` 就抛 NullReferenceException
        ///    → 游戏动作 `MoveToMapCoordAction` 以异常收场 → 地图屏卡死/黑屏。
        ///    （日志原文：`MoveToMapCoordAction ... completed with exception: ... NullReferenceException`）
        ///
        ///    ⇒ 正确写法：把我们自己的 Task 挂到 `__result` 上，
        ///      这样调用方会**等我们的进裂隙流程跑完**，而不是等一个 null。
        ///      顺带把原来 fire-and-forget 的 `_ = EnterRiftAsync(...)` 也去掉了 ——
        ///      异步流程接回调用链，异常不再"掉在地上"。
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix(RunManager __instance, MapCoord coord, ref Task __result)
        {
            try
            {
                var state = __instance.State;
                if (state == null) return true;

                // 不是本局那个裂隙点 → 原逻辑（这一步天然排除了跨局残留的误拦）。
                if (GetRiftCoord(state) is not { } rift || !coord.Equals(rift)) return true;

                // 已经在裂隙里（比如读档后地图还没换回来）→ 别再套娃。
                if (state.Map is FrostRiftActMap) return true;

                // ★ 原方法第一件事就是 AddVisitedMapCoord；我们跳过原方法，得自己补上，
                //   否则"当前位置"不更新，裂隙出口回不来、地图屏也不会把这个点标成已走。
                //   （AddVisitedMapCoord 返回 false = 这个点已经走过 → 让它走原逻辑，玩家会被送回当前房间，符合预期。）
                if (!state.AddVisitedMapCoord(coord)) return true;

                Entry.Logger.Info($"[裂隙点] 玩家点击 {coord} → 进入凛冬裂隙。");
                __result = FrostRiftSession.EnterRiftAsync(state);   // ★ 接回调用链，不能留 null
                return false;   // 拦下：不进普通房间
            }
            catch (Exception e)
            {
                Entry.Logger.Warn($"[裂隙点] 拦截失败，退回默认行为: {e.Message}");
                return true;
            }
        }
    }
}
