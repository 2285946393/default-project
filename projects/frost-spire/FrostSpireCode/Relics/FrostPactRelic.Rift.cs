using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using FrostSpire.Portal;

namespace FrostSpire.Relics;

/// <summary>
/// 凛冬契约的**裂隙部分**（阶段 1.5）。
///
/// 用 partial 拆出来，故意和 FrostPactRelic.cs 本体分开：
///   - 本体的波次逻辑（锁1/锁2 + 16 波表）是 M2 已实测的，**一行都不动**。
///   - 裂隙是 M3.5 的新东西，单独一个文件，出问题一眼看得出是哪边。
///
/// ★★ 2026-10-06 重大修正：「开启凛冬裂隙」卡**已停用发放**。
///
///   为什么（用户实测报障原话：「这牌有什么用。地图上是不能打出牌。
///   只有战斗中才能打出牌。然后这牌又不能在战斗中打出」）：
///
///   `OpenFrostRiftCard.IsPlayable` 要求 `state.CurrentRoom is MapRoom`（站在地图房），
///   可**地图房压根没有手牌 UI** —— 玩家在地图上根本看不到、也点不到手牌。
///   ⇒ 这张卡**永远打不出去**，只会白占牌库一个位置，纯纯的废牌。
///
///   它当初的定位是「阶段 1 的最低成本入口」（见 `OpenFrostRiftCard` 的类注释），
///   而**阶段 2 已经做完**：地图上会注入一个「凛冬裂隙」问号节点（`FrostRiftMapNode`），
///   点它就能进裂隙 —— 那才是玩家实际在用的入口（用户上一轮报的"点传送黑屏"就是它）。
///   ⇒ 卡牌入口彻底冗余，停发。
///
///   **类本身保留**（不删）：存档里可能还留着这张卡的实例，删类会让老档读不出来。
///   它在战斗里依旧是灰的（`IsPlayable` 为 false），不会干扰战斗。
/// </summary>
public sealed partial class FrostPactRelic
{
    /// <summary>已经发过裂隙卡了（进存档，防读档重发）。★ 已停用发放，字段保留仅为存档兼容。</summary>
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public bool RiftCardGranted { get; set; }

    /// <summary>
    /// 已经发过凛冬单位卡组了（进存档，防反复进裂隙重复发牌）。
    /// ★ 用户要求：单位卡是**进了传送点**才发，所以这个标志由 `FrostRiftSession.EnterRiftAsync` 置位。
    /// </summary>
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public bool StarterCardsGranted { get; set; }

    public override async System.Threading.Tasks.Task AfterObtained()
    {
        // ★ 先跑本体（如果本体以后要加 AfterObtained，这里不会挡路）。
        await base.AfterObtained();

        // ★★ 2026-10-06：**不再发「开启凛冬裂隙」卡** —— 它是打不出去的废牌，理由见类注释。
        //    地图上的传送节点（`FrostRiftMapNode`）才是正式入口。
        //    `GrantRiftCardAsync` 保留不删，万一以后要做"地图上按热键开裂隙"还能捡回来。
        // await GrantRiftCardAsync();
    }

    /// <summary>
    /// 塞一张裂隙卡进牌库。失败只记日志，绝不影响开局。
    /// ★ 当前**没有调用方**（发放已停用）—— 保留是为了以后接「地图热键开门」时复用。
    /// </summary>
    private async System.Threading.Tasks.Task GrantRiftCardAsync()
    {
        try
        {
            if (RiftCardGranted) return;                       // 读档重入：不重发
            if (Owner == null) return;
            if (Owner.RunState is not RunState state) return;

            // ★ 用泛型 CreateCard<T>（RunState:641）—— 拿到的是"属于这个玩家"的实例，
            //   直接 Add 进牌库。这是 Natsuki 遗物里那一套的简化版（它多做了一步 MarkCardAsSeen）。
            var card = state.CreateCard<OpenFrostRiftCard>(Owner);

            await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Bottom);
            RiftCardGranted = true;

            Entry.Logger.Info("凛冬尖塔：已把「开启凛冬裂隙」放进牌库（裂隙入口就位）。");
            Entry.Toast("牌库里多了一张「开启凛冬裂隙」——想挑战独立战场时打出它。", "凛冬尖塔");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"发裂隙卡失败（不影响凛冬波次玩法）: {e.Message}");
        }
    }
}
