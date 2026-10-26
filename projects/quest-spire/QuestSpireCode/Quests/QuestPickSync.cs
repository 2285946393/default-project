using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Runs;
using QuestSpire.Relics;

namespace QuestSpire.Quests;

// 多人局的「选任务同步」。
//
// 做法照抄游戏自己的「玩家选择」通道（就是「弃牌选哪张」「遗物三选一」用的那一套），
// 也照抄《海克斯符文》的用法：
//   1. 开局时，**每个客户端都按同一顺序、给每位玩家各预留一个编号** → 各端编号一致；
//   2. 本机玩家挑完 → SyncLocalChoice 把「挑了哪几个任务」广播出去；
//   3. 其他客户端各自 WaitForRemoteChoice，收到后写进**那位玩家自己的任务册**。
//
// 这样每台机器的任务列表完全一致，主机那份存档也是全的 → 读档不再变样。
// 单人局完全不走这里（人数 ≤ 1 直接返回 false）。
//
// ⚠️ 编号必须"各端同进退"：只有本机预留、别人不预留，会让后面游戏自己的选择错位。
//    所以商店那种"随时可能发生"的挑选不进这里（多人局只在开局挑一次）。
//
// ⚠️⚠️ 编号计数器本身**是校验和的一部分**（`NetFullCombatState.FromRun` 里
//     `nextChoiceIds = PlayerChoiceSynchronizer.ChoiceIds`），所以「谁预留了几次」各端必须一样；
//     只要有一台没预留（或者替别人预留了），主客机校验和对不上 → 客机被踢回主菜单。
public static class QuestPickSync
{
    // 每位玩家这一局的"选择编号"。各客户端算出来必须一样。
    private static readonly Dictionary<ulong, uint> _ids = new();
    private static Player? _local;
    private static bool _active;

    /// <summary>局结束 / 新局开头调用：把这一局的编号和本地玩家引用丢掉。</summary>
    public static void Reset()
    {
        _active = false;
        _ids.Clear();
        _local = null;
    }

    /// <summary>
    /// 开局时调用（新的一局）。多人局里真的接管挑选 → 返回 true；
    /// 单人局、或者拿不到游戏的选择通道 / 认不出本地玩家 → 返回 false（调用方自己决定怎么办）。
    /// </summary>
    public static bool StartMultiplayer(RunState? state)
    {
        Reset();
        if (state == null || state.Players.Count <= 1) return false;

        try
        {
            var sync = RunManager.Instance.PlayerChoiceSynchronizer;
            if (sync == null)
            {
                Entry.Logger.Warn("QuestPickSync: 拿不到玩家选择通道，多人局走自动分配。");
                return false;
            }

            // ⚠️ 本地玩家**必须精确认出来**，认不出来就整个放弃（退回自动分配）。
            //
            // 为什么不能用 `Entry.LocalPlayer` 的那个「退回第一位玩家」兜底：
            //   兜底之后 `_local` 会变成**别人**。于是
            //     · `NotifyLocalPick` 会拿**别人的编号**去广播 → 等于替别人挑任务；
            //     · 自己那个编号永远没人填 → 各端等的不是同一件事；
            //     · 本机的面板/账本也挂到了别人的册子上。
            //   宁可退回"同种子自动分配"（玩法差一点但一定同步），也不要认错人。
            //   判据用 `LocalContext.NetId`（游戏自己的口径，`RunManager.Launch()` 里写的），
            //   别用 `NetService.NetId`（断线时它会抛异常，抛了就更容易退化成"认错人"）。
            var localId = MegaCrit.Sts2.Core.Context.LocalContext.NetId;
            _local = localId.HasValue
                ? state.Players.FirstOrDefault(p => p.NetId == localId.Value)
                : null;
            if (_local == null)
            {
                Entry.Logger.Warn($"QuestPickSync: 认不出本地玩家（LocalContext.NetId={localId?.ToString() ?? "null"}，"
                                  + $"对局里是 {string.Join(",", state.Players.Select(x => x.NetId))}）→ 多人局改走自动分配。");
                return false;
            }

            // 按玩家顺序预留：所有客户端都跑同一段 → 编号一致。
            foreach (var p in state.Players) _ids[p.NetId] = sync.ReserveChoiceId(p);

            _active = true;

            // 后台等别人的选择：谁先挑都一样，不卡任何人。
            foreach (var p in state.Players)
            {
                if (p.NetId == _local.NetId) continue;
                _ = WaitForRemoteAsync(p);
            }

            Entry.Logger.Info($"QuestPickSync: 多人局接管选任务，已为 {state.Players.Count} 位玩家预留编号"
                              + $"（我是 {_local.NetId}）。");
            return true;
        }
        catch (Exception e)
        {
            _active = false;
            Entry.Logger.Warn($"QuestPickSync.StartMultiplayer 失败（走自动分配）：{e.Message}");
            return false;
        }
    }

    /// <summary>本机玩家点「确认接取」后调用：把挑了哪几个广播给其他人。</summary>
    public static void NotifyLocalPick(IReadOnlyList<QuestDef> chosen)
    {
        if (!_active || _local == null) return;
        try
        {
            // ⚠️ 「接取时改动」（诅咒交易塞诅咒 / 大改造换牌）必须**各端一起做**：
            //    牌组是各端各复制一份，只有一台改 → 各端牌组不一样 → 校验和不匹配 → 掉线。
            //    `EnsureQuestSetups` 自己带幂等闸（QuestSetupDone），各端多调几次没关系。
            _ = QuestRunner.LocalBook?.EnsureQuestSetups();

            var sync = RunManager.Instance.PlayerChoiceSynchronizer;
            if (sync == null || !_ids.TryGetValue(_local.NetId, out var id)) return;

            // 用「任务种类」当身份证：各端枚举一致，比传索引稳。
            var kinds = chosen.Select(d => (int)d.Kind).ToList();
            sync.SyncLocalChoice(_local, id, PlayerChoiceResult.FromIndexes(kinds));
            Entry.Logger.Info($"QuestPickSync: 广播本机选择，共 {kinds.Count} 个任务。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"QuestPickSync.NotifyLocalPick 失败：{e.Message}");
        }
    }

    private static async Task WaitForRemoteAsync(Player p)
    {
        try
        {
            var sync = RunManager.Instance.PlayerChoiceSynchronizer;
            if (sync == null || !_ids.TryGetValue(p.NetId, out var id)) return;

            var result = await sync.WaitForRemoteChoice(p, id);
            var relic = p.GetRelic<QuestBookRelic>();
            if (relic == null) return;

            int n = 0;
            foreach (var raw in result.AsIndexes())
            {
                var def = QuestCatalog.Find((QuestKind)raw);
                if (def != null && relic.State.TryAdd(def)) n++;
            }
            Entry.Logger.Info($"QuestPickSync: 收到玩家 {p.NetId} 的任务选择，写入 {n} 个。");

            // 各端一起做「接取时改动」（理由同 NotifyLocalPick 里那段）。
            if (n > 0) _ = relic.EnsureQuestSetups();
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"QuestPickSync.WaitForRemoteAsync 失败：{e.Message}");
        }
    }
}
