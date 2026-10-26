using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using FrostSpire.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

/// <summary>
/// 建筑卡四件套（M3.1）：打出 = 给自己挂对应工事能力；重复打出 = 层数叠加（Counter）。
/// 「建筑不占场上单位、不打人、只提供每回合被动」——单位（民兵等）走 AddPet 路线，见 FrostCards.cs。
/// v1 卡面借默认渲染（真图进 pck，M4）。
/// </summary>
public abstract class FrostBuildingCard : CardModel, IFrostCardArt
{
    protected FrostBuildingCard()
        : base(1, CardType.Power, CardRarity.Common, TargetType.None)
    {
    }

    protected override void OnUpgrade()
    {
        // v1：建筑不升级（升级语义 M3.2 统一设计：比如费用-1 或初始层数+1）。
    }
}

/// <summary>冰墙工事：挂/叠 IceWallPower（每回合 3×层数 格挡）。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class IceWallBuildingCard : FrostBuildingCard
{
    protected override async System.Threading.Tasks.Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<IceWallPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}

/// <summary>箭塔工事：挂/叠 ArrowTowerPower（每回合射击最左侧敌人 4×层数）。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class ArrowTowerBuildingCard : FrostBuildingCard
{
    protected override async System.Threading.Tasks.Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<ArrowTowerPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}

/// <summary>篝火工事：挂/叠 CampfirePower（每回合回 2×层数）。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class CampfireBuildingCard : FrostBuildingCard
{
    protected override async System.Threading.Tasks.Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<CampfirePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}

/// <summary>了望台工事：挂/叠 WatchtowerPower（每回合多抽 层数 张）。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class WatchtowerBuildingCard : FrostBuildingCard
{
    protected override async System.Threading.Tasks.Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<WatchtowerPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
