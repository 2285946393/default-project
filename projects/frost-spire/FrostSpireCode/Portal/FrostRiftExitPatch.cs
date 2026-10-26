using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrostSpire.Portal;

/// <summary>
/// 裂隙的**出口补丁**——「走完终点 → 送回原地图」。
///
/// ★ 双保险，两条路都要（学自集成战略事件 3747637213 的 TreeHole）：
///
///   **① 主路径**：`RunManager.ProceedFromTerminalRewardsScreen`（Harmony Postfix）
///      打完终点节点的奖励、点「继续」时触发。这是"正常走完"的路径。
///      ★ 已核实签名：`public async Task ProceedFromTerminalRewardsScreen()`（sts2.dll:1424）
///
///   **② 兜底路径**：`RunManager.RoomEntered` **事件订阅**（不是 Harmony 补丁！）
///      ★ 已核实：`RoomEntered` 是 `public event Action`（sts2.dll:270），
///        在 `RunManager` 内部 `this.RoomEntered?.Invoke()`（:1245）处触发。
///        **对事件不能打 Harmony 补丁** —— 只能订阅。ZCode 的稿和 wb 的第一版都漏了这点。
///      进任何房间时检查一次：如果"人在裂隙地图上 + 已踩过终点 + 现在进的是地图房"，
///      就送回去。防的是"玩家在裂隙里手动点地图屏乱走"之类的漏网情况。
///
/// ⚠️ Natsuki 还有第三条：读档恢复（`RunState.FromSerializable`）。
///    我们**不做**——裂隙会话被中断就直接作废（v1 取舍），见 FrostRiftSavedState 注释。
/// </summary>
internal static class FrostRiftExitPatch
{
    private static bool _installed;

    /// <summary>挂上事件兜底。在 Entry.Initialize 里调一次。</summary>
    public static void Install()
    {
        if (_installed) return;
        _installed = true;
        RunManager.Instance.RoomEntered += OnRoomEntered;
        Entry.Logger.Info("[裂隙] 出口事件兜底已挂上。");
    }

    /// <summary>主出口：终点奖励界面点「继续」。★ 已核实方法存在。</summary>
    [HarmonyPatch(typeof(RunManager), "ProceedFromTerminalRewardsScreen")]
    [HarmonyPostfix]
    private static void ProceedPostfix(ref Task __result)
    {
        var original = __result;
        __result = AfterProceedAsync(original);
    }

    private static async Task AfterProceedAsync(Task proceedTask)
    {
        try
        {
            await proceedTask;

            var state = RunManager.Instance.DebugOnlyGetState();
            if (state == null) return;
            if (state.Map is not FrostRiftActMap riftMap) return;

            // 只有"已经踩过终点"才允许出去——否则玩家在半路点了个什么就被踢出去了。
            if (!HasVisitedTerminal(state, riftMap))
            {
                Entry.Logger.Info("[裂隙] 尚未走完终点，留在裂隙里。");
                return;
            }

            await FrostRiftSession.ExitRiftAsync(state);
        }
        catch (Exception e)
        {
            Entry.Logger.Error($"[裂隙] 主出口处理失败: {e}");
        }
    }

    /// <summary>兜底：进任何房间时检查一次（事件回调，不是补丁）。</summary>
    private static void OnRoomEntered()
    {
        try
        {
            var state = RunManager.Instance.DebugOnlyGetState();
            if (state == null) return;
            if (state.Map is not FrostRiftActMap riftMap) return;

            // 只在地图房触发，避免刚进战斗房就被弹出去
            if (state.CurrentRoom is not MapRoom) return;
            if (!HasVisitedTerminal(state, riftMap)) return;

            Entry.Logger.Info("[裂隙] 兜底路径触发：已走完终点且回到地图房，准备返回。");
            _ = FrostRiftSession.ExitRiftAsync(state);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 兜底检查失败（不影响游戏）: {e.Message}");
        }
    }

    private static bool HasVisitedTerminal(RunState state, FrostRiftActMap riftMap)
    {
        try
        {
            foreach (var c in state.VisitedMapCoords)
                if (riftMap.IsTerminalCoord(c))
                    return true;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[裂隙] 读访问记录失败: {e.Message}");
        }
        return false;
    }
}
