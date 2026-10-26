using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Powers;

// ============================================================================
// 凛冬「回响」· 触发式能力（M3.9）
//
// ★ 用户原话：
//   「自己多加些凛冬牌。要有创意。比如那我方场上被摧毁，对所有敌人造成一次伤害。
//     这种效果牌。你不会动脑子直接改成如果我的牌被消耗。对所有敌人造成一次伤害吗。
//     把凛冬里面要触发效果的牌换成我们这边要触发效果的牌。诸如此类还要我多说吗」
//
// ⇒ 《凛冬前线》的卡之所以"有意思"，一大半来自**触发式效果**：
//   它不写在"打出时"，而写在"某件事发生时"。
//   这个 mod 之前 16 张牌**全是"打出即生效"**，一张触发式都没有 —— 这是最大的缺口。
//
// ★★ 设计主轴：**把触发条件全部换成"我方侧"的事件**
//   · 我方随从被摧毁        → 遗霜（伤害）/ 霜之遗赠（格挡）
//   · 我的牌被消耗          → 雪崩（伤害）/ 余烬（抽牌）
//   · 我方随从受到伤害      → 凛冬之怒（反击）
//   · 敌人的格挡被打破      → 碎冰（追加）
//   · 我弃一张牌            → 寒风（伤害）
//   · 我打出一张攻击牌      → 霜之回响（追加）
//
// ★★★ 钩子从哪来（反编译 `MegaCrit.Sts2.Core.Models.AbstractModel` 核实，非猜测）：
//   PowerModel 里写了 `public override bool ShouldReceiveCombatHooks => true;`
//   ⇒ **Power 一定会收到战斗钩子**，只要覆写同名虚方法即可。
//
//   已核实的签名（ilspycmd 反编译 sts2.dll 原文）：
//     Task AfterDeath(PlayerChoiceContext, Creature, bool wasRemovalPrevented, float deathAnimLength)
//     Task AfterCardExhausted(PlayerChoiceContext, CardModel, bool causedByEthereal)
//     Task AfterCardDiscarded(PlayerChoiceContext, CardModel)
//     Task AfterCardPlayed(PlayerChoiceContext, CardPlay)
//     Task AfterDamageReceived(PlayerChoiceContext, Creature, DamageResult, ValueProp, Creature?, CardModel?)
//     Task AfterBlockBroken(PlayerChoiceContext, Creature target, Creature? breaker)
//
// ★★ 三个必须防的坑：
//   ① **钩子是全局的**（敌人死、别人的牌消耗都会调进来）→ 每张都必须自己过滤阵营/归属，
//      否则会出现"敌人死了给我方发伤害""联机时别人打牌我触发"。
//   ② **`AfterDeath` 里 creature 可能已经不在 `Pets` 里了** → 不能用
//      `Pets.Contains(creature)` 判断阵营，必须靠 `IsPet` + `PetOwner`。
//   ③ **伤害来源用 `ValueProp.Unpowered`**：这是"反应"，不该吃力量加成
//      （和 M3.1 起的 ArrowTowerPower / MilitiaAttackPower 保持一致）。
// ============================================================================

/// <summary>
/// 触发式能力的公共基类：层数 = 强度（Counter 叠加，重复打出变强）。
///
/// ★ 为什么用 `Counter` 而不是 `Single`：`StackType.Counter` 允许同一能力叠多层，
///   效果按 `Amount`（层数）放大 —— 这正是「重复部署 = 强化」那套手感的延续，
///   也是《凛冬前线》"同一张牌打两遍会变强"的核心。
/// </summary>
public abstract class FrostTriggerPowerBase : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldPlayVfx => false;

    // ---- 阵营 / 归属判定（钩子是全局的，必须自己过滤）----

    /// <summary>死者是不是**我的随从**（排除我自己、排除敌人、排除联机队友的随从）。</summary>
    protected static bool IsMyMinion(PowerModel power, Creature? c)
    {
        if (c == null) return false;
        if (!c.IsPet) return false;                      // 玩家本体 / 敌人：PetOwner == null
        return c.PetOwner == power.Owner.Player;         // 是我的
    }

    /// <summary>这张牌是不是**我的**（排除队友的牌）。</summary>
    protected static bool IsMyCard(PowerModel power, CardModel? card)
        => card != null && card.Owner == power.Owner.Player;

    /// <summary>这个生物是不是**敌人**。用 Side 判，不用 IsMonster（我方随从也是 MonsterModel）。</summary>
    protected static bool IsEnemy(PowerModel power, Creature? c)
        => c != null && power.Owner != null && c.Side != power.Owner.Side;

    /// <summary>
    /// 是不是**我方**回合开始（照抄 `FrostBuildingPowerBase` 的守卫：`Owner.Side == side`）。
    /// ★ 两个基类各留一份而不是互相继承，是为了让"触发式"和"建筑光环"两条线互不耦合。
    /// </summary>
    protected static bool IsMyTurnStart(PowerModel power, CombatSide side)
        => power.Owner != null && power.Owner.Side == side;

    /// <summary>
    /// ★ M3.11：把持有者的**力量**加进伤害数值。
    ///
    /// 为什么必须显式加：官方 `StrengthPower.ModifyDamageAdditive` 有
    /// `if (!props.IsPoweredAttack()) return 0m;`，而 `IsPoweredAttack()` =
    /// `HasFlag(Move) && !HasFlag(Unpowered)`。我们的伤害全是 `Unpowered`
    /// ⇒ **力量一点都加不上去**，寒冰王座的「全体友军 +3 力量」会是空头支票。
    /// （本基类里同样留一份，理由同 `IsMyTurnStart`。）
    /// </summary>
    protected static decimal AddStrength(PowerModel power, decimal amount)
    {
        if (power.Owner == null) return amount;
        return amount + power.Owner.GetPowerAmount<StrengthPower>();
    }

    // ---- 效果输出 ----

    /// <summary>对全体可击中敌人造成 `amount` 伤害（无来源、不吃力量）。</summary>
    protected static async Task HitAllAsync(PowerModel power, PlayerChoiceContext ctx, decimal amount)
    {
        var enemies = power.CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var alive = new List<Creature>();
        foreach (var e in enemies)
            if (e != null && e.IsAlive) alive.Add(e);
        if (alive.Count == 0) return;

        await CreatureCmd.Damage(ctx, alive, amount, ValueProp.Unpowered, power.Owner);
    }
}

