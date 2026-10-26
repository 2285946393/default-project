using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Powers;

/// <summary>
/// 凛冬工事能力四件套（M3.1）。挂在玩家身上的持久 Buff，层数=强度；
/// 重复打出对应工事牌 → 层数叠加 → 效果变强。
/// ⚠️ PowerModel 的 Owner 是**生物**（不是 Player）——玩家本人 = Owner（能力挂在谁身上谁就是 Owner）。
/// 触发时机统一用「玩家回合开始」（side 守卫照抄 FlameBarrierPower 的 `Owner.Side != side`）。
/// 命令全走已验证：CreatureCmd.Damage（FlameBarrier 反伤同款，无卡来源 Unpowered）/
/// GainBlock（quest-spire 验证）/ Heal（OstyCmd 验证）/ CardPileCmd.Draw（MinionStrike 同款，要传 Player）。
/// </summary>
public abstract class FrostBuildingPowerBase : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldPlayVfx => false;

    protected static bool IsMyTurnStart(PowerModel power, CombatSide side)
        => power.Owner != null && power.Owner.Side == side;

    // ====================================================================
    // ★★ M3.11 修：「养大哥」原来是空的（力量对随从完全不生效）
    //
    //  反编译 `StrengthPower.ModifyDamageAdditive` 原文：
    //      if (base.Owner != dealer) return 0m;
    //      if (!props.IsPoweredAttack()) return 0m;   // ← 卡在这里
    //      return base.Amount;
    //  而 `IsPoweredAttack()` = `props.HasFlag(Move) && !props.HasFlag(Unpowered)`。
    //
    //  我们所有随从/建筑的自动攻击都用 `ValueProp.Unpowered`（官方给"能力伤害"的惯例值），
    //  ⇒ **力量一点都加不上去**。最直接的受害者是**寒冰王座**（4 费 Rare）：
    //    它的「部署时全体友军 +3 力量」是**一张空头支票** —— 箭塔、民兵一点没变强。
    //
    //  ★ 为什么不改成 `ValueProp.Move`（那样力量会自动生效）：
    //    Move 会顺带带来一堆连带行为 —— 被格挡挡住、触发敌人荆棘反伤、
    //    被 RuneWallPower 减半、被"受到攻击时"类钩子反复触发。
    //    ⇒ 改动面太大，且会让箭塔从"稳定输出"变成"会被挡"。
    //  ★ 采用的做法：**伤害保持 Unpowered，但显式把力量加进去**。
    //    精确、可控、零连带风险，且语义上说得通（力量本来就该让一切攻击变强）。
    // ====================================================================
    protected static decimal AddStrength(PowerModel power, decimal amount)
    {
        if (power.Owner == null) return amount;
        return amount + power.Owner.GetPowerAmount<StrengthPower>();
    }
}

/// <summary>冰墙：玩家回合开始时，获得 3×层数 格挡。</summary>
[RegisterPower]
public sealed class IceWallPower : FrostBuildingPowerBase
{
    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;
        await CreatureCmd.GainBlock(Owner, Amount * 3m, ValueProp.Unpowered, null);
    }
}

/// <summary>
/// 箭塔：拥有者回合开始时，对**全部可击中敌人**造成 4×层数 伤害。
///
/// ★ M3.2 改成 AOE（照 Monster Train 2 与本体 `BlackHolePower` 的样板）：
///   `CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, amount, ValueProp.Unpowered, Owner)`
///   原来的版本只打"最左侧一个"，偏离了火炮塔的定位，已废弃。
///
/// 两种挂法都支持：
///   · 挂在**建筑生物**上（M3.2 新玩法：箭塔 = 一座会开火的建筑）
///   · 挂在**玩家**上（M3.1 旧玩法：能力牌式炮台，保留兼容）
/// </summary>
[RegisterPower]
public sealed class ArrowTowerPower : FrostBuildingPowerBase
{
    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        // AOE：一次打全部可击中敌人（BlackHolePower 同款调用）
        // ★ M3.11：显式加上持有者的力量 —— 否则「寒冰王座 +3 力量」对箭塔完全无效。
        await CreatureCmd.Damage(choiceContext, enemies, AddStrength(this, Amount * 4m), ValueProp.Unpowered, Owner);
    }
}

/// <summary>篝火：玩家回合开始时，回复 2×层数 生命。</summary>
[RegisterPower]
public sealed class CampfirePower : FrostBuildingPowerBase
{
    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;
        if (Owner.CurrentHp >= Owner.MaxHp) return;
        await CreatureCmd.Heal(Owner, Amount * 2m, false);
    }
}

/// <summary>了望台：玩家回合开始时，多抽 层数 张牌（Draw 要的是 Player）。</summary>
[RegisterPower]
public sealed class WatchtowerPower : FrostBuildingPowerBase
{
    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;
        var player = Owner.Player;
        if (player == null) return;
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}
