using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Powers;

// ============================================================================
// 凛冬「精锐」能力（M3.12）
//
// 三个《凛冬前线》特色词条（连击 / 固守 / 不朽）的尖塔化实现，
// 外加一条**「历战 → 晋升」的自动成长线** —— 那才是"养大哥"真正的引擎。
//
// ★ 设计主轴：**让"单位活得久"这件事本身产生收益**。
//   《凛冬前线》里防御单位只活一回合，玩家被迫不断补位；
//   尖塔里随从不会自动死，所以我们反过来做 ——
//   **活得越久 → 历战越高 → 晋升 → 越强**。这是同一套"战场经营"乐趣的正向版本。
// ============================================================================

/// <summary>
/// 霜刃卫士 · **连击**：每回合对最前方的敌人**连续攻击 2 次**，每次 5(+力量) 点。
///
/// ★ 两段独立结算（不是一次打 10）：
///   力量每段都加、目标掉两次血条、破甲/易伤各自算一次 —— 这才是"连击"的手感。
/// </summary>
[RegisterPower]
public sealed class FrostBladeComboPower : FrostTriggerPowerBase
{
    public const int DamagePerHit = 5;
    public const int Hits = 2;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        // ★ 连击次数随层数增加：重复部署（= 强化）后每回合打更多下
        int hits = Hits * (int)Amount;
        for (int i = 0; i < hits; i++)
        {
            var target = enemies[0];
            if (target == null || !target.IsAlive) break;

            // ★ M3.11：随从的攻击也要吃自己的力量（否则寒冰王座给的力量是空的）
            decimal dmg = AddStrength(this, DamagePerHit);
            await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, Owner);
        }
    }
}

/// <summary>
/// 不冻者 · **不朽**：每回合结束**回满生命**；每回合攻击 1 次 6(+力量) 点。
///
/// ★ 为什么两个效果写在同一个能力里：它们共同定义了"不冻者"这个单位 ——
///   分开写反而让人看不出这是一体的。（攻击在回合开始，回血在回合结束。）
///
/// ★ 回满而不是"回 X 点"：这才叫"不朽"。
///   代价是它的血量刻意压到 10 —— 遇到单回合 16 伤的 GlobeHead 照样会倒。
/// </summary>
[RegisterPower]
public sealed class UndyingFrostPower : FrostTriggerPowerBase
{
    public const int DamagePerTurn = 6;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var target = enemies[0];
        if (target == null || !target.IsAlive) return;

        decimal dmg = AddStrength(this, Amount * DamagePerTurn);
        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, Owner);
    }

    /// <summary>
    /// 回合结束回满 —— 「不朽」的本体。
    /// ★ 用 `AfterSideTurnEnd`（我方回合真的结束了）而不是 `BeforeSideTurnEnd`，
    ///   否则会在"打出牌之后、弃牌之前"就回满，少了挨打那段。
    /// </summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner == null || !Owner.IsAlive) return;
        if (Owner.Side != side) return;          // 只在我方回合结束时回

        decimal missing = Owner.MaxHp - Owner.CurrentHp;
        if (missing <= 0) return;

        await CreatureCmd.Heal(Owner, missing, false);
    }
}

/// <summary>
/// 凛冬守望者 · **固守**：它存活期间，**全体友军（含它自己）受到的伤害 -1×层数**。
///
/// ★ 实现走 `ModifyDamageAdditive`（就是 `StrengthPower` 用的那个钩子，返回**加值**）：
///   返回负数 = 减伤。它会被询问"这次伤害要调整多少"，我们只在
///   **被打的人是我方**时才返回负值。
///
/// ★ 为什么不用 `ModifyDamageMultiplicative`（RuneWallPower 用的那个）：
///   那个是乘法系数（0.5 = 减半），适合"减半"；
///   我们要的是**固定 -1**（对多段小伤害收益最大），所以用加值版。
/// </summary>
[RegisterPower]
public sealed class FrostWardenPower : FrostTriggerPowerBase
{
    public override decimal ModifyDamageAdditive(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == null || Owner == null) return 0m;
        if (!Owner.IsAlive) return 0m;                 // 它倒了，光环消失
        if (target.Side != Owner.Side) return 0m;      // 只保护我方

