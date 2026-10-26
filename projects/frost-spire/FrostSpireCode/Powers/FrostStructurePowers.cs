using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Powers;

// ============================================================================
// 凛冬工事 · 「建筑光环」能力（M3.6 —— 卡池扩充）
//
// 和 M3.1 那批「挂在玩家身上的工事能力」（IceWallPower / CampfirePower / …）的区别：
//   这批挂在**建筑身上**，效果作用于**主人玩家**或**全体友军** ——
//   也就是《凛冬前线》里「建筑卡提供场地支援」的定位。
//
// ★ 为什么另起一批而不改旧的：旧四件套已被 M3.1 的能力牌使用且行为已验证，
//   直接改会动到已验证的东西。这里全部新增，互不干扰。
//
// 触发时机统一 `BeforeSideTurnStart` + `IsMyTurnStart` 守卫
//（与 FrostBuildingPowerBase 一致，M3.1~M3.5 实测可用）。
// ============================================================================

/// <summary>
/// 篝火光环：挂在篝火建筑上，**每回合给主人玩家回 2×层数 血**。
///
/// ★ 为什么不复用 M3.1 的 `CampfirePower`：那个是 `Heal(Owner)` ——
///   挂在玩家身上时 Owner 就是玩家（正确），但挂到建筑上 Owner 是建筑，会**给建筑自己回血**。
///   所以这里新写一个，回血对象显式取 `Owner.PetOwner`（主人）。
/// </summary>
[RegisterPower]
public sealed class CampfireAuraPower : FrostBuildingPowerBase
{
    public const int HealPerTurn = 2;

    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var master = Owner.PetOwner?.Creature;
        if (master == null || !master.IsAlive) return;
        if (master.CurrentHp >= master.MaxHp) return;      // 满血不空放

        await CreatureCmd.Heal(master, Amount * HealPerTurn, false);
    }
}

/// <summary>
/// 瞭望台光环：挂在瞭望台建筑上，**每回合给主人玩家多抽 层数 张牌**。
/// 同样因为"抽牌要的是 Player 而不是 Creature"，必须显式取 `Owner.PetOwner`。
/// </summary>
[RegisterPower]
public sealed class WatchtowerAuraPower : FrostBuildingPowerBase
{
    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var master = Owner.PetOwner;
        if (master == null) return;

        await CardPileCmd.Draw(choiceContext, (int)Amount, master);
    }
}

/// <summary>
/// 符文壁：**受普通攻击时伤害减半**（1:1 抄本体 `GuardedPower`）。
///
/// ★ 钩子语义（反编译 `GuardedPower` 确认）：`ModifyDamageMultiplicative` 返回的是
///   **乘法系数** —— 1m = 不变、0.5m = 减半、2m = 翻倍。别把它当成"减少的量"。
/// ★ `props.IsPoweredAttack()` 过滤：**只减普通攻击**，不减毒/能力伤害 ——
///   这是官方一致惯例（`DieForYouPower` / `ThornsPower` 都这么做），跟着做即可。
/// </summary>
[RegisterPower]
public sealed class RuneWallPower : FrostBuildingPowerBase
{
    /// <summary>受伤系数（0.5 = 减半）。</summary>
    public const decimal DamageFactor = 0.5m;

    /// <summary>不叠加 —— 一座墙挂一层就够，多了也不会更强。</summary>
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner) return 1m;               // 只管自己
        if (!props.IsPoweredAttack()) return 1m;      // 只减普通攻击
        return DamageFactor;
    }
}

/// <summary>
/// 寒霜法师光环：挂在法师身上，**每回合给随机一个敌人挂 层数 层虚弱**。
///
/// ★ 随机怎么做到「联机各端一致」：不用 `System.Random`，用
///   `new Rng((ulong)combatState.RoundNumber, "frost_mage_aura")` ——
///   种子只由**回合数**决定，各端同一回合拿到的种子必然相同（WaveTable 同款做法）。
/// </summary>
[RegisterPower]
public sealed class FrostMageAuraPower : FrostBuildingPowerBase
{
    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var list = new List<Creature>();
        foreach (var e in enemies)
            if (e != null && e.IsAlive) list.Add(e);
        if (list.Count == 0) return;

        var rng = new Rng((ulong)combatState.RoundNumber, "frost_mage_aura");
        var target = list[rng.NextInt(list.Count)];

        await PowerCmd.Apply<WeakPower>(choiceContext, target, Amount, Owner, null);
    }
}

/// <summary>
/// 霜爆塔 · 蓄力：挂在霜爆塔上，**每回合给自己叠 4 层蓄力**（层数 = 攒下的伤害）。
///
/// ★ 层数就是伤害：`Amount` 每回合 +4，玩家点「引爆」时一次性打出并清零。
///   这就是《凛冬前线》精锐卡"越养越强"的轻量版 —— 攒得越久越猛，
///   但要赌这座塔在被拆之前能不能憋出来。
/// </summary>
[RegisterPower]
public sealed class FrostBurstChargePower : FrostBuildingPowerBase
{
    public const int ChargePerTurn = 4;

    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side)) return;

        // 给自己加层数（层数 = 已攒的伤害）
        await PowerCmd.Apply<FrostBurstChargePower>(choiceContext, Owner, ChargePerTurn, Owner, null);
    }
}

/// <summary>
/// 寒冰王座光环：挂在王座上，**每回合给主人玩家 +2 格挡**（王座的持续支援）。
/// 王座的另一半年（部署时全体友军 +3 力量）写在卡里，见 `FrostStructureCards`。
/// </summary>
[RegisterPower]
public sealed class FrostThroneAuraPower : FrostBuildingPowerBase
{
    public const int BlockPerTurn = 2;

    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var master = Owner.PetOwner?.Creature;
        if (master == null || !master.IsAlive) return;

        await CreatureCmd.GainBlock(master, Amount * BlockPerTurn, ValueProp.Unpowered, null);
    }
}