// ============================================================================
//  一、殉爆线 ——「我方随从被摧毁」触发
// ============================================================================

/// <summary>
/// 遗霜（用户点名的第一张）：**我方随从被摧毁时，对全体敌人造成 6×层数 伤害**。
///
/// ★ 这是整个 mod 第一张真正的"反应牌"：它的价值不在打出的那一秒，
///   而在于之后**每一次**随从倒下都会回响一次。
///   ⇒ 它把「随从会死」这个**坏事**变成了**资源** —— 这就是《凛冬前线》
///     「防御单位只活一回合」的设计精神：单位本来就是消耗品，那就让消耗有回报。
///
/// ★ 为什么只认"随从"不认"玩家本体"：玩家死了游戏就结束了，触发毫无意义；
///   而且 `IsPet` 恰好把玩家本体排除（玩家的 `PetOwner == null`）。
/// </summary>
[RegisterPower]
public sealed class LastFrostPower : FrostTriggerPowerBase
{
    public const int Damage = 6;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented) return;          // 「复活类」阻止了移除 → 不算真死
        if (Amount <= 0) return;
        if (!IsMyMinion(this, creature)) return;

        await HitAllAsync(this, choiceContext, Amount * Damage);

        Entry.Logger.Info($"凛冬尖塔：遗霜 —— {creature.Name} 倒下，全体敌人 -{Amount * Damage}。");
    }
}

/// <summary>
/// 霜之遗赠：**我方随从被摧毁时，获得 8×层数 格挡**。
///
/// ★ 和「遗霜」是同一触发点的两种解法 —— 一个把死亡换成**输出**，一个换成**续航**。
///   这两张不能同时太强，所以遗赠给的格挡更高（格挡是一次性的，伤害是永久的）。
/// </summary>
[RegisterPower]
public sealed class FrostLegacyPower : FrostTriggerPowerBase
{
    public const int Block = 8;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented) return;
        if (Amount <= 0) return;
        if (!IsMyMinion(this, creature)) return;

        await CreatureCmd.GainBlock(Owner, Amount * Block, ValueProp.Unpowered, null);
    }
}

// ============================================================================
//  二、消耗线 ——「我的牌被消耗」触发
// ============================================================================

/// <summary>
/// 雪崩（用户点名的第二张）：**每当一张我的牌被消耗，对全体敌人造成 5×层数 伤害**。
///
/// ★★ 为什么必须配套「消耗源」卡（这是设计上的硬约束，不是可选）：
///   我们在做之前盘过整个凛冬卡池 —— **一张带 `Exhaust` 关键字的牌都没有**。
///   没有消耗源，这张牌就是**永远不触发的死牌**（玩家打出来只看到一句永远不兑现的话）。
///   ⇒ 所以同一批里必须补 `FrostBrandCard`（霜燃：1 费抽 2，消耗）/ `IceShardCard`（冰棱：0 费 7 伤，消耗），
///     让"消耗流"真的能自己转起来。这是这一批最重要的一条自我约束。
///
/// ★ `causedByEthereal` 也照常触发：虚无牌回合末自动烧掉，照样算"被消耗"。
/// </summary>
[RegisterPower]
public sealed class AvalanchePower : FrostTriggerPowerBase
{
    public const int Damage = 5;

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Amount <= 0) return;
        if (!IsMyCard(this, card)) return;

        await HitAllAsync(this, choiceContext, Amount * Damage);
    }
}

