using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Action;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Actions;

/// <summary>
/// 凛冬尖塔的「随从行动」基类（M3.4）—— ★ 用户要的「像邦布那样」的东西。
///
/// 机制（反编译 MinionLib.Action 取证，与星见雅 mod 完全同源）：
///   `MinionLib.Action.ActionModel : PowerModel` —— 挂在随从身上的**可点击行动**。
///   挂上去之后，玩家**点随从本体**（或点它的图标）就能触发，不用出牌。
///
/// 关键字段：
///   · `TargetType`（抽象）—— NoTarget / 单体 / 全体。决定点了之后要不要选目标。
///   · `Amount`（继承自 PowerModel）—— ★ **本回合可用次数**（点几次就消耗几次）。
///   · `OnAct(ctx, target)`（抽象）—— 点下去做什么。
///   · `DecrementAfterAct` —— true = 每次触发减 1 层（次数用完图标变灰）。
///   · `AutoRemoveAtTurnEnd` —— true = 回合结束整个移除（一次性行动）。
///   · `CanAct(combat)` —— 默认实现：Amount > 0 且随从活着且在同一场战斗。
///
/// 图标点击由 MinionLib 自动装上（`ActionClickPatch` / `ActionPowerIconClickPatch`
/// 都是 `[HarmonyPatch(typeof(NCreature), "_Ready")]` 的自挂 patch，我们不用写任何 UI 代码）。
///
/// ★ 与「自带 AI 的怪」的区别：
///   怪的行动 = 服务端自动跑（MonsterMoveStateMachine）；
///   本类 = **等玩家点头**（更适合"建筑/装置"这种由玩家操作的东西）。
/// </summary>
public abstract class FrostActionBase : ActionModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>行动图标是给玩家点的，必须可见。</summary>
    protected override bool IsVisibleInternal => true;

    /// <summary>
    /// ★ M3.11：随从的主动攻击也要吃**它自己**的力量。
    ///
    /// 为什么必须显式加：官方 `StrengthPower.ModifyDamageAdditive` 有
    /// `if (!props.IsPoweredAttack()) return 0m;`，而 `IsPoweredAttack()` 要求
    /// `Move && !Unpowered` —— 我们随从的伤害全是 `Unpowered`（官方给"能力伤害"的惯例值），
    /// ⇒ **力量一点都加不上去**，寒冰王座的「全体友军 +3 力量」因此是空头支票。
    /// 详见 `Powers/FrostPowers.cs` 里 `AddStrength` 的注释。
    /// </summary>
    protected static decimal WithStrength(Creature creature, decimal amount)
        => amount + creature.GetPowerAmount<StrengthPower>();
}

/// <summary>
/// 冰垒·举盾：点一次，给**全体我方随从**各加 3 格挡，然后本回合不能再点。
///
/// 设计意图：冰垒 = 前排守卫。除了被动挡伤，还给玩家一个"主动开盾"的操作点，
///   让"打得对不对"这件事有操作空间（而不是纯站桩）。
/// </summary>
[RegisterPower]
public sealed class IceBulwarkGuardAction : FrostActionBase
{
    public const int BlockPerUse = 3;

    public override TargetType TargetType => TargetType.None;

    /// <summary>每回合 1 次。</summary>
    public override bool DecrementAfterAct => true;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var me = Owner;
        if (me == null) return;

        // 给全体我方随从加格挡（含自己）—— 用 PetOwner 拿玩家，再遍历 Pets
        var player = me.PetOwner;
        var pets = player?.PlayerCombatState?.Pets;
        if (pets == null) return;

        foreach (var pet in pets)
        {
            if (pet == null || !pet.IsAlive) continue;
            await CreatureCmd.GainBlock(pet, BlockPerUse, ValueProp.Unpowered, null);
        }

        Entry.Logger.Info($"凛冬尖塔：冰垒举盾，全体随从 +{BlockPerUse} 格挡。");
    }
}

/// <summary>
/// 箭塔·齐射：点一次，对**全体敌人**造成 6 点伤害，每回合 1 次。
///
/// 设计意图：箭塔的每回合被动 AOE 是"保底输出"，
///   这个主动技是玩家自己挑时机打的"爆发"—— 补刀、清残血，有决策感。
/// </summary>
[RegisterPower]
public sealed class ArrowTowerVolleyAction : FrostActionBase
{
    public const int DamagePerUse = 6;

    public override TargetType TargetType => TargetType.None;

    public override bool DecrementAfterAct => true;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var me = Owner;                      // PowerModel.Owner 是非空 Creature
        var combat = me.CombatState;
        if (combat == null) return;

        var enemies = combat.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        // ★ M3.11：加上箭塔自己的力量（寒冰王座给的力量在这里兑现）
        decimal dmg = WithStrength(me, DamagePerUse);
        await CreatureCmd.Damage(choiceContext, enemies, dmg, ValueProp.Unpowered, me);
        Entry.Logger.Info($"凛冬尖塔：箭塔齐射，全体敌人 -{dmg}。");
    }
}

/// <summary>
/// 民兵·突击：点一次，对**指定敌人**造成 8 点伤害，每回合 1 次。
///
/// 设计意图：这是唯一需要**选目标**的行动（`TargetType.SingleTarget`）——
///   玩家点民兵 → 进入选敌模式 → 点敌人 → 打 8。
///   补足民兵"有想法、能操作"的手感（邦布就是这么做的）。
/// </summary>
[RegisterPower]
public sealed class MilitiaChargeAction : FrostActionBase
{
    public const int DamagePerUse = 8;

    /// <summary>★ 单体目标：点了之后要玩家选一个敌人（TargetType.AnyEnemy）。</summary>
    public override TargetType TargetType => TargetType.AnyEnemy;

    public override bool DecrementAfterAct => true;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var me = Owner;
        if (me == null || target == null || !target.IsAlive) return;

        // ★ M3.11：加上民兵自己的力量
        decimal dmg = WithStrength(me, DamagePerUse);
        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, me);
        Entry.Logger.Info($"凛冬尖塔：民兵突击，对 {target.Name} 造成 {dmg}。");
    }
}
