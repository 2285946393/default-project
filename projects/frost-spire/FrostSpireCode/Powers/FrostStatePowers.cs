using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Powers;

// ============================================================================
// 凛冬「裂霜」· 状态与养成能力（M3.10）
//
// ★ 为什么做这一批 —— 不是"再多几张"，是**补两个结构性缺口**：
//
//   ① **凛冬卡池一张易伤（Vulnerable）都没有**。
//      易伤是尖塔的**伤害放大器**（本体 Bash = 2 费 8 伤 + 2 层易伤，实际收益远超 8）。
//      没有它，凛冬卡组到中后期会"打不动" —— 这是 M3.8 就记下的隐患：
//      「实测若打不动，**优先补一张带易伤的凛冬牌，而不是再削波次**」。
//
//   ② **力量（Strength）只有寒冰王座一张 4 费 Rare**。
//      力量是"养大哥"的通用货币，全池只有一张高价牌，成长线等于没有。
//
// ⇒ 所以这一批的两条主轴是：**易伤来源** + **力量来源**，
//   并且**继续沿用 M3.9 的触发式范式**（不写"打出即生效"的平庸牌）。
//
// ★★ 本批挖到的官方样板：`JuggernautPower`（本体「获得格挡时对随机敌人造成伤害」）
//   反编译原文：
//     public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
//     {
//         if (!(amount <= 0m) && creature == base.Owner) {
//             var enemies = base.CombatState.HittableEnemies;
//             if (enemies.Count != 0) {
//                 var target = base.Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);
//                 await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, base.Amount, ValueProp.Unpowered, base.Owner);
//             }
//         }
//     }
//
//   ★★ 从这段里学到两件事（都是自己想会想错的）：
//     a) `AfterBlockGained` **没有 PlayerChoiceContext 参数**，但 Damage 仍需要一个 →
//        官方用 `new ThrowingPlayerChoiceContext()`，**不是传 null**。
//        （"Throwing" = 一旦真的需要玩家做选择就抛异常，因为这里不该有选择。）
//     b) 官方联机安全的随机取目标是 `Owner.Player.RunState.Rng.CombatTargets.NextItem(list)`。
// ============================================================================

/// <summary>
/// 霜蚀蔓延：**每当一名敌人死亡，把它身上的易伤层数转给其它还活着的敌人**。
///
/// ★ 这是易伤线的**放大器**，也是本批最有创意的一张：
///   它把「易伤」从一个**会随目标死亡而蒸发**的 debuff，变成了**可以传递的资源**。
///   ⇒ 打法因此改变：给**快死的那个**挂易伤，让它死时把 debuff 传下去 ——
///     《凛冬前线》"战场是一个整体"的味道，而不是逐个点杀。
///
/// ★ 为什么读得到死者的层数：`AfterDeath` 触发时 creature 对象还在、Powers 还在，
///   用 `GetPowerAmount<VulnerablePower>()` 取（反编译确认有这个泛型方法）。
/// </summary>
[RegisterPower]
public sealed class FrostSpreadPower : FrostTriggerPowerBase
{
    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented) return;
        if (Amount <= 0) return;
        if (!IsEnemy(this, creature)) return;

        int stacks = creature.GetPowerAmount<VulnerablePower>();
        if (stacks <= 0) return;

        var enemies = CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var alive = new List<Creature>();
        foreach (var e in enemies)
        {
            // ★ 必须排除死者本人：它可能还挂在 HittableEnemies 里（刚死、还没移除）
            if (e == null || !e.IsAlive || e == creature) continue;
            alive.Add(e);
        }
        if (alive.Count == 0) return;

        decimal transfer = stacks * Amount;
        await PowerCmd.Apply<VulnerablePower>(choiceContext, alive, transfer, Owner, null);

        Entry.Logger.Info($"凛冬尖塔：霜蚀蔓延 —— {creature.Name} 的 {stacks} 层易伤散布给 {alive.Count} 名敌人。");
    }
}

/// <summary>
/// 霜锻：**每回合开始时，获得 1×层数 力量**。
///
/// ★ 力量的**平价来源**（寒冰王座 4 费只给一次性 +3，这张 1 费是复利）。
///   ⇒ 这就是《凛冬前线》"精锐养成"最直白的译法：**越早挂，滚得越大**。
///   2 费挂两层 = 每回合 +2，10 回合后就是 +20 力量。
/// </summary>
[RegisterPower]
public sealed class FrostForgePower : FrostTriggerPowerBase
{
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}

/// <summary>
/// 极寒核心：**我方随从被摧毁时，获得 1×层数 力量**。
///
/// ★ 殉爆线的**第三种解法**（前两种是遗霜的即时伤害 / 霜之遗赠的格挡）：
///   这个把死亡换成**永久成长** —— 死一个随从就永久变强一点。
///   ⇒ 三张牌构成一条完整的「怎么对待阵亡」决策树：要眼前输出、要这回合活命、还是要长期变强。
///     这是《凛冬前线》"防御单位只活一回合"能成立的根本：单位本来就会死，问题只是死得值不值。
/// </summary>
[RegisterPower]
public sealed class DeepFreezeCorePower : FrostTriggerPowerBase
{
    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented) return;
        if (Amount <= 0) return;
        if (!IsMyMinion(this, creature)) return;

        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}

/// <summary>
/// 冰壳：**每当你获得格挡时，对全体敌人造成 1×层数 伤害**。
///
/// ★ 它是「防御」与「进攻」之间的桥 —— 让「凛冬壁垒」这种纯保命牌也带输出。
///   数值压到 1 是因为触发频率极高（每回合好几张格挡牌）：
///   一张壁垒 5 格挡触发一次 = 5 点？不 —— **是按"获得格挡"这个事件触发，不按格挡量**，
///   所以一张壁垒只触发一次 1×层数 点。想让伤害高就叠层数（多打几张冰壳）。
///
/// ★★ 关键实现细节（照抄官方 `JuggernautPower`，别自己发明）：
///   `AfterBlockGained` **没有 PlayerChoiceContext 参数** → 用
///   `new ThrowingPlayerChoiceContext()`（**不是 null**）。
///   另：`creature == Owner` 守卫 —— 别人的格挡不关我事（联机）。
/// </summary>
[RegisterPower]
public sealed class IceShellPower : FrostTriggerPowerBase
{
    public const int Damage = 1;

    public override async Task AfterBlockGained(
        Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount <= 0m) return;
        if (creature != Owner) return;
        if (Amount <= 0) return;

        var enemies = CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var alive = new List<Creature>();
        foreach (var e in enemies)
            if (e != null && e.IsAlive) alive.Add(e);
        if (alive.Count == 0) return;

        // ★ 官方同款：这个钩子没有 ctx，用 ThrowingPlayerChoiceContext
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), alive, Amount * Damage, ValueProp.Unpowered, Owner);
    }
}
