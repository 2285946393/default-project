using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using FrostSpire.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

// ============================================================================
// 凛冬「裂霜」· 状态与养成牌（M3.10）—— 5 张
//
// ★ 这批不是"再多几张"，是**补两个会让卡组中期崩掉的结构性缺口**：
//
//   ① **易伤缺口**：M3.8 结束时凛冬卡池**一张易伤都没有**。
//      易伤是尖塔的伤害放大器 —— 没有它，中后期就是纯数值比拼，
//      而我们的数值（凛冬打击 1 费 6 伤）在第 10 波之后必然不够看。
//      ⇒ 补「裂霜」（1 费 5 伤 + 2 层易伤）+「霜蚀蔓延」（易伤可传递）。
//
//   ② **力量缺口**：力量原本只有寒冰王座（4 费 Rare，一次性 +3）一张。
//      ⇒ 补「霜锻」（每回合 +1 力量，复利版）+「极寒核心」（随从阵亡换永久力量）。
//
// ★ 数值对标（反编译本体取数，不凭感觉）：
//   本体 Bash = **2 费 8 伤 + 2 层易伤**；本体 Strike = 1 费 6 伤。
//   我们的「裂霜」= **1 费 5 伤 + 2 层易伤** —— 比 Bash 便宜一半、直伤更低，
//   但易伤的**放大器价值**完整保留（易伤 = 受到的攻击伤害 +50%）。
//
// ★ 沿用 M3.9 的触发式范式，5 张里有 4 张是能力牌（挂着持续生效），不是一次性效果。
// ============================================================================

/// <summary>
/// 裂霜（1 费 · 攻击）：造成 5 点伤害，并给 **2 层易伤**。
///
/// ★ 这是凛冬卡池**第一张也是唯一一张易伤来源**（M3.10 之前是 0 张）。
///   它让「凛冬打击」这种白板牌在中后期还有价值 —— 易伤一挂，6 伤变 9 伤。
///
/// ★ 冲击铁律：走 `DamageCmd.Attack(...).FromCard(...)`（**不是 `FromMonster`**），
///   后者会把目标解析成玩家侧（M3.8 已踩，详见 FrostUnitPowers.cs 的类注释）。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostCrackCard : CardModel, IFrostCardArt
{
    /// <summary>易伤层数（对标本体 Bash 的 2 层）。</summary>
    public const int VulnerableAmount = 2;

    public FrostCrackCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override System.Collections.Generic.IEnumerable<DynamicVar> CanonicalVars
        => new DynamicVar[] { new DamageVar(5m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null || !target.IsAlive) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext, target, VulnerableAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

/// <summary>霜蚀蔓延（1 费 · 能力）：敌人死亡时，把它的易伤层数转给其它敌人。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostSpreadCard : FrostTriggerCardBase<FrostSpreadPower>
{
    public FrostSpreadCard() : base(1, CardRarity.Uncommon) { }
}

/// <summary>霜锻（1 费 · 能力）：每回合开始获得 1×层数 力量（复利成长）。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostForgeCard : FrostTriggerCardBase<FrostForgePower>
{
    public FrostForgeCard() : base(1, CardRarity.Uncommon) { }
}

/// <summary>极寒核心（1 费 · 能力）：我方随从被摧毁时，获得 1×层数 力量。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class DeepFreezeCoreCard : FrostTriggerCardBase<DeepFreezeCorePower>
{
    public DeepFreezeCoreCard() : base(1, CardRarity.Rare) { }
}

/// <summary>冰壳（1 费 · 能力）：每当你获得格挡时，对全体敌人造成 1×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class IceShellCard : FrostTriggerCardBase<IceShellPower>
{
    public IceShellCard() : base(1, CardRarity.Uncommon) { }
}
