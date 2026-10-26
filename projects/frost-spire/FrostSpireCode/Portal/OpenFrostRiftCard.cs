using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using FrostSpire.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Portal;

/// <summary>
/// 开启凛冬裂隙（传送门方案的**阶段 1 入口**）。
///
/// ⚠️⚠️ **2026-10-06：本卡已停用发放，是一张废牌，别再用它当入口。**
///
///   用户实测报障原话：「这牌有什么用。地图上是不能打出牌。只有战斗中才能打出牌。
///   然后这牌又不能在战斗中打出，然后你给我这些召唤物有什么用。」
///
///   根因：下面 `IsPlayable` 要求 `state.CurrentRoom is MapRoom`（站在地图房），
///   但**地图房没有手牌 UI** —— 玩家在地图上根本看不到手牌，更点不到。
///   ⇒ 这张卡**永远打不出去**（战斗里被 `IsPlayable` 挡、地图上没 UI），
///     只会白占牌库一个位置。
///
///   它当初的定位是「阶段 1 的最低成本入口」：
///     打这张牌 → 立刻把整张地图换成裂隙小地图 → 打完再回来。
///     理由是当时**地图上放点**要动 `ActMap.Grid` + 地图屏图标/可达性补丁（一大坨 UI 活儿）。
///
///   但**阶段 2 已经做完**了：地图上会注入一个「凛冬裂隙」问号节点（见 `FrostRiftMapNode`），
///   点它就能进裂隙 —— 那才是玩家实际在用的入口。
///   ⇒ 卡牌入口彻底冗余，`FrostPactRelic.AfterObtained` 已不再发放它（见 `FrostPactRelic.Rift.cs`）。
///
///   **类本身保留不删**：老存档里可能还有这张卡的实例，删类会让存档读不出来。
///   它在战斗里依旧是灰的，不会干扰战斗。
///
///   —— 以后若要捡回来，正确做法是给它接一个**地图上的热键/按钮**（见 `UI/FrostHotkey.cs`
///   的写法：常驻 Node + `_Input` 收键），而不是靠"地图房出牌"。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class OpenFrostRiftCard : CardModel
{
    public OpenFrostRiftCard()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
    }

    /// <summary>
    /// ⚠️ 这个条件**在地图上永远不成立**（地图房没有手牌 UI，牌根本不会被渲染/点击），
    ///   所以本卡实际上是死牌。详见类注释。
    /// </summary>
    protected override bool IsPlayable
    {
        get
        {
            try
            {
                if (CombatManager.Instance.IsInProgress) return false;
                return Owner?.RunState is RunState state && state.CurrentRoom is MapRoom;
            }
            catch
            {
                return false;   // 拿不到状态就当不能用（安全侧）
            }
        }
    }

    protected override async System.Threading.Tasks.Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ★ Owner.RunState 的静态类型是 IRunState（接口），裂隙要的是 RunState（具体类）——
        //   和 Natsuki 的写法一样，做一次显式 downcast。
        var state = Owner?.RunState as RunState;
        if (state == null)
        {
            Entry.Logger.Warn("[裂隙] 没有 RunState，打不开裂隙。");
            return;
        }

        // 双保险：IsPlayable 已经挡了一层，这里再确认一次（万一有别的路径绕进来）。
        if (state.CurrentRoom is not MapRoom)
        {
            Entry.Toast("裂隙只能在地图上开启。", "凛冬尖塔");
            Entry.Logger.Info("[裂隙] 不在地图房，拒绝开启。");
            return;
        }

        await FrostRiftSession.EnterRiftAsync(state);
    }

    protected override void OnUpgrade()
    {
        // 0 费技能，升级无意义（本卡的用途是"开门"，不是数值）。
    }
}
