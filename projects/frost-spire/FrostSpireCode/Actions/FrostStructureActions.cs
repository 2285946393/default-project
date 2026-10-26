using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Action;
using STS2RitsuLib.Interop.AutoRegistration;
using FrostSpire.Powers;

namespace FrostSpire.Actions;

// ============================================================================
// 凛冬工事 · 建筑的可点击行动（M3.6 —— 卡池扩充）
//
// 基类 `FrostActionBase` 见 FrostActions.cs（M3.4 做的，MinionLib 的 ActionModel）：
//   挂在随从身上 → 玩家**点随从本体或它的图标**就能触发，不用出牌。
//   机制与星见雅 mod 的邦布完全同源。
// ============================================================================

/// <summary>
/// 寒霜法师 · 冰封：点法师 → **选一个敌人 → 让它跳过下一个回合**。
///
/// ★ 核心 API：`CreatureCmd.Stun(creature)`（反编译确认）——
///   内部会挂一个 stun move，敌人下回合"什么都不做"，并自动播 `NStunnedVfx` 冰封特效。
///   `nextMoveId` 传 null = 晕完之后回到它上一次的行动（官方默认行为）。
///
/// 设计意图：这是**唯一能直接剥夺敌人一回合**的牌 —— 打断 Boss 的大招、
///   给自己争一回合喘息。控制流派的支点。
/// </summary>
[RegisterPower]
public sealed class FrostMageFreezeAction : FrostActionBase
{
    /// <summary>★ 单体目标：点了之后要玩家选一个敌人（和民兵突击同款）。</summary>
    public override TargetType TargetType => TargetType.AnyEnemy;

    /// <summary>每回合 1 次。</summary>
    public override bool DecrementAfterAct => true;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target == null || !target.IsAlive) return;

        await CreatureCmd.Stun(target);

        Entry.Logger.Info($"凛冬尖塔：寒霜法师冰封了 {target.Name}（跳过下一回合）。");
        Entry.Toast($"冰封：{target.Name} 将跳过下一回合。", "凛冬尖塔");
    }
}

/// <summary>
/// 霜爆塔 · 引爆：点塔 → **对全体敌人造成「已蓄力」点伤害，然后清空蓄力**。
///
/// ★ 蓄力 = `FrostBurstChargePower` 的层数（每回合自动 +4）。
///   层数直接当伤害用，所以"攒了多久"和"打多痛"是同一件事，零额外状态。
///
/// ★ `DecrementAfterAct = false`：引爆**不消耗行动次数** ——
///   限制它的不是次数而是蓄力（爆完就清零，再点也没伤害）。
///   所以这里不需要"每回合一次"的约束，随时可点、随时可爆。
/// </summary>
[RegisterPower]
public sealed class FrostBurstDetonateAction : FrostActionBase
{
    public override TargetType TargetType => TargetType.None;

    public override bool DecrementAfterAct => false;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var me = Owner;
        if (me == null) return;

        var charge = me.GetPower<FrostBurstChargePower>();
        if (charge == null || charge.Amount <= 0)
        {
            Entry.Toast("霜爆塔还没蓄力。", "凛冬尖塔");
            return;
        }

        // ★ M3.11：蓄力 + 塔自己的力量（寒冰王座给的力量在这里兑现）
        decimal damage = WithStrength(me, charge.Amount);

        var enemies = me.CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        await CreatureCmd.Damage(choiceContext, enemies, damage, ValueProp.Unpowered, me);

        // 引爆后清空蓄力（重新攒）
        await PowerCmd.Remove(charge);

        Entry.Logger.Info($"凛冬尖塔：霜爆塔引爆，全体敌人 -{damage}（蓄力已清空）。");
    }
}
