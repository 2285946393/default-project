using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

// ============================================================================
// 凛冬工事 · 基础卡（M3.8）—— 「攻击」类与「防御」类
//
// ★ 为什么必须补这两张（用户原话）：
//   「那本来的卡是不是没什么用？要不要直接把自己卡组全部删掉换成我们凛冬前线的卡组？」
//
//   在补它们之前，我们的 14 张卡里**只有 1 张攻击牌**（暴风雪，2 费 AOE 8）。
//   其余全是「部署建筑 / 控制 / 回血」——**没有任何一张便宜的即时直伤**。
//   所以那时候原版 Strike 是玩家唯一能"当场打死一只怪"的牌，
//   我们的牌要 2~3 回合才回本。**这时候把卡组换成纯凛冬牌，第 1 波都过不去。**
//
//   ⇒ 想换成纯凛冬卡组，**先得把「攻击」这一类补齐**。
//     这两张就是《凛冬前线》五大类里「攻击 / 防御」的最小落地，
//     也是纯凛冬起始卡组的骨架（和本体 Strike / Defend 同费同数值，保证难度可比）。
//
// ★ 实现照抄本体 `StrikeIronclad` / `DefendIronclad`（反编译取证）：
//   · 数值走 `CanonicalVars`（`DamageVar` / `BlockVar`）而不是硬编码常量 ——
//     这样**力量、虚弱、易伤、升级**这些修正才会自动生效。
//   · 攻击走 `DamageCmd.Attack(...).FromCard(this, cardPlay).Targeting(target)` ——
//     这是卡牌攻击的**正确**路径（`FromMonster` 会把目标解析成玩家侧，见 FrostUnitPowers.cs 的坑）。
// ============================================================================

/// <summary>
/// 凛冬打击（1 费 / 攻击 / 单体）：造成 6 点伤害。升级 +3。
/// 对标本体 `StrikeIronclad`（1 费 6 伤），保证「换了卡组之后前几波难度不变」。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostStrikeCard : CardModel, IFrostCardArt
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars
        => new[] { new DamageVar(6m, ValueProp.Move) };

    public FrostStrikeCard()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

/// <summary>
/// 凛冬壁垒（1 费 / 技能 / 自身）：获得 5 点格挡。升级 +3。
/// 对标本体 `DefendIronclad`（1 费 5 格挡）。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostDefendCard : CardModel, IFrostCardArt
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars
        => new[] { new BlockVar(5m, ValueProp.Move) };

    public FrostDefendCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