        decimal reduce = Amount;                        // -1×层数
        return amount > reduce ? -reduce : -amount;     // ★ 别把伤害减成负数
    }
}

// ============================================================================
//  「历战 → 晋升」—— 养大哥的引擎
// ============================================================================

/// <summary>
/// 历战（挂在**随从**身上）：纯计数器，本身不产生任何效果。
///
/// ★ 为什么单独做一个"什么都不做"的能力：晋升判定需要一个**可读、可清零**的计数，
///   而 `StackType.Counter` 的能力层数天然就是这个东西。
///   真正的逻辑（加历战 / 判定晋升）在 <see cref="FrostDrillPower"/> 里。
/// </summary>
[RegisterPower]
public sealed class FrostVeterancyPower : PowerModel
{
    /// <summary>晋升所需历战层数。</summary>
    public const int Threshold = 4;

    /// <summary>每次晋升的血上限 / 回血量。</summary>
    public const int PromoteHp = 4;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldPlayVfx => false;
}

/// <summary>
/// 凛冬操演（挂在**玩家**身上）：每回合给你**每个**随从 +1×层数 历战；
/// 历战满 4 → 该随从**晋升**（+4 血上限、+1 力量、回 4 血），历战清零，可以继续下一轮晋升。
///
/// ★★ 这是整个 mod 第一次把「活下去」直接换算成「变强」：
///   随从在场上多站 4 个回合 = 一次晋升。于是玩家会**主动保护老兵**、
///   会为"这只已经晋升两次了，不能让它死"做决策 —— 这就是《凛冬前线》"养大哥"的正解。
///   （对比：M3.9 的「遗霜」是把阵亡换成即时收益，这条线是把**存活**换成长期收益，
///     两条线互相拉扯，构成真正的取舍。）
///
/// ★ 为什么光环挂在**玩家**而不是逐个挂随从：
///   挂玩家 → **新召唤的随从下一回合自动开始积累历战**（不用为"后上场的没有"打补丁）。
///   挂随从则每次召唤都要手动补挂，而且能力牌打出时场上可能还没有随从。
///
/// ★ 晋升后 `PowerCmd.Remove(vet)` 清零，下一轮从头攒 —— 每次晋升都是 4 回合，节奏稳定可预期。
/// </summary>
[RegisterPower]
public sealed class FrostDrillPower : FrostTriggerPowerBase
{
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var pets = Owner.Player?.PlayerCombatState?.Pets;
        if (pets == null || pets.Count == 0) return;

        foreach (var pet in pets)
        {
            if (pet == null || !pet.IsAlive) continue;

            // ① 积累历战
            await PowerCmd.Apply<FrostVeterancyPower>(choiceContext, pet, Amount, Owner, null);

            // ② 判定晋升
            await TryPromoteAsync(choiceContext, pet);
        }
    }

    /// <summary>历战满阈值 → 晋升（+血上限、+力量、回血），历战清零。</summary>
    private async Task TryPromoteAsync(PlayerChoiceContext choiceContext, Creature pet)
    {
        var vet = pet.GetPower<FrostVeterancyPower>();
        if (vet == null || vet.Amount < FrostVeterancyPower.Threshold) return;

        // ★ 先清零再发奖励：万一发奖励时抛异常，也不会留下"满层却没晋升"的脏状态
        await PowerCmd.Remove(vet);

        await PowerCmd.Apply<StrengthPower>(choiceContext, pet, 1m, Owner, null);
        await CreatureCmd.GainMaxHp(pet, FrostVeterancyPower.PromoteHp);
        await CreatureCmd.Heal(pet, FrostVeterancyPower.PromoteHp, false);

        Entry.Logger.Info(
            $"凛冬尖塔：{pet.Name} 历战满 {FrostVeterancyPower.Threshold} → 晋升！" +
            $"+{FrostVeterancyPower.PromoteHp} 血上限、+1 力量。");
        Entry.Toast($"{pet.Name} 晋升（+{FrostVeterancyPower.PromoteHp} 血上限 / +1 力量）", "凛冬尖塔");
    }
}
