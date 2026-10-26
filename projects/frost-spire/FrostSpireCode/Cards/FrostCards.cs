using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Godot;
using MinionLib.Minion;
using FrostSpire.Deploy;
using FrostSpire.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace FrostSpire.Cards;

/// <summary>凛冬尖塔的共享卡池（工事牌都挂这里）。成员照抄本体 MockCardPool 的最小实现。</summary>
[RegisterSharedCardPool]
public sealed class FrostCardPool : TypeListCardPoolModel
{
    public override string Title => "凛冬尖塔";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => new(0.55f, 0.78f, 0.95f);
    public override bool IsColorless => false;
}

/// <summary>
/// 征召民兵（M3 第一张工事牌·单位卡）：1 费技能。
/// 打出：征一只民兵入伍（我方随从，站**后排**，每回合自动攻击最前面的敌人 5 点）；
/// 再打：**重复部署 = 强化** —— 给已有民兵 +3 生命上限并回血，且**每回合多打 5 点**。
///
/// ★ M3.3 改走 MinionLib：`MinionCmd.AddMinion&lt;MilitiaM&gt;`（= `PlayerCmd.AddPet` +
///   站位 + 排布）。放在 Back 位 → 不抢冰垒的前排替伤，自己专心输出。
///   v1 借 leaf_slime_m 的外观，零新素材。
///
/// ★★ M3.8：**自动攻击原来是假的**（`AttackMove` 是空实现，只显示意图图标、不打伤害）。
///   现已改为 <see cref="Powers.MilitiaAttackPower"/> 光环（玩家回合开始触发），
///   由本卡的 `OnPlay` 挂上 —— 和 6 座新建筑一样「光环挂在卡上」，
///   所以重复部署时层数 +1 = 每回合多打 5 点。详见 `Powers/FrostUnitPowers.cs` 的类注释。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class SummonMilitiaCard : CardModel, IFrostCardArt
{
    /// <summary>部署容量占 2 点。</summary>
    public const int DeploySize = (int)FrostDeploySize.Normal;

    public SummonMilitiaCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.None)
    {
    }

    protected override bool IsPlayable
    {
        get
        {
            try
            {
                var player = Owner;
                if (player?.Creature == null) return false;
                if (!CombatManager.Instance.IsInProgress) return false;
                // 已有民兵 → 只是强化，不占新容量；没有才需要容量。
                if (FindMilitia(player) != null) return true;
                return FrostDeployment.CanDeploy(player.Creature, DeploySize);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>找我名下还活着的民兵（没有则返回 null）。</summary>
    private static Creature? FindMilitia(Player player)
        => FrostDeployment.FindSameBuilding(player.Creature, typeof(MilitiaM), MinionPosition.Back);

    protected override async System.Threading.Tasks.Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature == null) return;

        // 有新征新、有旧强化 —— 全在 AddBuildingAsync 里判（同类型 + 同站位）。
        var militia = await FrostDeployment.AddBuildingAsync<MilitiaM>(
            choiceContext, player, DeploySize,
            FrostDeploySize.Normal, MinionPosition.Back, this);
        if (militia == null) return;

        // ★ 自动攻击的光环挂在**卡**上：重复部署时这段会再跑一次 → 层数 +1 → 每回合多打 5 点。
        await PowerCmd.Apply<Powers.MilitiaAttackPower>(choiceContext, militia, 1m, player.Creature, this);

        Entry.Logger.Info($"凛冬尖塔：民兵已就位（后排，{militia.MaxHp} 血，每回合自动攻击最前方敌人）。");
    }

    protected override void OnUpgrade()
    {
        // 升级版：民兵更硬。M3.4 换 DynamicVar。
    }
}