/// <summary>
/// 余烬：**每当一张我的牌被消耗，抽 1×层数 张牌**。
///
/// ★ 消耗流的另一条腿：雪崩给输出，余烬给续航。
///   两张一起挂上 → 「霜燃」变成 1 费「全体 -5 + 净抽 1 张」的正循环，
///   这就是《凛冬前线》里"卡组自己转起来"的感觉。
/// </summary>
[RegisterPower]
public sealed class WinterEmberPower : FrostTriggerPowerBase
{
    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Amount <= 0) return;
        if (!IsMyCard(this, card)) return;

        var player = Owner.Player;
        if (player == null) return;

        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
    }
}

// ============================================================================
//  三、受击线 ——「我方随从挨打」触发
// ============================================================================

/// <summary>
/// 凛冬之怒：**我方随从受到伤害时，对全体敌人造成 3×层数 伤害**。
///
/// ★ 和本体 `ThornsPower`（荆棘：被**攻击**时反伤）的区别：
///   荆棘挂在**玩家**身上、只反弹给**攻击者**；这张挂在玩家身上但监听**随从**挨打，
///   而且**打全体** —— 也就是《凛冬前线》"整条战线一起反击"的味道。
///   数值刻意压到 3：一个回合里 3 个随从各挨一下就是 9 点全体，再高就失控了。
///
/// ★ 为什么不用 `AfterDamageReceivedLate`：没必要等，立即反打手感更直接。
/// </summary>
[RegisterPower]
public sealed class WinterWrathPower : FrostTriggerPowerBase
{
    public const int Damage = 3;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Amount <= 0) return;
        if (!IsMyMinion(this, target)) return;

        await HitAllAsync(this, choiceContext, Amount * Damage);
    }
}

// ============================================================================
//  四、破甲线 ——「敌人格挡被打破」触发
// ============================================================================

/// <summary>
/// 碎冰：**每当一名敌人的格挡被打破，对它造成 8×层数 伤害**。
///
/// ★ 触发点 `AfterBlockBroken(ctx, target, breaker)`：
///   `target` = 格挡被打破的人。我们只关心**敌人**的格挡被打碎
///   （本体怪会加格挡，比如 BowlbugEgg 的「Protect +7」）。
///
/// ★ 为什么是"追加伤害"而不是"打碎格挡"：打碎格挡是**攻击行为**的修饰，
///   这里我们要的是**事件响应** —— 破甲的瞬间补一刀，把「破了敌人一层壳」
///   变成一次真正的开膛。8 点偏高，是因为这个触发点**出现频率低**。
/// </summary>
[RegisterPower]
public sealed class IceShatterPower : FrostTriggerPowerBase
{
    public const int Damage = 8;

    public override async Task AfterBlockBroken(
        PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
    {
        if (Amount <= 0) return;
        if (!IsEnemy(this, target)) return;
        if (!target.IsAlive) return;

        await CreatureCmd.Damage(choiceContext, target, Amount * Damage, ValueProp.Unpowered, Owner);
    }
}

// ============================================================================
//  五、牌流动线 ——「弃牌 / 打出攻击牌」触发
// ============================================================================

/// <summary>
/// 寒风：**每当你弃一张牌，对全体敌人造成 2×层数 伤害**。
///
/// ★ 触发点 `AfterCardDiscarded` 包括**回合结束时自动弃掉的手牌** ——
///   这是**故意保留**的：它让这张牌变成「手上留的牌越多，回合结束伤害越高」，
///   和尖塔"手牌管理"的核心玩法咬合。数值压到 2 就是这个原因（5 张手牌 = 10 点）。
/// </summary>
[RegisterPower]
public sealed class ColdWindPower : FrostTriggerPowerBase
{
    public const int Damage = 2;

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (Amount <= 0) return;
        if (!IsMyCard(this, card)) return;

        await HitAllAsync(this, choiceContext, Amount * Damage);
    }
}

/// <summary>
/// 霜之回响：**每当你打出一张攻击牌，对全体敌人造成 3×层数 伤害**。
///
/// ★ 这是**攻击流**的放大器：凛冬打击打 6 点单体，挂上它以后每张攻击牌都附带一次全体 3 点。
///   ⇒ 它把原本"单体"的凛冬打击变成了"伪 AOE"，是让**基础卡后期不废**的关键设计
///   （16 波连打，第 10 波 6 点单体已经不够看了）。
/// </summary>
[RegisterPower]
public sealed class FrostEchoPower : FrostTriggerPowerBase
{
    public const int Damage = 3;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount <= 0) return;
        if (cardPlay?.Card == null) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (cardPlay.Player != Owner.Player) return;

        await HitAllAsync(this, choiceContext, Amount * Damage);
    }
}
